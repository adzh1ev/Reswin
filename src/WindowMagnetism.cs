using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Reswin {
    // A gesture owns this state and only its serialized window worker uses it.
    // Every result is derived from the unsnapped pointer trajectory, so pulling
    // past the release radius always detaches instead of accumulating offsets.
    internal sealed class WindowMagnetism {
        const int RefreshMilliseconds=250,MaxWindows=512;
        readonly Settings settings;
        readonly uint ownProcess;
        readonly int ownIntegrity;
        readonly List<Candidate> windows=new List<Candidate>();
        readonly List<Native.Rect> occluders=new List<Native.Rect>();
        readonly Dictionary<uint,ProcessRule> processes=new Dictionary<uint,ProcessRule>();
        Axis horizontal,vertical;
        uint frameDpi,lastRefresh;
        int insetLeft,insetTop,insetRight,insetBottom;
        bool haveFrames;

        struct Candidate {
            internal IntPtr Window;
            internal uint Process,Thread;
            internal Native.Rect Frame;
        }
        sealed class ProcessRule {internal string Path; internal bool Allowed;}
        struct Axis {
            internal bool Active,Screen;
            internal IntPtr Window,Monitor;
            internal uint Process,Thread;
            internal bool MovingFar,TargetFar;
        }
        struct Choice {
            internal Axis Axis;
            internal int Offset;
            internal long Distance;
        }

        internal WindowMagnetism(Settings settings,uint ownProcess,int ownIntegrity) {
            this.settings=settings; this.ownProcess=ownProcess; this.ownIntegrity=ownIntegrity;
        }
        internal Native.Rect Adjust(IntPtr window,Native.Rect proposed) {
            bool screen=settings.MagnetToScreen,others=settings.MagnetToWindows;
            if(!screen && !others) {horizontal=new Axis(); vertical=new Axis(); return proposed;}
            uint dpi=Native.WindowDpi(window);
            if(frameDpi!=dpi) {
                UpdateInsets(window,dpi); frameDpi=dpi;
                horizontal=new Axis(); vertical=new Axis();
            }
            double scale=(dpi==0?96:dpi)/96.0;
            int enter=Math.Max(1,(int)Math.Ceiling(12*scale));
            int leave=Math.Max(enter,(int)Math.Ceiling(20*scale));
            var visible=new Native.Rect(proposed.Left+insetLeft,proposed.Top+insetTop,proposed.Right-insetRight,proposed.Bottom-insetBottom);
            if(visible.Width<=0 || visible.Height<=0) return proposed;
            uint now=unchecked((uint)Environment.TickCount);
            if(others && (!haveFrames || unchecked(now-lastRefresh)>=RefreshMilliseconds)) {
                RefreshWindows(window); haveFrames=true; lastRefresh=now;
            }
            if(!others) {windows.Clear(); processes.Clear(); haveFrames=false;}
            Native.Rect work=new Native.Rect(); IntPtr monitor=IntPtr.Zero;
            if(screen) {
                monitor=Native.MonitorFromRect(ref visible,2);
                var info=new Native.MonitorInfo(); info.Size=Marshal.SizeOf(info);
                if(monitor==IntPtr.Zero || !Native.GetMonitorInfo(monitor,ref info)) monitor=IntPtr.Zero;
                else work=info.Work;
            }
            int x=Resolve(ref horizontal,true,visible,work,monitor,screen,others,enter,leave);
            int y=Resolve(ref vertical,false,visible,work,monitor,screen,others,enter,leave);
            return new Native.Rect(proposed.Left+x,proposed.Top+y,proposed.Right+x,proposed.Bottom+y);
        }
        void UpdateInsets(IntPtr window,uint dpi) {
            insetLeft=insetTop=insetRight=insetBottom=0;
            Native.Rect outer,frame;
            if(!Native.GetWindowRect(window,out outer) || !ReadFrame(window,outer,out frame)) return;
            int limit=Math.Max(1,(int)Math.Ceiling(64.0*(dpi==0?96:dpi)/96));
            insetLeft=Geometry.Clamp(frame.Left-outer.Left,0,limit);
            insetTop=Geometry.Clamp(frame.Top-outer.Top,0,limit);
            insetRight=Geometry.Clamp(outer.Right-frame.Right,0,limit);
            insetBottom=Geometry.Clamp(outer.Bottom-frame.Bottom,0,limit);
        }
        static bool ReadFrame(IntPtr window,Native.Rect outer,out Native.Rect frame) {
            if(Native.DwmGetWindowFrame(window,9,out frame,Marshal.SizeOf(typeof(Native.Rect)))==0 && frame.Width>0 && frame.Height>0) return true;
            frame=outer; return false;
        }
        int Resolve(ref Axis held,bool x,Native.Rect moving,Native.Rect work,IntPtr monitor,bool screen,bool others,int enter,int leave) {
            int retained;
            if(Retain(held,x,moving,work,monitor,screen,others,leave,out retained)) return retained;
            held=new Axis();
            var best=new Choice {Distance=long.MaxValue};
            if(screen && monitor!=IntPtr.Zero) {
                // Screen corners can hold both axes, but each axis chooses only
                // one edge. The nearest monitor follows the window, not the mouse.
                Offer(ref best,new Axis {Active=true,Screen=true,Monitor=monitor},x?work.Left:work.Top,x?moving.Left:moving.Top,enter);
                Offer(ref best,new Axis {Active=true,Screen=true,Monitor=monitor,MovingFar=true,TargetFar=true},x?work.Right:work.Bottom,x?moving.Right:moving.Bottom,enter);
            }
            if(others) foreach(var target in windows) {
                if(!Overlap(moving,target.Frame,x,enter)) continue;
                int near=x?target.Frame.Left:target.Frame.Top,far=x?target.Frame.Right:target.Frame.Bottom;
                int movingNear=x?moving.Left:moving.Top,movingFar=x?moving.Right:moving.Bottom;
                var axis=new Axis {Active=true,Window=target.Window,Process=target.Process,Thread=target.Thread};
                Offer(ref best,axis,near,movingNear,enter);
                axis.TargetFar=true; Offer(ref best,axis,far,movingNear,enter);
                axis.MovingFar=true; Offer(ref best,axis,far,movingFar,enter);
                axis.TargetFar=false; Offer(ref best,axis,near,movingFar,enter);
            }
            if(!best.Axis.Active) return 0;
            held=best.Axis; return best.Offset;
        }
        bool Retain(Axis held,bool x,Native.Rect moving,Native.Rect work,IntPtr monitor,bool screen,bool others,int leave,out int offset) {
            offset=0; if(!held.Active) return false;
            Native.Rect frame;
            if(held.Screen) {
                if(!screen || monitor==IntPtr.Zero || held.Monitor!=monitor) return false;
                frame=work;
            } else {
                if(!others || !Native.IsWindowVisible(held.Window) || Native.IsIconic(held.Window)) return false;
                uint process; uint thread=Native.GetWindowThreadProcessId(held.Window,out process);
                if(process!=held.Process || thread!=held.Thread) return false;
                bool found=false; frame=new Native.Rect();
                foreach(var candidate in windows) if(candidate.Window==held.Window && candidate.Process==process && candidate.Thread==thread) {frame=candidate.Frame; found=true; break;}
                if(!found || !Overlap(moving,frame,x,leave)) return false;
            }
            int edge=x?(held.TargetFar?frame.Right:frame.Left):(held.TargetFar?frame.Bottom:frame.Top);
            int source=x?(held.MovingFar?moving.Right:moving.Left):(held.MovingFar?moving.Bottom:moving.Top);
            long distance=(long)edge-source;
            if(Math.Abs(distance)>leave) return false;
            offset=(int)distance; return true;
        }
        static bool Overlap(Native.Rect a,Native.Rect b,bool horizontalAxis,int tolerance) {
            return horizontalAxis?(long)a.Top<=(long)b.Bottom+tolerance && (long)b.Top<=(long)a.Bottom+tolerance:
                (long)a.Left<=(long)b.Right+tolerance && (long)b.Left<=(long)a.Right+tolerance;
        }
        static void Offer(ref Choice best,Axis axis,int target,int source,int enter) {
            long offset=(long)target-source,distance=Math.Abs(offset);
            if(distance>enter || distance>=best.Distance) return;
            best=new Choice {Axis=axis,Offset=(int)offset,Distance=distance};
        }
        void RefreshWindows(IntPtr moving) {
            windows.Clear(); occluders.Clear(); int count=0;
            Native.EnumWindows(delegate(IntPtr window,IntPtr unused) {
                if(++count>MaxWindows) return false;
                if(window==moving || !Native.IsWindowVisible(window) || Native.IsIconic(window)) return true;
                long style=Native.GetWindowLongPtr(window,-16).ToInt64(),extended=Native.GetWindowLongPtr(window,-20).ToInt64();
                if((style&0x40000000)!=0 || (extended&(0x08000000|0x80))!=0 || Native.GetWindow(window,4)!=IntPtr.Zero) return true;
                string cls=Native.Class(window);
                if(cls=="Progman" || cls=="WorkerW" || cls=="Shell_TrayWnd" || cls=="Shell_SecondaryTrayWnd" || cls=="#32768" || cls=="tooltips_class32") return true;
                int cloaked; if(Native.DwmGetWindowAttribute(window,14,out cloaked,4)==0 && cloaked!=0) return true;
                Native.Rect outer,frame;
                if(!Native.GetWindowRect(window,out outer) || outer.Width<=0 || outer.Height<=0) return true;
                ReadFrame(window,outer,out frame);
                if(Native.MonitorFromRect(ref frame,0)==IntPtr.Zero) return true;
                // A fully covered background window has no visible border to
                // attract to. The actively dragged window itself never occludes.
                foreach(var front in occluders) if(frame.Left>=front.Left && frame.Top>=front.Top && frame.Right<=front.Right && frame.Bottom<=front.Bottom) return true;
                occluders.Add(frame);
                uint process; uint thread=Native.GetWindowThreadProcessId(window,out process);
                if(process==0 || thread==0) return true;
                ProcessRule rule;
                if(!processes.TryGetValue(process,out rule)) {
                    if(processes.Count>=MaxWindows) processes.Clear();
                    string path=Native.ProcessPath(process);
                    rule=new ProcessRule {Path=path,Allowed=path!=null && (process==ownProcess || Native.Integrity(process)<=ownIntegrity)};
                    processes[process]=rule;
                }
                if(!rule.Allowed) return true;
                try {if(settings.IsExcluded(rule.Path)) return true;} catch {return true;}
                windows.Add(new Candidate {Window=window,Process=process,Thread=thread,Frame=frame});
                return true;
            },IntPtr.Zero);
        }
    }
}
