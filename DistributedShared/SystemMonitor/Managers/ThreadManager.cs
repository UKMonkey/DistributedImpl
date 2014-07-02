using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using ThreadState = System.Threading.ThreadState;

namespace DistributedShared.SystemMonitor.Managers
{
    public class ThreadManager
    {
        private volatile bool _allowNewThreads = true;
        public bool AllowNewThreads { get { return _allowNewThreads; } set { _allowNewThreads = value; } }

        private readonly HashSet<Thread> _validThreads = new HashSet<Thread>();
        private readonly Dictionary<Thread, string> _tmp = new Dictionary<Thread, string>(); 

        public ThreadManager()
        {
            _validThreads.Add(Thread.CurrentThread);
        }


        public void StartNewThread(Thread thread, string name)
        {
            lock(this)
            {
                _validThreads.RemoveWhere(item => item.ThreadState == ThreadState.Stopped ||
                                                  item.ThreadState == ThreadState.Aborted);

                if (!AllowNewThreads)
                    return;

                thread.Name = name;

                thread.Start();
                _validThreads.Add(thread);

                _tmp.Add(thread, name);
            }
        }


        public bool IsValidThread(ProcessThread thread)
        {
            lock (this)
            {
                var item = _validThreads.FirstOrDefault(i => i.ManagedThreadId == thread.Id);
                return item != null;
            }
        }


        public void KillAllThreads()
        {
            lock (this)
            {
                AllowNewThreads = false;
            }

            foreach (var item in _validThreads)
                item.Abort();
        }
    }
}
