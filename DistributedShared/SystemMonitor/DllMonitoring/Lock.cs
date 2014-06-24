using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DistributedShared.SystemMonitor.DllMonitoring
{
    class Lock : IDisposable
    {
        private Mutex _item;
        public readonly bool Valid;

        public Lock(Mutex item, int waitTime = -1)
        {
            if (waitTime > 0)
                Valid = item.WaitOne(50);
            else
                item.WaitOne();
            _item = item;
        }


        public void Dispose()
        {
            _item.ReleaseMutex();
        }
    }
}
