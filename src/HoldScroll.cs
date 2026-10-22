using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;

namespace Reswin {
    internal sealed class HoldScroll : IDisposable {
        readonly Settings settings;
        readonly Dispatcher dispatcher;
        readonly DispatcherTimer timer;
        readonly bool[] keys=new bool[256];
        readonly uint ownProcess=(uint)Process.GetCurrentProcess().Id;
        readonly int ownIntegrity;
        readonly Stopwatch clock=new Stopwatch();
        IntPtr target,root,foreground;
        Native.Point anchor;
        int consumedKey,blocked,epoch,busy;
        double previous,remainder;
        bool latched,disposed;
        internal bool Active { get { return target!=IntPtr.Zero; } }
        internal HoldScroll(Settings settings,Dispatcher dispatcher) {
            this.settings=settings; this.dispatcher=dispatcher;
            ownIntegrity=Native.Integrity(ownProcess);
            timer=new DispatcherTimer(DispatcherPriority.Input,dispatcher) {Interval=TimeSpan.FromMilliseconds(16)};
            timer.Tick+=Tick;
        }
        bool Down(int key) { return keys[key] || (Native.GetAsyncKeyState(key)&0x8000)!=0; }
        int SingleKey { get { return settings.ScrollKey==ScrollBinding.OtherModifier?(settings.Modifier==0xA2?0xA4:0xA2):settings.ScrollKey==ScrollBinding.Space?0x20:settings.ScrollKey==ScrollBinding.F8?0x77:0; } }
        bool TriggerKey(int key) { return settings.ScrollKey==ScrollBinding.CtrlAlt?(key==0xA2 || key==0xA4):key==SingleKey; }
        bool ChordDown { get { return settings.ScrollKey==ScrollBinding.CtrlAlt?keys[0xA2] && keys[0xA4]:SingleKey!=0 && keys[SingleKey]; } }
        bool AllReleased { get { return settings.ScrollKey==ScrollBinding.CtrlAlt?!keys[0xA2] && !keys[0xA4]:SingleKey==0 || !keys[SingleKey]; } }
        internal bool HandleKey(int message,int key,bool canStart) {
            if(key<0 || key>=keys.Length) return false;
            bool down=message==0x100 || message==0x104;
            bool up=message==0x101 || message==0x105;
            if(!down && !up) return false;
            bool repeat=keys[key]; keys[key]=down;
            if(up && key==consumedKey) { consumedKey=0; Stop(); latched=false; blocked=0; return true; }
            if(down && key==consumedKey) return true;
            if(AllReleased) { Stop(); latched=false; blocked=0; }
            if(up && TriggerKey(key)) { Stop(); return false; }
            if(down && !TriggerKey(key)) { if(Active) Stop(); blocked=1; return false; }
            if(!down || repeat || !ChordDown || latched || blocked!=0 || !canStart || !settings.Enabled || settings.ScrollKey==ScrollBinding.None) return false;
            // Do not turn existing Ctrl/Shift/Win shortcuts into scrolling. Ctrl+Alt
            // is an explicit option; right Alt (AltGr) is never used as a trigger.
            if(Down(0xA0) || Down(0xA1) || Down(0xA3) || Down(0xA5) || Down(0x5B) || Down(0x5C)) return false;
            if(settings.ScrollKey!=ScrollBinding.CtrlAlt && ((SingleKey!=0xA2 && Down(0xA2)) || (SingleKey!=0xA4 && Down(0xA4)))) return false;
            latched=true;
            if(!Begin()) return false;
            // Modifiers remain normal modifiers, so Alt+Tab and Ctrl+C still work.
            // Space/F8 are dedicated hotkeys while enabled, with paired key suppression.
            if(key==0x20 || key==0x77) { consumedKey=key; return true; }
            return false;
        }
        bool Begin() {
            Native.Point point;
            if(disposed || !Native.GetCursorPos(out point)) return false;
            IntPtr child=Native.WindowFromPoint(point),window=Native.GetAncestor(child,2);
            if(window==IntPtr.Zero || !Native.IsWindowVisible(window) || Native.IsIconic(window)) return false;
            uint process; Native.GetWindowThreadProcessId(window,out process);
            if(process==ownProcess) return false;
            string cls=Native.Class(window);
            if(cls=="Progman" || cls=="WorkerW" || cls=="Shell_TrayWnd" || cls=="Shell_SecondaryTrayWnd" || cls=="#32768") return false;
            string path=Native.ProcessPath(process);
            if(path==null || settings.IsExcluded(path) || Native.Integrity(process)>ownIntegrity) return false;
            target=child; root=window; anchor=point; foreground=Native.GetForegroundWindow();
            remainder=0; previous=0; clock.Restart(); Interlocked.Increment(ref epoch);
            timer.Start(); return true;
        }
        void Tick(object sender,EventArgs args) {
            Native.Point point;
            if(!Active || disposed || !settings.Enabled || !ChordDown || !Native.IsWindow(target) || !Native.IsWindowVisible(root) || Native.IsIconic(root) || Native.GetForegroundWindow()!=foreground || !Native.GetCursorPos(out point)) { Stop(); return; }
            double now=clock.Elapsed.TotalSeconds,dt=Math.Min(0.05,now-previous); previous=now;
            int distance=point.Y-anchor.Y;
            if(Math.Abs(distance)<=8) { remainder=0; return; }
            if(Interlocked.CompareExchange(ref busy,0,0)!=0) { remainder=0; return; }
            double speed=Math.Min(40,Math.Pow((Math.Abs(distance)-8)/70.0,1.35)*6);
            double amount=-Math.Sign(distance)*speed*120*dt;
            if(Math.Sign(remainder)!=Math.Sign(amount)) remainder=0;
            remainder+=amount;
            int steps=(int)(remainder/15); if(steps==0) return;
            int delta=Math.Max(-240,Math.Min(240,steps*15)); remainder-=delta;
            int generation=epoch;
            IntPtr destination=target;
            long coordinates=(uint)(anchor.X&0xffff)|((long)(uint)(anchor.Y&0xffff)<<16);
            Interlocked.Exchange(ref busy,1);
            ThreadPool.QueueUserWorkItem(delegate {
                bool failed=false;
                try {
                    if(!disposed && generation==Interlocked.CompareExchange(ref epoch,0,0)) {
                        IntPtr result;
                        // Bounded synchronous delivery prevents a slow app accumulating
                        // wheel messages that would keep scrolling after key release.
                        failed=Native.SendMessageTimeoutValue(destination,0x20A,new IntPtr(delta<<16),new IntPtr(coordinates),0x1|0x2|0x20,150,out result)==IntPtr.Zero;
                    }
                } finally { Interlocked.Exchange(ref busy,0); }
                if(failed && !disposed && !dispatcher.HasShutdownStarted) dispatcher.BeginInvoke(new Action(delegate { if(epoch==generation) Stop(); }));
            });
        }
        internal void Stop() {
            if(Active && (keys[0xA4] || (Native.GetAsyncKeyState(0xA4)&0x8000)!=0)) Native.MaskAltMenu();
            target=IntPtr.Zero; timer.Stop(); clock.Stop(); remainder=0;
            Interlocked.Increment(ref epoch);
        }
        public void Dispose() { disposed=true; Stop(); timer.Tick-=Tick; }
    }
}
