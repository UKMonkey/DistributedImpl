using System;
using DistributedShared.SystemMonitor.DllMonitoring;

namespace DistributedClientDll.SystemMonitor.DllMonitoring
{
    public class ClientDllMonitor : DllMonitor<ClientSharedMemory>
    {
        public ClientDllMonitor(String targetWorkingDir, String exeName, String sharedMemoryPath)
            :base(targetWorkingDir, exeName, sharedMemoryPath)
        {
        }
    }
}
