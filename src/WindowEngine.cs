using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace Reswin {
    internal sealed class WindowEngine : IDisposable {
        readonly Settings settings;
        readonly HoldScroll scroll;
        readonly Dispatcher dispatcher;
        readonly uint ownProcess=(uint)Process.GetCurrentProcess().Id;
        readonly int ownIntegrity;
        readonly Native.HookProc mouseProc,keyProc;
        readonly bool[] suppressed=new bool[5];
        IntPtr mouseHook,keyHook;
        Gesture current;
        IntPtr clickWindow;
        Native.Point clickPoint;
        int clickButton=-1,clickModifier;
        uint clickTime;
        volatile bool disposed;
        readonly object workLock=new object();
        readonly Dictionary<IntPtr,Queue<WindowRequest>> workers=new Dictionary<IntPtr,Queue<WindowRequest>>();
        readonly object snapLock=new object();
        readonly Dictionary<IntPtr,SideSnap> sideSnaps=new Dictionary<IntPtr,SideSnap>();
        readonly string snapProperty="Reswin.SideSnap."+Guid.NewGuid().ToString("N");
        int snapCookie;
        sealed class SideSnap {
            internal uint Process,Thread;
            internal IntPtr Cookie;
            internal Native.Rect Normal,Applied,PlacementNormal;
            internal bool HavePlacement;
        }
        internal sealed class WindowRequest {
            internal Gesture Gesture;
            internal Native.Point Point;
        }
        internal event Action<string> Status;
        internal sealed class Gesture {
            internal IntPtr Window;
            internal WindowAction Action;
            internal Native.Rect Origin;
            internal Native.Point Start,Latest;
            internal int Button,Edges,MinW=1,MinH=1,MaxW=100000,MaxH=100000;
            internal bool Initialized;
            internal bool Dragged,ClickCancelled,CanMaximize,MoveMaximize,MaximizeOnly,RestoringSideSnap;
            internal int DragX,DragY,DoubleX,DoubleY,Modifier;
            internal uint Started,Dpi;
            internal IntPtr SnapMonitor;
            internal int SnapDirection;
            internal IntPtr EdgeMonitor;
            internal Native.Rect EdgeBounds;
            internal int EdgeDirection;
            internal WindowRequest QueuedRequest;
        }
        internal WindowEngine(Settings settings,Dispatcher dispatcher) {
            this.settings=settings; this.dispatcher=dispatcher; scroll=new HoldScroll(settings,dispatcher);
            ownIntegrity=Native.Integrity(ownProcess); mouseProc=MouseHook; keyProc=KeyHook;
        }
        internal void Start() {
            mouseHook=Native.SetWindowsHookEx(14,mouseProc,Native.GetModuleHandle(null),0);
            keyHook=Native.SetWindowsHookEx(13,keyProc,Native.GetModuleHandle(null),0);
            if(mouseHook==IntPtr.Zero || keyHook==IntPtr.Zero) {
                int error=Marshal.GetLastWin32Error(); Dispose();
                throw new System.ComponentModel.Win32Exception(error,L.T("Не удалось включить управление окнами."));
            }
        }
        IntPtr MouseHook(int code,IntPtr message,IntPtr pointer) {
            if(code<0) return Native.CallNextHookEx(mouseHook,code,message,pointer);
            if(message.ToInt32()==0x200 && current==null) return Native.CallNextHookEx(mouseHook,code,message,pointer);
            try {
                var data=Marshal.PtrToStructure<Native.MouseData>(pointer);
                if((data.Flags&1)==0 && (scroll.HandleMouse(message.ToInt32(),data.Mouse) || HandleMouse(message.ToInt32(),data.Point,data.Mouse))) return new IntPtr(1);
            } catch(Exception e) { Cancel(); Report(L.T("Жест отменён: ")+e.Message); }
            return Native.CallNextHookEx(mouseHook,code,message,pointer);
        }
        IntPtr KeyHook(int code,IntPtr message,IntPtr pointer) {
            if(code>=0) {
                try {
                    var data=Marshal.PtrToStructure<Native.KeyData>(pointer);
                    if((data.Flags&0x10)==0) {
                        if(scroll.HandleKey(message.ToInt32(),(int)data.Key,current==null,data.Scan,data.Flags)) return new IntPtr(1);
                        if((message.ToInt32()==0x101 || message.ToInt32()==0x105) && data.Key==settings.Modifier) Cancel();
                    }
                } catch { Cancel(); }
            }
            return Native.CallNextHookEx(keyHook,code,message,pointer);
        }
        internal bool HandleMouse(int message,Native.Point point,uint mouseData=0) {
            // Keep idle mouse-move callbacks allocation-free and skip all Win32 queries.
            if(message==0x200 && current==null) return false;
            return HandleMouse(message,point,(Native.GetAsyncKeyState(settings.Modifier)&0x8000)!=0,mouseData);
        }
        internal bool HandleMouse(int message,Native.Point point,bool modifierHeld,uint mouseData=0) {
            if(message==0x200) {
                if(current==null) return false;
                if(!settings.Enabled || !modifierHeld) { Cancel(); return false; }
                // Observe movement without consuming it: swallowing WM_MOUSEMOVE
                // freezes the OS cursor, so subsequent relative input never
                // accumulates and the final button-up resets the window position.
                UpdatePointer(current,point);
                if(current.Action!=WindowAction.Move || current.Dragged) Schedule(current);
                return false;
            }
            int button=message==0x201 || message==0x202?0:message==0x204 || message==0x205?1:message==0x207 || message==0x208?2:-1;
            // WM_XBUTTONDOWN/UP: high word identifies XBUTTON1 (back) or XBUTTON2 (forward).
            if(message==0x20B || message==0x20C) {
                uint side=(mouseData>>16)&0xFFFF;
                button=side==1?3:side==2?4:-1;
            }
            if(button<0) {
                if(message==0x20A || message==0x20E) { ClearClick(); if(current!=null) current.ClickCancelled=true; }
                return false;
            }
            scroll.Stop();
            bool down=message==0x201 || message==0x204 || message==0x207 || message==0x20B;
            if(!down) {
                if(!suppressed[button]) return false;
                suppressed[button]=false;
                if(current!=null && current.Button==button) {
                    var completed=current; current=null;
                    Finish(completed,point,modifierHeld);
                }
                return true;
            }
            if(suppressed[button]) return true;
            if(current!=null) { ClearClick(); current.ClickCancelled=true; suppressed[button]=true; return true; }
            if(!settings.Enabled || !modifierHeld) { ClearClick(); return false; }
            WindowAction action=settings.Action(button); if(action==WindowAction.None) { ClearClick(); return false; }
            IntPtr window=Native.GetAncestor(Native.WindowFromPoint(point),2);
            Native.Rect rect;
            if(!Eligible(window,action,out rect)) { ClearClick(); return false; }
            suppressed[button]=true;
            if(settings.Modifier==0xA4) Native.MaskAltMenu();
            var gesture=new Gesture { Window=window,Action=action,Origin=rect,Start=point,Latest=point,Button=button,Edges=Geometry.Edges(rect,point) };
            if(action==WindowAction.Move) {
                gesture.Started=unchecked((uint)Environment.TickCount);
                gesture.Modifier=settings.Modifier; gesture.Dpi=Native.WindowDpi(window);
                gesture.DragX=Native.PointerMetric(68,gesture.Dpi); gesture.DragY=Native.PointerMetric(69,gesture.Dpi);
                // Windows reports the full double-click rectangle, centered at the first down.
                gesture.DoubleX=Native.PointerMetric(36,gesture.Dpi)/2; gesture.DoubleY=Native.PointerMetric(37,gesture.Dpi)/2;
                gesture.CanMaximize=CanMaximize(window);
                bool doubleClick=settings.MaximizeOnDoubleClick && gesture.CanMaximize &&
                    clickWindow==window && clickButton==button && clickModifier==settings.Modifier &&
                    unchecked(gesture.Started-clickTime)<=Native.GetDoubleClickTime() &&
                    Math.Abs((long)point.X-clickPoint.X)<=gesture.DoubleX && Math.Abs((long)point.Y-clickPoint.Y)<=gesture.DoubleY;
                ClearClick();
                if(doubleClick) {
                    Schedule(new Gesture {Window=window,Action=WindowAction.Maximize,MoveMaximize=true});
                    return true;
                }
                // A click must not restore a maximized window before a possible second click.
                // Start moving only once the system drag threshold is crossed.
                current=gesture;
                return true;
            }
            ClearClick();
            if(action==WindowAction.Move || action==WindowAction.Resize) current=gesture;
            Schedule(gesture);
            return true;
        }
        void ClearClick() { clickWindow=IntPtr.Zero; clickButton=-1; }
        bool CanMaximize(IntPtr window) {
            uint process; Native.GetWindowThreadProcessId(window,out process);
            return process!=ownProcess && (Native.GetWindowLongPtr(window,-16).ToInt64()&0x10000)!=0;
        }
        void UpdatePointer(Gesture gesture,Native.Point point) {
            gesture.Latest=point;
            if(gesture.Action==WindowAction.Move && !gesture.Dragged &&
                (Math.Abs((long)point.X-gesture.Start.X)>gesture.DragX || Math.Abs((long)point.Y-gesture.Start.Y)>gesture.DragY)) {
                gesture.Dragged=true;
                ClearClick();
            }
            if(gesture.Action==WindowAction.Move && gesture.Dragged && settings.MaximizeOnTopEdge) UpdateEdge(gesture,point);
        }
        void UpdateEdge(Gesture gesture,Native.Point point) {
            Native.Rect bounds=gesture.EdgeBounds;
            if(gesture.EdgeMonitor==IntPtr.Zero || point.X<bounds.Left || point.X>=bounds.Right || point.Y<bounds.Top || point.Y>=bounds.Bottom) {
                IntPtr monitor=Native.MonitorFromPoint(point,2);
                var info=new Native.MonitorInfo(); info.Size=Marshal.SizeOf(info);
                gesture.EdgeDirection=0;
                if(monitor==IntPtr.Zero || !Native.GetMonitorInfo(monitor,ref info)) {gesture.EdgeMonitor=IntPtr.Zero; return;}
                gesture.EdgeMonitor=monitor; gesture.EdgeBounds=bounds=info.Monitor;
            }
            // Match the usable edge gesture, not a two-pixel mouse-up target.
            // Hysteresis retains a reached edge through small release jitter.
            double scale=(gesture.Dpi==0?96:gesture.Dpi)/96.0;
            int topEnter=Math.Min(Math.Max(1,bounds.Height/4),(int)Math.Ceiling(32*scale));
            int topLeave=Math.Min(Math.Max(1,bounds.Height/4),(int)Math.Ceiling(48*scale));
            int sideEnter=Math.Min(Math.Max(1,bounds.Width/4),(int)Math.Ceiling(120*scale));
            int sideLeave=Math.Min(Math.Max(1,bounds.Width/4),(int)Math.Ceiling(152*scale));
            int top=point.Y-bounds.Top,left=point.X-bounds.Left,right=bounds.Right-1-point.X;
            int next=top<=topEnter?1:left<=sideEnter?2:right<=sideEnter?3:0;
            if(next==0 && gesture.EdgeDirection!=0) {
                int distance=gesture.EdgeDirection==1?top:gesture.EdgeDirection==2?left:right;
                if(distance<=(gesture.EdgeDirection==1?topLeave:sideLeave)) next=gesture.EdgeDirection;
            }
            gesture.EdgeDirection=next;
        }
        void Finish(Gesture gesture,Native.Point point,bool modifierHeld) {
            UpdatePointer(gesture,point);
            if(gesture.Action!=WindowAction.Move) { Schedule(gesture); return; }
            if(!gesture.Dragged) {
                ClearClick();
                uint now=unchecked((uint)Environment.TickCount);
                if(settings.Enabled && modifierHeld && settings.MaximizeOnDoubleClick && gesture.CanMaximize && !gesture.ClickCancelled &&
                    settings.Modifier==gesture.Modifier && unchecked(now-gesture.Started)<=Native.GetDoubleClickTime()) {
                    clickWindow=gesture.Window; clickPoint=gesture.Start; clickButton=gesture.Button;
                    clickModifier=gesture.Modifier; clickTime=gesture.Started;
                }
                return;
            }
            // Commit the final position first. A separate, non-coalesced terminal action
            // cannot be overtaken by an older cursor sample from this completed drag.
            Schedule(gesture);
            if(!settings.Enabled || !modifierHeld || !settings.MaximizeOnTopEdge || !gesture.CanMaximize ||
                gesture.ClickCancelled || settings.Modifier!=gesture.Modifier) return;
            if(gesture.EdgeMonitor!=IntPtr.Zero && gesture.EdgeDirection!=0) {
                Schedule(new Gesture {Window=gesture.Window,Action=WindowAction.Maximize,
                    MoveMaximize=true,MaximizeOnly=true,SnapMonitor=gesture.EdgeMonitor,SnapDirection=gesture.EdgeDirection,
                    Modifier=gesture.Modifier});
            }
        }
        internal bool Eligible(IntPtr window,WindowAction action,out Native.Rect rect) {
            rect=new Native.Rect();
            if(window==IntPtr.Zero || !Native.IsWindowVisible(window) || Native.IsIconic(window) || !Native.GetWindowRect(window,out rect) || rect.Width<=0 || rect.Height<=0) return false;
            uint process; Native.GetWindowThreadProcessId(window,out process);
            // Allow moving and centering our settings without changing the tray
            // lifecycle or attempting to resize the fixed-size settings window.
            if(process==ownProcess && action!=WindowAction.Move && action!=WindowAction.Center) return false;
            long style=Native.GetWindowLongPtr(window,-16).ToInt64(), extended=Native.GetWindowLongPtr(window,-20).ToInt64();
            if((style&0x40000000)!=0 || (extended&0x08000000)!=0) return false;
            string cls=Native.Class(window);
            if(cls=="Progman" || cls=="WorkerW" || cls=="Shell_TrayWnd" || cls=="Shell_SecondaryTrayWnd" || cls=="#32768") return false;
            int cloaked; if(Native.DwmGetWindowAttribute(window,14,out cloaked,4)==0 && cloaked!=0) return false;
            var monitor=Native.Monitor(window);
            if(!Native.IsZoomed(window) && rect.Left<=monitor.Monitor.Left && rect.Top<=monitor.Monitor.Top && rect.Right>=monitor.Monitor.Right && rect.Bottom>=monitor.Monitor.Bottom) return false;
            string path=Native.ProcessPath(process);
            if(path==null || settings.IsExcluded(path)) return false;
            if(Native.Integrity(process)>ownIntegrity) { Report(L.T("Окно запущено с повышенными правами. Для него запустите Reswin от администратора.")); return false; }
            if(action==WindowAction.Resize && ((style&0x40000)==0 || Native.IsZoomed(window))) return false;
            if(action==WindowAction.Center && Native.IsZoomed(window)) return false;
            if(action==WindowAction.Minimize && (style&0x20000)==0) return false;
            if(action==WindowAction.Maximize && (style&0x10000)==0) return false;
            return true;
        }
        void Schedule(Gesture gesture) {
            lock(workLock) {
                if(disposed) return;
                // Only the newest cursor position waits behind an in-flight resize.
                // Never post a backlog of positions to the destination application's UI.
                if(gesture.QueuedRequest!=null) {
                    gesture.QueuedRequest.Point=gesture.Latest;
                    return;
                }
                var request=new WindowRequest {Gesture=gesture,Point=gesture.Latest};
                gesture.QueuedRequest=request;
                Queue<WindowRequest> queue;
                bool startWorker=!workers.TryGetValue(gesture.Window,out queue);
                if(startWorker) {
                    queue=new Queue<WindowRequest>();
                    workers.Add(gesture.Window,queue);
                }
                queue.Enqueue(request);
                // One worker per window also preserves ordering across separate gestures
                // and one-shot actions. A hung application cannot block other windows.
                if(startWorker) ThreadPool.QueueUserWorkItem(delegate { Drain(gesture.Window); });
            }
        }
        void Drain(IntPtr window) {
            var cadence=Stopwatch.StartNew();
            long previous=-8;
            while(true) {
                // Cap fast applications at roughly 125 updates/s without changing the
                // system timer resolution. Slow applications naturally apply backpressure.
                int delay=(int)(8-(cadence.ElapsedMilliseconds-previous));
                if(delay>0) Thread.Sleep(delay);
                WindowRequest request;
                lock(workLock) {
                    Queue<WindowRequest> queue;
                    if(!workers.TryGetValue(window,out queue)) return;
                    if(disposed || queue.Count==0) { workers.Remove(window); return; }
                    request=queue.Dequeue();
                    request.Gesture.QueuedRequest=null;
                }
                previous=cadence.ElapsedMilliseconds;
                if(disposed || !Native.IsWindow(window)) continue;
                try { Apply(request.Gesture,request.Point); }
                catch(Exception e) {
                    string error=e.Message;
                    if(!disposed && !dispatcher.HasShutdownStarted) dispatcher.BeginInvoke(new Action(delegate {
                        if(current==request.Gesture) current=null;
                        Report(L.T("Не удалось изменить окно: ")+error);
                    }));
                }
            }
        }
        internal void Apply(Gesture g) { Apply(g,g.Latest); }
        void Apply(Gesture g,Native.Point point) {
            if(g.MoveMaximize) {
                if(g.Initialized) return;
                g.Initialized=true;
                Native.Rect rect;
                if(disposed || !settings.Enabled || !(g.MaximizeOnly?settings.MaximizeOnTopEdge:settings.MaximizeOnDoubleClick) ||
                    !Eligible(g.Window,WindowAction.Maximize,out rect)) return;
                if(g.MaximizeOnly) {
                    if(Native.IsZoomed(g.Window)) return;
                    if(g.SnapDirection>1) SnapSide(g.Window,g.SnapMonitor,g.SnapDirection==3,rect);
                    else {
                        var own=OwnSnap(g.Window,rect);
                        if(own!=null && !RestoreNormal(g.Window,own,g.SnapMonitor)) return;
                        if(!Native.GetWindowRect(g.Window,out rect) || !PrepareSnap(g.Window,g.SnapMonitor,rect)) return;
                        Native.ShowWindow(g.Window,3);
                    }
                } else ToggleMaximize(g.Window,rect);
                // Every state change completes on the window's worker, after its
                // final Move and before any later gesture for this same window.
                return;
            }
            if(g.Action==WindowAction.Minimize || g.Action==WindowAction.Maximize) {
                if(!g.Initialized) {
                    g.Initialized=true;
                    if(g.Action==WindowAction.Minimize) Native.ShowWindowAsync(g.Window,6);
                    else {
                        Native.Rect rect;
                        if(Native.GetWindowRect(g.Window,out rect)) ToggleMaximize(g.Window,rect);
                    }
                }
                return;
            }
            if(!g.Initialized) {
                g.Initialized=true;
                Native.Rect currentRect;
                if(!Native.GetWindowRect(g.Window,out currentRect)) return;
                // Earlier requests can finish between this gesture's mouse-down
                // and its first worker turn. Validate restore state against the
                // current window, while keeping the original cursor anchor.
                var own=OwnSnap(g.Window,currentRect);
                bool arranged=g.Action==WindowAction.Move && Native.IsArranged(g.Window);
                bool maximized=g.Action==WindowAction.Move && Native.IsZoomed(g.Window);
                if(g.Action==WindowAction.Move && (own!=null || arranged || maximized)) {
                    Native.Rect normal;
                    if(own!=null) normal=own.Normal;
                    else {
                        var placement=new Native.Placement(); placement.Length=Marshal.SizeOf(placement);
                        if(!Native.GetWindowPlacement(g.Window,ref placement)) return;
                        // WINDOWPLACEMENT uses workspace coordinates. Only its
                        // dimensions are used; the cursor supplies screen position.
                        normal=placement.Normal;
                    }
                    g.Origin=AnchorRestore(g.Origin,normal,g.Start);
                    g.RestoringSideSnap=own!=null;
                    if(arranged || maximized) Native.ShowWindow(g.Window,9);
                } else if(g.Action==WindowAction.Center && own!=null) {
                    g.Origin=own.Normal;
                    g.RestoringSideSnap=true;
                }
                if(g.Action==WindowAction.Resize) {
                    var limits=new Native.MinMax(); IntPtr result;
                    // Query outside the hook, with a strict timeout for unresponsive apps.
                    if(Native.SendMessageTimeout(g.Window,0x24,IntPtr.Zero,ref limits,2,40,out result)!=IntPtr.Zero) {
                        g.MinW=Math.Max(1,limits.MinTrack.X); g.MinH=Math.Max(1,limits.MinTrack.Y);
                        g.MaxW=limits.MaxTrack.X>0?limits.MaxTrack.X:100000; g.MaxH=limits.MaxTrack.Y>0?limits.MaxTrack.Y:100000;
                    }
                }
            }
            Native.Rect target=g.Origin;
            if(g.Action==WindowAction.Center) target=Geometry.Center(target,Native.Monitor(g.Window).Work);
            else {
                int dx=point.X-g.Start.X,dy=point.Y-g.Start.Y;
                if(g.Action==WindowAction.Resize) target=Geometry.Resize(target,g.Edges,dx,dy,g.MinW,g.MinH,g.MaxW,g.MaxH);
                else target=new Native.Rect(target.Left+dx,target.Top+dy,target.Right+dx,target.Bottom+dy);
            }
            Native.Rect actual;
            bool haveActual=Native.GetWindowRect(g.Window,out actual);
            if(haveActual && SameRect(actual,target)) return;
            uint flags=0x4|0x10;
            if(haveActual && actual.Width==target.Width && actual.Height==target.Height) flags|=0x1;
            // Synchronous on the background worker: wait until this position has been
            // processed, then take the newest sample. ASYNCWINDOWPOS would defeat
            // coalescing by filling the receiving thread's queue with obsolete resizes.
            if(Native.SetWindowPos(g.Window,IntPtr.Zero,target.Left,target.Top,target.Width,target.Height,flags) && g.RestoringSideSnap) {
                ForgetSnap(g.Window);
                g.RestoringSideSnap=false;
            }
        }
        static bool SameRect(Native.Rect a,Native.Rect b) {
            return a.Left==b.Left && a.Top==b.Top && a.Right==b.Right && a.Bottom==b.Bottom;
        }
        static Native.Rect VisibleNormal(Native.Rect rect,Native.Rect work) {
            int x=rect.Width<=work.Width?Geometry.Clamp(rect.Left,work.Left,work.Right-rect.Width):work.Left+(work.Width-rect.Width)/2;
            int y=rect.Height<=work.Height?Geometry.Clamp(rect.Top,work.Top,work.Bottom-rect.Height):work.Top;
            return new Native.Rect(x,y,x+rect.Width,y+rect.Height);
        }
        static Native.Rect AnchorRestore(Native.Rect from,Native.Rect normal,Native.Point pointer) {
            int width=Math.Max(1,normal.Width),height=Math.Max(1,normal.Height);
            double fraction=Math.Max(0,Math.Min(1,(pointer.X-from.Left)/(double)Math.Max(1,from.Width)));
            int x=pointer.X-(int)Math.Round(width*fraction);
            int y=pointer.Y-Math.Min(Math.Max(0,pointer.Y-from.Top),height/2);
            var info=new Native.MonitorInfo(); info.Size=Marshal.SizeOf(info);
            if(Native.GetMonitorInfo(Native.MonitorFromPoint(pointer,2),ref info)) {
                int title=Math.Min(height,Math.Max(24,Native.PointerMetric(4,96)));
                x=Geometry.Clamp(x,info.Work.Left-width+Math.Min(width,title),info.Work.Right-Math.Min(width,title));
                y=Geometry.Clamp(y,info.Work.Top,Math.Max(info.Work.Top,info.Work.Bottom-title));
            }
            return new Native.Rect(x,y,x+width,y+height);
        }
        SideSnap OwnSnap(IntPtr window,Native.Rect actual) {
            lock(snapLock) {
                SideSnap state;
                if(!sideSnaps.TryGetValue(window,out state)) return null;
                uint process; uint thread=Native.GetWindowThreadProcessId(window,out process);
                bool valid=process==state.Process && thread==state.Thread && Native.GetProp(window,snapProperty)==state.Cookie;
                if(valid && (Native.IsZoomed(window) || Native.IsIconic(window))) {
                    var placement=new Native.Placement(); placement.Length=Marshal.SizeOf(placement);
                    // Compare placement coordinates with the saved placement, never
                    // with screen coordinates. Native move/resize must invalidate
                    // our restore record even when followed by maximize/minimize.
                    valid=state.HavePlacement && Native.GetWindowPlacement(window,ref placement) && SameRect(placement.Normal,state.PlacementNormal);
                } else if(valid) valid=SameRect(actual,state.Applied);
                if(valid) return state;
                ForgetSnapLocked(window,state);
                return null;
            }
        }
        void ForgetSnapLocked(IntPtr window,SideSnap state) {
            if(Native.GetProp(window,snapProperty)==state.Cookie) Native.RemoveProp(window,snapProperty);
            sideSnaps.Remove(window);
        }
        void ForgetSnap(IntPtr window) {
            lock(snapLock) {
                SideSnap state;
                if(sideSnaps.TryGetValue(window,out state)) ForgetSnapLocked(window,state);
            }
        }
        bool RestoreNormal(IntPtr window,SideSnap state,IntPtr monitor) {
            var info=new Native.MonitorInfo(); info.Size=Marshal.SizeOf(info);
            if(monitor==IntPtr.Zero) monitor=Native.MonitorFromWindow(window,2);
            if(!Native.GetMonitorInfo(monitor,ref info)) return false;
            var rect=VisibleNormal(state.Normal,info.Work);
            if(Native.IsZoomed(window) || Native.IsArranged(window)) Native.ShowWindow(window,9);
            if(!Native.SetWindowPos(window,IntPtr.Zero,rect.Left,rect.Top,rect.Width,rect.Height,0x4|0x10)) return false;
            ForgetSnap(window);
            return true;
        }
        void ToggleMaximize(IntPtr window,Native.Rect actual) {
            bool maximized=Native.IsZoomed(window);
            var state=OwnSnap(window,actual);
            if(state!=null) {
                // Give Windows the original normal rectangle before maximizing,
                // so its own Restore button also restores the original size.
                if(!RestoreNormal(window,state,IntPtr.Zero)) return;
                if(maximized) return;
            }
            Native.ShowWindow(window,maximized?9:3);
        }
        bool PrepareSnap(IntPtr window,IntPtr monitor,Native.Rect rect) {
            var info=new Native.MonitorInfo(); info.Size=Marshal.SizeOf(info);
            if(monitor==IntPtr.Zero || !Native.GetMonitorInfo(monitor,ref info) || info.Work.Width<=0 || info.Work.Height<=0) return false;
            // Maximize still uses Windows' normal state change, after giving it
            // a visible restore location on the monitor reached by the pointer.
            var visible=VisibleNormal(rect,info.Work);
            if((rect.Left!=visible.Left || rect.Top!=visible.Top) && !Native.SetWindowPos(window,IntPtr.Zero,visible.Left,visible.Top,0,0,0x4|0x10|0x1)) return false;
            if(Native.MonitorFromWindow(window,2)==monitor) return true;
            int x=info.Work.Left+(info.Work.Width-rect.Width)/2;
            int y=rect.Height<=info.Work.Height?info.Work.Top+(info.Work.Height-rect.Height)/2:info.Work.Top;
            if(!Native.SetWindowPos(window,IntPtr.Zero,x,y,0,0,0x4|0x10|0x1)) return false;
            return Native.MonitorFromWindow(window,2)==monitor;
        }
        static Native.Rect HalfFrame(IntPtr window,Native.Rect visible) {
            Native.Rect outer,frame;
            if(!Native.GetWindowRect(window,out outer) || Native.DwmGetWindowFrame(window,9,out frame,Marshal.SizeOf(typeof(Native.Rect)))!=0) return visible;
            // GetWindowRect includes the invisible resize border; align the actual
            // visible frame with the half-workarea, including on scaled displays.
            int limit=(int)Math.Ceiling(64.0*Native.WindowDpi(window)/96);
            int left=Geometry.Clamp(frame.Left-outer.Left,0,limit),top=Geometry.Clamp(frame.Top-outer.Top,0,limit);
            int right=Geometry.Clamp(outer.Right-frame.Right,0,limit),bottom=Geometry.Clamp(outer.Bottom-frame.Bottom,0,limit);
            return new Native.Rect(visible.Left-left,visible.Top-top,visible.Right+right,visible.Bottom+bottom);
        }
        void SnapSide(IntPtr window,IntPtr monitor,bool right,Native.Rect actual) {
            if(disposed || (Native.GetWindowLongPtr(window,-16).ToInt64()&0x40000)==0) return;
            var info=new Native.MonitorInfo(); info.Size=Marshal.SizeOf(info);
            if(monitor==IntPtr.Zero || !Native.GetMonitorInfo(monitor,ref info) || info.Work.Width<2 || info.Work.Height<1) return;
            // Both halves share this exact boundary: odd widths leave the extra
            // pixel to the right, without gaps or overlap and with taskbars spared.
            int middle=info.Work.Left+info.Work.Width/2;
            var half=right?new Native.Rect(middle,info.Work.Top,info.Work.Right,info.Work.Bottom):new Native.Rect(info.Work.Left,info.Work.Top,middle,info.Work.Bottom);
            var state=OwnSnap(window,actual);
            bool created=state==null;
            if(created) {
                uint process; uint thread=Native.GetWindowThreadProcessId(window,out process);
                if(process==0 || thread==0) return;
                state=new SideSnap {Process=process,Thread=thread,Normal=VisibleNormal(actual,info.Work),Applied=actual};
                lock(snapLock) {
                    if(disposed) return;
                    int cookie=unchecked(++snapCookie)&int.MaxValue;
                    if(cookie==0) cookie=unchecked(++snapCookie)&int.MaxValue;
                    state.Cookie=new IntPtr(cookie);
                    // A per-window property disappears when HWND is destroyed;
                    // PID/thread alone cannot detect handle reuse in one process.
                    if(!Native.SetProp(window,snapProperty,state.Cookie)) return;
                    sideSnaps[window]=state;
                }
            }
            var target=HalfFrame(window,half);
            // Direct placement does not enter the shell's Snap workflow, so it
            // cannot open Snap Assist or inject shortcuts into another app.
            if(!Native.SetWindowPos(window,IntPtr.Zero,target.Left,target.Top,target.Width,target.Height,0x4|0x10)) {
                if(created) ForgetSnap(window);
                return;
            }
            // A cross-monitor DPI change can alter invisible frame thickness.
            // Recalculate once after the target has processed its DPI messages.
            var corrected=HalfFrame(window,half);
            if(!SameRect(target,corrected)) Native.SetWindowPos(window,IntPtr.Zero,corrected.Left,corrected.Top,corrected.Width,corrected.Height,0x4|0x10);
            Native.Rect applied;
            if(!Native.GetWindowRect(window,out applied)) {ForgetSnap(window); return;}
            var placement=new Native.Placement(); placement.Length=Marshal.SizeOf(placement);
            bool havePlacement=Native.GetWindowPlacement(window,ref placement);
            lock(snapLock) {
                SideSnap saved;
                if(disposed || !sideSnaps.TryGetValue(window,out saved) || saved!=state) {
                    if(Native.GetProp(window,snapProperty)==state.Cookie) Native.RemoveProp(window,snapProperty);
                    return;
                }
                state.Applied=applied; state.PlacementNormal=placement.Normal; state.HavePlacement=havePlacement;
            }
        }
        internal void Cancel() {
            scroll.Stop(); ClearClick();
            if(current!=null) {
                var completed=current; current=null;
                if(completed.Action!=WindowAction.Move || completed.Dragged) Schedule(completed);
            }
        }
        void Report(string text) { if(Status!=null) dispatcher.BeginInvoke(new Action(delegate { if(Status!=null) Status(text); })); }
        public void Dispose() {
            disposed=true; current=null; scroll.Dispose();
            lock(workLock) {
                foreach(var queue in workers.Values) {
                    foreach(var request in queue) request.Gesture.QueuedRequest=null;
                    queue.Clear();
                }
                workers.Clear();
            }
            lock(snapLock) {
                foreach(var pair in sideSnaps) {
                    if(Native.GetProp(pair.Key,snapProperty)==pair.Value.Cookie) Native.RemoveProp(pair.Key,snapProperty);
                }
                sideSnaps.Clear();
            }
            if(mouseHook!=IntPtr.Zero) Native.UnhookWindowsHookEx(mouseHook);
            if(keyHook!=IntPtr.Zero) Native.UnhookWindowsHookEx(keyHook);
            mouseHook=keyHook=IntPtr.Zero;
        }
    }
}
