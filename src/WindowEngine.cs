using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace Reswin {
    internal sealed class WindowEngine : IDisposable {
        readonly Settings settings;
        readonly Dispatcher dispatcher;
        readonly uint ownProcess=(uint)Process.GetCurrentProcess().Id;
        readonly int ownIntegrity;
        readonly Native.HookProc mouseProc,keyProc;
        readonly bool[] suppressed=new bool[5];
        IntPtr mouseHook,keyHook;
        Gesture current;
        bool disposed;
        internal event Action<string> Status;
        internal sealed class Gesture {
            internal IntPtr Window;
            internal WindowAction Action;
            internal Native.Rect Origin;
            internal Native.Point Start,Latest;
            internal int Button,Edges,MinW=1,MinH=1,MaxW=100000,MaxH=100000;
            internal bool Pending,Initialized;
        }
        internal WindowEngine(Settings settings,Dispatcher dispatcher) {
            this.settings=settings; this.dispatcher=dispatcher;
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
                if((data.Flags&1)==0 && HandleMouse(message.ToInt32(),data.Point,data.Mouse)) return new IntPtr(1);
            } catch(Exception e) { Cancel(); Report(L.T("Жест отменён: ")+e.Message); }
            return Native.CallNextHookEx(mouseHook,code,message,pointer);
        }
        IntPtr KeyHook(int code,IntPtr message,IntPtr pointer) {
            if(code>=0) {
                try {
                    var data=Marshal.PtrToStructure<Native.KeyData>(pointer);
                    if((data.Flags&0x10)==0 && (message.ToInt32()==0x101 || message.ToInt32()==0x105) && data.Key==settings.Modifier) Cancel();
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
                current.Latest=point; Schedule(current); return false;
            }
            int button=message==0x201 || message==0x202?0:message==0x204 || message==0x205?1:message==0x207 || message==0x208?2:-1;
            // WM_XBUTTONDOWN/UP: high word identifies XBUTTON1 (back) or XBUTTON2 (forward).
            if(message==0x20B || message==0x20C) {
                uint side=(mouseData>>16)&0xFFFF;
                button=side==1?3:side==2?4:-1;
            }
            if(button<0) return false;
            bool down=message==0x201 || message==0x204 || message==0x207 || message==0x20B;
            if(!down) {
                if(!suppressed[button]) return false;
                suppressed[button]=false;
                if(current!=null && current.Button==button) { current.Latest=point; Cancel(); }
                return true;
            }
            if(suppressed[button]) return true;
            if(current!=null) { suppressed[button]=true; return true; }
            if(!settings.Enabled || !modifierHeld) return false;
            WindowAction action=settings.Action(button); if(action==WindowAction.None) return false;
            IntPtr window=Native.GetAncestor(Native.WindowFromPoint(point),2);
            Native.Rect rect;
            if(!Eligible(window,action,out rect)) return false;
            suppressed[button]=true;
            if(settings.Modifier==0xA4) Native.MaskAltMenu();
            var gesture=new Gesture { Window=window,Action=action,Origin=rect,Start=point,Latest=point,Button=button,Edges=Geometry.Edges(rect,point) };
            if(action==WindowAction.Move || action==WindowAction.Resize) current=gesture;
            Schedule(gesture);
            return true;
        }
        internal bool Eligible(IntPtr window,WindowAction action,out Native.Rect rect) {
            rect=new Native.Rect();
            if(window==IntPtr.Zero || !Native.IsWindowVisible(window) || Native.IsIconic(window) || !Native.GetWindowRect(window,out rect) || rect.Width<=0 || rect.Height<=0) return false;
            uint process; Native.GetWindowThreadProcessId(window,out process);
            if(process==ownProcess) return false;
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
            if(gesture.Pending || disposed) return;
            gesture.Pending=true;
            dispatcher.BeginInvoke(DispatcherPriority.Input,new Action(delegate {
                gesture.Pending=false;
                if(disposed || !Native.IsWindow(gesture.Window)) return;
                try { Apply(gesture); } catch(Exception e) { if(current==gesture) current=null; Report(L.T("Не удалось изменить окно: ")+e.Message); }
            }));
        }
        internal void Apply(Gesture g) {
            if(g.Action==WindowAction.Minimize || g.Action==WindowAction.Maximize) {
                if(!g.Initialized) {
                    g.Initialized=true;
                    // Restore uses Windows' saved normal size and position.
                    int command=g.Action==WindowAction.Minimize?6:Native.IsZoomed(g.Window)?9:3;
                    Native.ShowWindowAsync(g.Window,command);
                }
                return;
            }
            if(!g.Initialized) {
                g.Initialized=true;
                if(g.Action==WindowAction.Move && Native.IsZoomed(g.Window)) {
                    var placement=new Native.Placement(); placement.Length=Marshal.SizeOf(placement);
                    if(!Native.GetWindowPlacement(g.Window,ref placement)) return;
                    int width=placement.Normal.Width,height=placement.Normal.Height;
                    double fraction=(g.Start.X-g.Origin.Left)/(double)Math.Max(1,g.Origin.Width);
                    int x=g.Start.X-(int)(width*fraction),y=g.Start.Y-Math.Min(g.Start.Y-g.Origin.Top,height/2);
                    g.Origin=new Native.Rect(x,y,x+width,y+height);
                    Native.ShowWindowAsync(g.Window,9);
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
                int dx=g.Latest.X-g.Start.X,dy=g.Latest.Y-g.Start.Y;
                if(g.Action==WindowAction.Resize) target=Geometry.Resize(target,g.Edges,dx,dy,g.MinW,g.MinH,g.MaxW,g.MaxH);
                else target=new Native.Rect(target.Left+dx,target.Top+dy,target.Right+dx,target.Bottom+dy);
            }
            // ASYNCWINDOWPOS prevents another application's UI thread from blocking ours.
            Native.SetWindowPos(g.Window,IntPtr.Zero,target.Left,target.Top,target.Width,target.Height,0x4000|0x4|0x10);
        }
        internal void Cancel() { if(current!=null) { Schedule(current); current=null; } }
        void Report(string text) { if(Status!=null) dispatcher.BeginInvoke(new Action(delegate { if(Status!=null) Status(text); })); }
        public void Dispose() {
            disposed=true; current=null;
            if(mouseHook!=IntPtr.Zero) Native.UnhookWindowsHookEx(mouseHook);
            if(keyHook!=IntPtr.Zero) Native.UnhookWindowsHookEx(keyHook);
            mouseHook=keyHook=IntPtr.Zero;
        }
    }
}
