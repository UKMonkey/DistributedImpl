using System;
using System.Collections.Generic;
using System.Linq;
using DistributedShared.SystemMonitor.DllMonitoring;
using System.IO;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction;

namespace DistributedServerDll.SystemMonitor.DllMonitoring
{
    /// <summary>
    /// This class is responsible for moving 
    /// </summary>
    public class ServerDirectoryMonitor : DllUpdater<HostDllCommunication>
    {
        public ServerDirectoryMonitor(String targetDir, ServerDllMonitor dllMonitor, String targetCopyDir)
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
