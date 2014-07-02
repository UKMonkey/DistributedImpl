using System;
using DistributedShared.SystemMonitor.DllMonitoring;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;

namespace DistributedClientDll.SystemMonitor.DllMonitoring
{
    public class ClientDllMonitor : DllMonitor<ClientDllCommunication>
    {
        public ClientDllMonitor(String targetWorkingDir, String exeName, GetSharedMemoryDelegate sharedMemory)
            :base(targetWorkingDir, exeName, sharedMemory)
        {
        }
    }
}
