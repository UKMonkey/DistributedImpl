using System.Collections.Generic;
using System.Linq;

using System.Diagnostics;

namespace DistributedShared.SystemMonitor.HandleMonitoring
{
    public class ThreadHandleDetector
    {
        public IEnumerable<OpenHandle> GetThreadHandleInformation()
        {
            return Process.GetCurrentProcess().Threads.Cast<ProcessThread>().Select(item => new OpenHandle(item));
        }
    }
}
