using System;
using DistributedShared.SystemMonitor.DllMonitoring;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedClientShared.SystemMonitor.DllMonitoring.DllInteraction;

namespace DistributedClientDll.SystemMonitor.DllMonitoring
{
    public class ClientDllMonitor : DllMonitor<HostDllCommunication>
    {
        public ClientDllMonitor(String targetWorkingDir, String exeName, GetSharedMemoryDelegate getSharedMemory)
            : base(targetWorkingDir, exeName, getSharedMemory)
        {
        }
    }
}
