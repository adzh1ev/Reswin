using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;

namespace Reswin {
    internal sealed class HoldScroll : IDisposable {
        readonly Settings settings;
        readonly Dispatcher dispatcher;
        readonly ScrollAnimator animator;
        readonly DispatcherTimer replayTimer;
        List<Native.Input> pendingReplay;
        readonly bool[] keys=new bool[256];
        readonly bool[] droppedKeys=new bool[256];
        readonly uint ownProcess=(uint)Process.GetCurrentProcess().Id;
        readonly int ownIntegrity;
        IntPtr target,root,foreground;
        Native.Point anchor;
        int privateKey,epoch;
        volatile bool disposed;
        internal bool Active { get { return target!=IntPtr.Zero; } }
        internal HoldScroll(Settings settings,Dispatcher dispatcher) {
            this.settings=settings; this.dispatcher=dispatcher;
            ownIntegrity=Native.Integrity(ownProcess);
            animator=new ScrollAnimator(SystemModifiersDown,delegate(int generation) {
                if(!disposed && !dispatcher.HasShutdownStarted)
                    dispatcher.BeginInvoke(new Action(delegate { if(!disposed && epoch==generation) Stop(); }));
            });
            replayTimer=new DispatcherTimer(DispatcherPriority.Input,dispatcher) {Interval=TimeSpan.FromMilliseconds(8)};
            replayTimer.Tick+=FlushReplay;
        }
        int ScrollKey { get { return settings.Modifier==0xA2?0xA4:0xA2; } }
        static bool SystemModifiersDown() {
            return (Native.GetAsyncKeyState(0x11)&0x8000)!=0 ||
                   (Native.GetAsyncKeyState(0x12)&0x8000)!=0 ||
                   (Native.GetAsyncKeyState(0x10)&0x8000)!=0 ||
                   (Native.GetAsyncKeyState(0x5B)&0x8000)!=0 ||
                   (Native.GetAsyncKeyState(0x5C)&0x8000)!=0;
        }
        bool OtherKeysDown(int trigger) {
            for(int i=0;i<keys.Length;i++) if(i!=trigger && keys[i]) return true;
            // The incoming trigger has not updated the OS key state yet.
            return SystemModifiersDown() || (Native.GetAsyncKeyState(1)&0x8000)!=0 ||
                (Native.GetAsyncKeyState(2)&0x8000)!=0 || (Native.GetAsyncKeyState(4)&0x8000)!=0 ||
                (Native.GetAsyncKeyState(5)&0x8000)!=0 || (Native.GetAsyncKeyState(6)&0x8000)!=0;
        }
        static Native.Input KeyInput(int key,uint scan=0,uint flags=0) {
            return new Native.Input {Type=1,Keyboard=new Native.KeyboardInput {Key=(ushort)key,Scan=(ushort)scan,Flags=flags}};
        }
        bool ReplayWithModifier(Native.Input input) {
            Stop();
            if(privateKey==0) return false;
            if(animator.IsSending) {
                // Finish the one in-flight wheel delivery before exposing Ctrl.
                // Buffer matching physical key-ups too, so quick shortcuts stay balanced.
                pendingReplay=new List<Native.Input> {KeyInput(privateKey),input};
                privateKey=0; replayTimer.Start(); return true;
            }
            // Send both events in one ordered batch. Physical key-up balances
            // the replayed modifier; injected events bypass our physical hooks.
            uint sent=Native.Inject(KeyInput(privateKey),input);
            if(sent!=0) privateKey=0;
            if(sent==1) return Native.Inject(input)==1;
            return sent==2;
        }
        void FlushReplay(object sender,EventArgs args) {
            if(animator.IsSending) return;
            replayTimer.Stop();
            if(pendingReplay==null) return;
            Native.Input[] inputs=pendingReplay.ToArray(); pendingReplay=null;
            uint sent=Native.Inject(inputs);
            if(sent<inputs.Length) {
                // UIPI or another input filter can reject injection. Never replay
                // an unmodified letter from a shortcut after such a failure.
                for(int i=(int)sent;i<inputs.Length;i++) {
                    var input=inputs[i];
                    if(input.Type==1) {
                        if((input.Keyboard.Flags&2)==0 && keys[input.Keyboard.Key]) droppedKeys[input.Keyboard.Key]=true;
                        else if((input.Keyboard.Flags&2)!=0) Native.Inject(input);
                    } else if((input.Mouse.Flags&(0x4|0x10|0x40|0x100))!=0) Native.Inject(input);
                }
            }
        }
        internal bool HandleKey(int message,int key,bool canStart,uint scan=0,uint flags=0) {
            if(key<0 || key>=keys.Length) return false;
            bool down=message==0x100 || message==0x104;
            bool up=message==0x101 || message==0x105;
            if(!down && !up) return false;
            bool repeat=keys[key]; keys[key]=down;
            if(droppedKeys[key]) { if(up) droppedKeys[key]=false; return true; }
            if(pendingReplay!=null) {
                pendingReplay.Add(KeyInput(key,scan,(flags&1)|(up?2u:0u)));
                return true;
            }
            if(privateKey!=0) {
                if(key==privateKey) {
                    if(up) { privateKey=0; Stop(); }
                    return true;
                }
                if(down) {
                    // If injection is denied, do not turn Ctrl+C into a typed C.
                    if(!ReplayWithModifier(KeyInput(key,scan,flags&1))) droppedKeys[key]=true;
                    return true;
                }
                return false;
            }
            if(!down || repeat || key!=ScrollKey || !canStart || !settings.Enabled ||
                settings.ScrollKey!=ScrollBinding.OtherModifier || OtherKeysDown(key)) return false;
            if(!Begin()) return false;
            // Neither half of a scroll-only modifier reaches the browser.
            privateKey=key;
            return true;
        }
        internal bool HandleMouse(int message,uint data) {
            if((privateKey==0 && pendingReplay==null) || message==0x200) return false;
            uint flags;
            switch(message) {
                case 0x201: flags=0x2; break;
                case 0x202: flags=0x4; break;
                case 0x204: flags=0x8; break;
                case 0x205: flags=0x10; break;
                case 0x207: flags=0x20; break;
                case 0x208: flags=0x40; break;
                case 0x20B: flags=0x80; break;
                case 0x20C: flags=0x100; break;
                case 0x20A: flags=0x800; break;
                case 0x20E: flags=0x1000; break;
                default: return false;
            }
            uint mouse=0;
            if(message==0x20B || message==0x20C) mouse=(data>>16)&0xffff;
            else if(message==0x20A || message==0x20E) mouse=unchecked((uint)(int)(short)(data>>16));
            // Ordinary Ctrl+click / Ctrl+wheel retain their native behavior.
            var input=new Native.Input {Type=0,Mouse=new Native.MouseInput {Flags=flags,Data=mouse}};
            if(pendingReplay!=null) { pendingReplay.Add(input); return true; }
            return ReplayWithModifier(input);
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
            IntPtr focused=Native.GetForegroundWindow();
            uint focusedProcess; Native.GetWindowThreadProcessId(focused,out focusedProcess);
            // Do not intercept shortcuts that cannot be replayed into an elevated app.
            if(focused==IntPtr.Zero || Native.Integrity(focusedProcess)>ownIntegrity) return false;
            target=child; root=window; anchor=point; foreground=focused;
            int generation=Interlocked.Increment(ref epoch);
            animator.Start(target,root,foreground,anchor,generation); return true;
        }
        internal void Stop() {
            target=IntPtr.Zero; animator.Stop();
            Interlocked.Increment(ref epoch);
            // Own the swallowed key until release or a real shortcut: restoring
            // Ctrl here could turn an in-flight wheel event into zoom.
        }
        public void Dispose() {
            disposed=true; Stop(); animator.Dispose();
            replayTimer.Stop(); replayTimer.Tick-=FlushReplay;
            if(pendingReplay!=null) { Native.Inject(pendingReplay.ToArray()); pendingReplay=null; }
            if(privateKey!=0 && keys[privateKey]) Native.Inject(KeyInput(privateKey));
            privateKey=0;
        }
    }
}
