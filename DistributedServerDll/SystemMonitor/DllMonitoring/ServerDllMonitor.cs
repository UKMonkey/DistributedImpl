using System;
using DistributedShared.SystemMonitor.DllMonitoring;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction;

namespace DistributedServerDll.SystemMonitor.DllMonitoring
{
    public class ServerDllMonitor : DllMonitor<HostDllCommunication>
    {
        public ServerDllMonitor(String targetWorkingDir, String exeName, GetSharedMemoryDelegate sharedMemory)
            : base(targetWorkingDir, exeName, sharedMemory)
        {
            Console.WriteLine("Monitoring " + targetWorkingDir + " for server dlls to use");
        }
    }
}
