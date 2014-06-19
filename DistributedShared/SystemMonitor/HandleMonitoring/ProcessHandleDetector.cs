using System;
using System.Collections.Generic;

using System.Diagnostics;
using System.Management;

namespace DistributedShared.SystemMonitor.HandleMonitoring
{
    public class ProcessHandleDetector
    {
        public IEnumerable<OpenHandle> GetProcessHandleInformation(int processId = 0)
        {
            if (processId == 0)
                processId = Process.GetCurrentProcess().Id;

            var mos = new ManagementObjectSearcher(String.Format("Select * From Win32_Process Where ParentProcessID={0}", processId));

            foreach (ManagementObject mo in mos.Get())
            {
                var process = Process.GetProcessById(Convert.ToInt32(mo["ProcessID"]));
                yield return new OpenHandle(process);
                foreach (var item in GetProcessHandleInformation(process.Id))
                    yield return item;
            }
        }
    }
}
