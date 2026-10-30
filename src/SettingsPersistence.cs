using System;
using System.Threading;

namespace Reswin {
    // One latest snapshot and at most one write in flight. Disk and XML work
    // never run on input/animation dispatch; shutdown flushes the latest edit.
    internal sealed class SettingsPersistence : IDisposable {
        readonly object gate=new object();
        readonly Timer timer;
        readonly Action<Exception> failed;
        Settings pending;
        bool writing,disposed,errorReported;

        internal SettingsPersistence(Action<Exception> failed) {
            this.failed=failed;
            timer=new Timer(WritePending,null,Timeout.Infinite,Timeout.Infinite);
        }
        internal void Schedule(Settings snapshot) {
            lock(gate) {
                if(disposed) return;
                pending=snapshot;
                timer.Change(180,Timeout.Infinite);
            }
        }
        void WritePending(object state) {
            Settings snapshot;
            lock(gate) {
                if(disposed || writing || pending==null) return;
                snapshot=pending; pending=null; writing=true;
            }
            Exception error=Write(snapshot);
            bool report=false;
            lock(gate) {
                bool newerPending=pending!=null;
                // Do not retry a failed write forever. Keep its snapshot for a
                // subsequent user change or the final flush instead.
                if(error!=null && pending==null) pending=snapshot;
                writing=false;
                if(error==null) errorReported=false;
                else if(!errorReported) {errorReported=true; report=true;}
                if(!disposed && newerPending) timer.Change(180,Timeout.Infinite);
                Monitor.PulseAll(gate);
            }
            if(report && failed!=null) failed(error);
        }
        static Exception Write(Settings snapshot) {
            try {snapshot.Save(); return null;}
            catch(Exception error) {return error;}
        }
        public void Dispose() {
            Settings snapshot;
            lock(gate) {
                if(disposed) return;
                disposed=true;
                timer.Change(Timeout.Infinite,Timeout.Infinite);
                while(writing) Monitor.Wait(gate);
                snapshot=pending; pending=null;
            }
            timer.Dispose();
            if(snapshot!=null) {
                // Application shutdown has already ended user interaction.
                // A synchronous final write prevents dropping the last edit.
                Exception error=Write(snapshot);
                if(error!=null) throw error;
            }
        }
    }
}
