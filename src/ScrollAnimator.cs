using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Reswin {
    // One sleeping worker owns the entire scroll stream. It never queues frames
    // on the WPF dispatcher or schedules independent ThreadPool jobs per frame.
    internal sealed class ScrollAnimator : IDisposable {
        sealed class Session {
            internal IntPtr Target,Root,Foreground;
            internal Native.Point Anchor;
            internal int Generation;
        }
        readonly AutoResetEvent wake=new AutoResetEvent(false);
        readonly Thread worker;
        readonly Func<bool> modifiersDown;
        readonly Action<int> stopped;
        Session current;
        volatile bool disposed;
        bool started;
        int sending;
        internal bool IsSending { get { return Volatile.Read(ref sending)!=0; } }
        internal ScrollAnimator(Func<bool> modifiersDown,Action<int> stopped) {
            this.modifiersDown=modifiersDown; this.stopped=stopped;
            worker=new Thread(Run) {IsBackground=true,Name="Reswin scrolling"};
        }
        internal void Start(IntPtr target,IntPtr root,IntPtr foreground,Native.Point anchor,int generation) {
            if(disposed) return;
            Volatile.Write(ref current,new Session {Target=target,Root=root,Foreground=foreground,Anchor=anchor,Generation=generation});
            if(!started) { started=true; worker.Start(); }
            Signal();
        }
        internal void Stop() {
            Volatile.Write(ref current,null);
            if(!disposed) Signal();
        }
        void Signal() {
            // The sleeping worker owns handle cleanup and can finish while
            // Dispose is signalling it. Shutdown must not race into a closed handle.
            try { wake.Set(); } catch(ObjectDisposedException) { }
        }
        bool IsCurrent(Session session) { return !disposed && ReferenceEquals(Volatile.Read(ref current),session); }
        void End(Session session) {
            if(ReferenceEquals(Interlocked.CompareExchange(ref current,null,session),session) && !disposed)
                stopped(session.Generation);
        }
        void Run() {
            try {
                using(var frameTimer=new FrameTimer()) {
                    var waits=new WaitHandle[] {wake,frameTimer};
                    while(!disposed) {
                        var session=Volatile.Read(ref current);
                        if(session==null) { wake.WaitOne(); continue; }
                        try { Animate(session,frameTimer,waits); }
                        catch { End(session); }
                    }
                }
            } finally { wake.Dispose(); }
        }
        void Animate(Session session,FrameTimer frameTimer,WaitHandle[] waits) {
            const double interval=1.0/240;
            var clock=Stopwatch.StartNew();
            var motion=new Motion();
            double previous=0,next=interval;
            while(IsCurrent(session)) {
                double delay=next-clock.Elapsed.TotalSeconds;
                if(delay>0 && !frameTimer.Wait(waits,delay)) return;
                if(!IsCurrent(session)) return;
                Native.Point point;
                if(modifiersDown() || !Native.IsWindow(session.Target) || !Native.IsWindowVisible(session.Root) ||
                    Native.IsIconic(session.Root) || Native.GetForegroundWindow()!=session.Foreground || !Native.GetCursorPos(out point)) {
                    End(session); return;
                }
                double now=clock.Elapsed.TotalSeconds;
                // Never make up missed frames with a large visible jump.
                double dt=Math.Min(1.0/60,Math.Max(0,now-previous)); previous=now;
                int delta=motion.Step(point.Y-session.Anchor.Y,dt);
                if(delta!=0) {
                    long coordinates=(uint)(session.Anchor.X&0xffff)|((long)(uint)(session.Anchor.Y&0xffff)<<16);
                    bool failed=false;
                    Interlocked.Exchange(ref sending,1);
                    try {
                        // This second check pairs with Stop and shortcut replay:
                        // either replay waits for sending, or this frame is cancelled.
                        if(IsCurrent(session) && !modifiersDown()) {
                            IntPtr result;
                            failed=Native.SendMessageTimeoutValue(session.Target,0x20A,new IntPtr(delta<<16),new IntPtr(coordinates),0x1|0x2|0x20,150,out result)==IntPtr.Zero;
                        }
                    } finally { Interlocked.Exchange(ref sending,0); }
                    if(failed) { End(session); return; }
                }
                next+=interval;
                double finished=clock.Elapsed.TotalSeconds;
                if(next<=finished) next=finished+interval;
            }
        }
        sealed class Motion {
            double velocity,acceleration,fraction;
            internal int Step(int distance,double dt) {
                double desired=Math.Abs(distance)<=8?0:-Math.Sign(distance)*Math.Min(40,Math.Pow((Math.Abs(distance)-8)/70.0,1.35)*6)*120;
                double oldVelocity=velocity;
                // Exact critically damped spring solution for velocity. Keeping
                // acceleration as state removes abrupt slope changes from lerping.
                double omega=2/(desired==0?0.035:0.10);
                double error=velocity-desired;
                double term=(acceleration+omega*error)*dt;
                double decay=Math.Exp(-omega*dt);
                velocity=desired+(error+term)*decay;
                acceleration=(acceleration-omega*term)*decay;
                if(desired==0 && Math.Abs(velocity)<0.3 && Math.Abs(acceleration)<2) {
                    velocity=0; acceleration=0; fraction=0; return 0;
                }
                // Integrate sub-frame travel instead of rounding each sample away.
                fraction+=(oldVelocity+velocity)*0.5*dt;
                int delta=Math.Max(-60,Math.Min(60,(int)fraction));
                fraction-=delta;
                // Carry only the sub-unit remainder, never a catch-up backlog.
                if(Math.Abs(fraction)>=1) fraction-=Math.Truncate(fraction);
                return delta;
            }
        }
        sealed class FrameTimer : WaitHandle {
            [DllImport("kernel32.dll",CharSet=CharSet.Unicode,ExactSpelling=true,SetLastError=true)]
            static extern IntPtr CreateWaitableTimerExW(IntPtr attributes,string name,uint flags,uint access);
            [DllImport("kernel32.dll",SetLastError=true)]
            static extern bool SetWaitableTimer(SafeWaitHandle timer,ref long due,int period,IntPtr callback,IntPtr argument,bool resume);
            internal FrameTimer() {
                // Native high-resolution timers do not change global timer resolution.
                IntPtr handle=CreateWaitableTimerExW(IntPtr.Zero,null,2,0x100002);
                if(handle==IntPtr.Zero) handle=CreateWaitableTimerExW(IntPtr.Zero,null,0,0x100002);
                SafeWaitHandle=new SafeWaitHandle(handle,true);
            }
            internal bool Wait(WaitHandle[] waits,double seconds) {
                long due=-Math.Max(1,(long)(seconds*10000000));
                if(!SafeWaitHandle.IsInvalid && SetWaitableTimer(SafeWaitHandle,ref due,0,IntPtr.Zero,IntPtr.Zero,false))
                    return WaitHandle.WaitAny(waits)==1;
                return !waits[0].WaitOne(Math.Max(1,(int)Math.Ceiling(seconds*1000)));
            }
        }
        public void Dispose() {
            if(disposed) return;
            disposed=true; Volatile.Write(ref current,null);
            if(started) Signal(); else wake.Dispose();
        }
    }
}
