using System;
using System.Collections.Generic;
using System.Linq;
using DistributedShared.SystemMonitor.DllMonitoring;
using System.IO;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedClientShared.SystemMonitor.DllMonitoring.DllInteraction;

namespace DistributedClientDll.SystemMonitor.DllMonitoring
{
    /// <summary>
    /// This class is responsible for moving 
    /// </summary>
    public class ClientDirectoryMonitor : DllUpdater<HostDllCommunication>
    {
        public ClientDirectoryMonitor(String targetDir, ClientDllMonitor dllMonitor, String targetCopyDir)
            : base(targetDir, dllMonitor, targetCopyDir)
        {
            Console.WriteLine("Monitoring " + targetDir + " for new client dlls to use");
        }


        public IEnumerable<String> GetAvailableFiles()
        {
            var files = Directory.EnumerateFiles(FolderToMonitor, ExtensionToMonitor).ToList();
            return files;
        }
    }
}
