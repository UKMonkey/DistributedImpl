using System;
using System.Collections.Generic;
using System.Linq;
using DistributedShared.SystemMonitor.DllMonitoring;
using System.IO;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction;

namespace DistributedServerDll.SystemMonitor.DllMonitoring
{
    public class ClientDirectoryMonitor : DllUpdater<HostDllCommunication>
    {
        public ClientDirectoryMonitor(String targetDir, ServerDllMonitor dllMonitor, String targetCopyDir)
            : base(targetDir, dllMonitor, targetCopyDir)
        {
            Console.WriteLine("Monitoring " + targetDir + " for new server dlls to use");
        }


        protected override void ProcessFile(String fullFileName, String fileName)
        {
        }


        public IEnumerable<String> GetAvailableFiles()
        {
            var files = Directory.EnumerateFiles(FolderToMonitor, ExtensionToMonitor).ToList();
            return files;
        }
    }
}
