using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace DistributedServer.DllMonitor
{
    public delegate void DllCallback(string dllName);

    public class Monitor
    {
        private readonly String _folderToMonitor;
        private readonly String _folderWithActive;

        private Thread _montoringThread;
        private bool _performWork = true;

        private readonly Dictionary<String, AppDomain> _loadedAsseblies = new Dictionary<string, AppDomain>();
        public event DllCallback DllUnloaded;
        public event DllCallback DllLoaded;

        public Monitor(String targetNewDirectory, String targetWorkingDirectory)
        {
            _folderToMonitor = targetNewDirectory;
            _folderWithActive = targetWorkingDirectory;
            ExamineFolder(_folderWithActive);
        }

        public Dictionary<String, AppDomain> GetLoadedDlls()
        {
            return _loadedAsseblies;
        }


        public void StartMonitoring()
        {
            _montoringThread = new Thread(MonitoringThreadMain);
            _performWork = true;
            _montoringThread.Start();
        }


        public void StopMonitoring()
        {
            if (_montoringThread != null)
            {
                _performWork = false;
                _montoringThread.Join();
                _montoringThread = null;
            }
        }


        private void LoadDll(String file)
        {
            var fileName = file.Substring(_folderToMonitor.Length + 1);
            if (_loadedAsseblies.ContainsKey(fileName))
            {
                if (DllUnloaded != null)
                    DllUnloaded(fileName);

                var deadDomain = _loadedAsseblies[fileName];
                AppDomain.Unload(deadDomain);
                _loadedAsseblies.Remove(fileName);
            }

            var domain = AppDomain.CreateDomain(fileName);
            var assemblyName = new AssemblyName { CodeBase = file };

            domain.Load(assemblyName);
            _loadedAsseblies.Add(fileName, domain);

            if (DllLoaded != null)
                DllLoaded(fileName);
        }


        private void ExamineFolder(string folderToExamine)
        {
            var files = Directory.EnumerateFiles(folderToExamine, "*.dll");
            foreach (var file in files)
            {
                lock (_loadedAsseblies)
                {
                    LoadDll(file);
                }
            }
        }


        private void MonitoringThreadMain()
        {
            while (_performWork)
            {
                ExamineFolder(_folderToMonitor);
                Thread.Sleep(1000);
            }
        }
    }
}
