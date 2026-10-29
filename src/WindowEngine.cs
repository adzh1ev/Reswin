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
        int inputRevision;
        uint clickTime;
        volatile bool disposed;
        readonly object workLock=new object();
        readonly Dictionary<IntPtr,Queue<WindowRequest>> workers=new Dictionary<IntPtr,Queue<WindowRequest>>();
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
            internal bool Dragged,ClickCancelled,CanMaximize,MoveMaximize,MaximizeOnly;
            internal int DragX,DragY,DoubleX,DoubleY,Modifier;
            internal uint Started,Dpi;
            internal IntPtr SnapMonitor;
            internal IntPtr SnapForeground;
            internal int SnapDirection,InputRevision;
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
                        if(message.ToInt32()==0x100 || message.ToInt32()==0x104) Interlocked.Increment(ref inputRevision);
                        if(scroll.HandleKey(message.ToInt32(),(int)data.Key,current==null,data.Scan,data.Flags)) return new IntPtr(1);
                        if((message.ToInt32()==0x101 || message.ToInt32()==0x105) && data.Key==settings.Modifier) Cancel(false);
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
                if(message==0x20A || message==0x20E) { Interlocked.Increment(ref inputRevision); ClearClick(); if(current!=null) current.ClickCancelled=true; }
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
            Interlocked.Increment(ref inputRevision);
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
            IntPtr monitor=Native.MonitorFromPoint(point,2);
            var info=new Native.MonitorInfo(); info.Size=Marshal.SizeOf(info);
            int edge=Math.Max(1,(int)Math.Ceiling(2.0*gesture.Dpi/96));
            if(monitor!=IntPtr.Zero && Native.GetMonitorInfo(monitor,ref info) &&
                point.X>=info.Monitor.Left && point.X<info.Monitor.Right &&
                point.Y>=info.Monitor.Top && point.Y<info.Monitor.Bottom) {
                // Top wins at corners; this setting exposes the three requested
                // native arrangements rather than introducing quarter layouts.
                int direction=point.Y<=info.Monitor.Top+edge?1:
                    point.X<=info.Monitor.Left+edge?2:point.X>=info.Monitor.Right-1-edge?3:0;
                if(direction!=0) Schedule(new Gesture {Window=gesture.Window,Action=WindowAction.Maximize,
                    MoveMaximize=true,MaximizeOnly=true,SnapMonitor=monitor,SnapDirection=direction,
                    Modifier=gesture.Modifier,InputRevision=Volatile.Read(ref inputRevision),SnapForeground=Native.GetForegroundWindow()});
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
                if(!settings.Enabled || !(g.MaximizeOnly?settings.MaximizeOnTopEdge:settings.MaximizeOnDoubleClick) ||
                    !Eligible(g.Window,WindowAction.Maximize,out rect)) return;
                if(g.MaximizeOnly) {
                    if(g.InputRevision!=Volatile.Read(ref inputRevision)) return;
                    if(Native.IsZoomed(g.Window) || !PrepareSnap(g.Window,g.SnapMonitor,rect)) return;
                    if(g.InputRevision!=Volatile.Read(ref inputRevision)) return;
                    if(g.SnapDirection<=1) Native.ShowWindow(g.Window,3);
                    else SnapSide(g);
                } else Native.ShowWindow(g.Window,Native.IsZoomed(g.Window)?9:3);
                // Unlike an asynchronous state change, completion here is ordered with
                // all preceding moves and later gestures for this target window.
                return;
            }
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
                if(g.Action==WindowAction.Move && (Native.IsZoomed(g.Window) || Native.IsArranged(g.Window))) {
                    var placement=new Native.Placement(); placement.Length=Marshal.SizeOf(placement);
                    if(!Native.GetWindowPlacement(g.Window,ref placement)) return;
                    int width=placement.Normal.Width,height=placement.Normal.Height;
                    double fraction=(g.Start.X-g.Origin.Left)/(double)Math.Max(1,g.Origin.Width);
                    int x=g.Start.X-(int)(width*fraction),y=g.Start.Y-Math.Min(g.Start.Y-g.Origin.Top,height/2);
                    g.Origin=new Native.Rect(x,y,x+width,y+height);
                    Native.ShowWindow(g.Window,9);
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
            if(haveActual && actual.Left==target.Left && actual.Top==target.Top &&
                actual.Width==target.Width && actual.Height==target.Height) return;
            uint flags=0x4|0x10;
            if(haveActual && actual.Width==target.Width && actual.Height==target.Height) flags|=0x1;
            // Synchronous on the background worker: wait until this position has been
            // processed, then take the newest sample. ASYNCWINDOWPOS would defeat
            // coalescing by filling the receiving thread's queue with obsolete resizes.
            Native.SetWindowPos(g.Window,IntPtr.Zero,target.Left,target.Top,target.Width,target.Height,flags);
        }
        bool PrepareSnap(IntPtr window,IntPtr monitor,Native.Rect rect) {
            var info=new Native.MonitorInfo(); info.Size=Marshal.SizeOf(info);
            if(monitor==IntPtr.Zero || !Native.GetMonitorInfo(monitor,ref info) || info.Work.Width<=0 || info.Work.Height<=0) return false;
            // Dragging from low in the content can leave the normal rectangle almost
            // entirely above the display. Save a visible restore location before
            // maximizing, with the same normal size and the taskbar left accessible.
            int x=rect.Width<=info.Work.Width?Geometry.Clamp(rect.Left,info.Work.Left,info.Work.Right-rect.Width):info.Work.Left+(info.Work.Width-rect.Width)/2;
            int y=rect.Height<=info.Work.Height?Geometry.Clamp(rect.Top,info.Work.Top,info.Work.Bottom-rect.Height):info.Work.Top;
            if((rect.Left!=x || rect.Top!=y) && !Native.SetWindowPos(window,IntPtr.Zero,x,y,0,0,0x4|0x10|0x1)) return false;
            if(Native.MonitorFromWindow(window,2)==monitor) return true;
            // Windows chooses the maximize display from the window, not the pointer.
            // Centering first resolves drags between monitors without changing size.
            x=info.Work.Left+(info.Work.Width-rect.Width)/2;
            y=rect.Height<=info.Work.Height?info.Work.Top+(info.Work.Height-rect.Height)/2:info.Work.Top;
            if(!Native.SetWindowPos(window,IntPtr.Zero,x,y,0,0,0x4|0x10|0x1)) return false;
            // A target that refuses movement (or an oversized spanning window) must
            // not unexpectedly maximize on another display.
            return Native.MonitorFromWindow(window,2)==monitor;
        }
        void SnapSide(Gesture gesture) {
            if(disposed || dispatcher.HasShutdownStarted || Native.IsArranged(gesture.Window)) return;
            // Prepare/move stays on the worker. The final input transaction runs
            // on the hook dispatcher, so no physical hook event can interleave
            // state validation and generation of the ordered shortcut batch.
            if(gesture.InputRevision!=Volatile.Read(ref inputRevision) ||
                Native.GetForegroundWindow()!=gesture.SnapForeground || !Native.CanSendSnapKeys(gesture.Modifier)) return;
            if(Native.GetForegroundWindow()!=gesture.Window) {
                if(!Native.SetForegroundWindow(gesture.Window)) return;
                // A successful cross-thread activation is asynchronous. Wait
                // briefly for its message to be handled before testing focus,
                // without attaching input queues or blocking our hook thread.
                IntPtr result;
                if(Native.SendMessageTimeoutValue(gesture.Window,0,IntPtr.Zero,IntPtr.Zero,2,100,out result)==IntPtr.Zero) return;
                if(Native.GetForegroundWindow()!=gesture.Window) return;
            }
            bool sent=false;
            dispatcher.Invoke(new Action(delegate {
                Native.Rect currentRect;
                if(disposed || current!=null || !settings.Enabled || !settings.MaximizeOnTopEdge ||
                    settings.Modifier!=gesture.Modifier || gesture.InputRevision!=Volatile.Read(ref inputRevision) ||
                    !Eligible(gesture.Window,WindowAction.Maximize,out currentRect) ||
                    Native.MonitorFromWindow(gesture.Window,2)!=gesture.SnapMonitor || Native.IsArranged(gesture.Window) ||
                    Native.GetForegroundWindow()!=gesture.Window) return;
                sent=Native.SnapToSide(gesture.Window,gesture.SnapDirection==3,gesture.Modifier);
            }),DispatcherPriority.Input);
            if(!sent) return;
            // Shell arrangement is asynchronous to SendInput. Let it commit
            // before this window's worker accepts a subsequent Move request.
            for(int attempt=0;attempt<25 && !disposed;attempt++) {
                if(Native.IsArranged(gesture.Window) || Native.IsIconic(gesture.Window)) break;
                Thread.Sleep(8);
            }
        }
        internal void Cancel(bool cancelPendingSnap=true) {
            if(cancelPendingSnap) Interlocked.Increment(ref inputRevision);
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
            if(mouseHook!=IntPtr.Zero) Native.UnhookWindowsHookEx(mouseHook);
            if(keyHook!=IntPtr.Zero) Native.UnhookWindowsHookEx(keyHook);
            mouseHook=keyHook=IntPtr.Zero;
        }
    }
}
