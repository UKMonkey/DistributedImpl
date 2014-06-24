using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using DistributedShared.SystemMonitor.Managers;

namespace DistributedShared.SystemMonitor.DllMonitoring
{
    public delegate void DllCallback(string dllName);

    /************************************************************************/
    /* Note that this doesn't actually load any dlls, but generates a class */
    /* that provides an easy way to generate some shared memory, and run    */
    /* an application that will get the full dll path as the argument       */
    /* This class neither knows nor cares about how that shared memory is   */
    /* implemented, it just monitors dlls and notifies them when to stop    */
    /* so that they can be replaced                                         */
    /************************************************************************/
    public class DllMonitor<SharedMemoryType>
        where SharedMemoryType : DllSharedMemory, new()
    {
        public readonly String FolderToMonitor;
        public readonly String DomainPrepend;
        protected readonly String FolderWithActive;

        private Thread _montoringThread;
        private volatile bool _performWork = true;

        private volatile bool _performDllLoads = true;
        public bool PerformDllLoads 
        {
            get { return _performDllLoads; } set { _performDllLoads = value; } 
        }

        private readonly String _exeName;
        private readonly List<String> _availableDlls = new List<string>();
        private readonly Dictionary<String, DllWrapper<SharedMemoryType>> _loadedDlls = new Dictionary<string, DllWrapper<SharedMemoryType>>();
        private readonly Dictionary<String, String> _dllToMd5 = new Dictionary<string, string>(); 

        public event DllCallback DllUnloaded;
        public event DllCallback DllLoaded;
        public event DllCallback DllUnavailable;

        public DllMonitor(String targetNewDirectory, String targetWorkingDirectory, String exeName)
        {
            FolderToMonitor = targetNewDirectory;
            FolderWithActive = targetWorkingDirectory;
            _exeName = exeName;

            Directory.CreateDirectory(targetNewDirectory);
            Directory.CreateDirectory(targetWorkingDirectory);
        }


        /// <summary>
        /// Gets a list of dll names that are currently wrapped.  Note that they may change immediately as this is not updated
        /// </summary>
        /// <returns></returns>
        public List<String> GetAvailableDlls()
        {
            var ret = new List<String>();
            lock (_loadedDlls)
            {
                ret.AddRange(_availableDlls);
            }
            return ret;
        }


        /// <summary>
        /// Returns a specific dll, if it's unavailable then listeners are notified that it was requested.
        /// </summary>
        /// <param name="dll"></param>
        /// <returns></returns>
        public virtual DllWrapper<SharedMemoryType> GetLoadedDll(string dll)
        {
            DllWrapper<SharedMemoryType> ret = null;
            lock (_loadedDlls)
            {
                ret = _loadedDlls.ContainsKey(dll)
                    ? _loadedDlls[dll] : null;
            }

            if (ret == null && DllUnavailable != null)
                DllUnavailable(dll);

            return ret;
        }


        /// <summary>
        /// Returns the MD5 of a specific dll
        /// </summary>
        /// <param name="dllName"></param>
        /// <returns></returns>
        public String GetDllMd5(String dllName)
        {
            lock (_loadedDlls)
            {
                if (_dllToMd5.ContainsKey(dllName))
                    return _dllToMd5[dllName];
                if (File.Exists(Path.Combine(FolderWithActive, dllName)))
                    return CalculateMD5(Path.Combine(FolderWithActive, dllName));
                return "";
            } 
        }


        /// <summary>
        /// Starts monitoring files for changes
        /// </summary>
        public void StartMonitoring()
        {
            ExamineFolder(FolderWithActive);
            _montoringThread = new Thread(MonitoringThreadMain);
            _performWork = true;
            StaticThreadManager.Instance.StartNewThread(_montoringThread, "DllMonitor");
        }


        /// <summary>
        /// Stops the monitoring process
        /// </summary>
        public void StopMonitoring()
        {
            if (_montoringThread != null)
            {
                _performWork = false;
                _montoringThread.Join();
                _montoringThread = null;
            }
        }


        /// <summary>
        /// Attempts to force load a dll, incase it hasn't been picked up already yet
        /// </summary>
        /// <param name="dllName"></param>
        public void UpdateSingleDll(string dllName)
        {
            lock (_loadedDlls)
            {
                var path = Path.Combine(FolderToMonitor, dllName);
                if (File.Exists(path))
                    LoadDll(path, true);
            }
        }


        /// <summary>
        /// Generates the md5 of a file
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream));
                }
            }
        }


        protected void UnloadDll(string dllName)
        {
            lock (_loadedDlls)
            {
                if (_loadedDlls.ContainsKey(dllName))
                {
                    if (DllUnloaded != null)
                        DllUnloaded(dllName);

                    var deadBin = _loadedDlls[dllName];
                    deadBin.ForceStopExe();
                }
            }
            GC.Collect();
        }


        public void DeleteDll(string dllName)
        {
            lock (_loadedDlls)
            {
                UnloadDll(dllName);
                File.Delete(Path.Combine(FolderWithActive, dllName));
            }
        }


        private AppDomainSetup DomainConfiguration()
        {
            var domaininfo = new AppDomainSetup();
            domaininfo.ApplicationBase = FolderWithActive;
            return domaininfo;
        }


        private AppDomain LoadDll(string fileName)
        {
            var domain = AppDomain.CreateDomain(DomainPrepend + fileName, AppDomain.CurrentDomain.Evidence, _domainInfo);
            var assemblyName = new AssemblyName { CodeBase = fileName };

            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler((x, y) => ResolveHandler(fileName));

            domain.Load(assemblyName);
            return domain;
        }


        private void LoadDll(String file, bool move)
        {
            var fileName = file.Substring(file.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            UnloadDll(fileName);

            if (move)
            {
                var newPath = Path.Combine(FolderWithActive, fileName);
                File.Delete(newPath);
                File.Move(file, newPath);
                file = newPath;
            }

            if (PerformDllLoads)
            {
                _loadedDlls.Add(fileName, LoadDll(file));
            }

            _dllToMd5.Add(fileName, CalculateMD5(file));

            lock (_availableDlls)
            {
                _availableDlls.Add(fileName);
            }

            if (DllLoaded != null)
                DllLoaded(fileName);
        }


        //http://support.microsoft.com/kb/837908/en-us
        //private Assembly ResolveHandler(object sender, ResolveEventArgs args)
        private Assembly ResolveHandler(string dllName)
        {
            return Assembly.LoadFrom(dllName);
        }


        private void ExamineFolder(string folderToExamine)
        {
            var files = Directory.EnumerateFiles(folderToExamine, "*.dll");
            var performMove = FolderWithActive != folderToExamine;

            foreach (var file in files)
            {
                lock (_loadedDlls)
                {
                    LoadDll(file, performMove);
                }
            }
        }


        private void MonitoringThreadMain()
        {
            while (_performWork)
            {
                ExamineFolder(FolderToMonitor);
                Thread.Sleep(1000);
            }
        }


        public byte[] GetDllContent(string dll)
        {
            lock (_loadedDlls)
            {
                var fullPath = Path.Combine(FolderWithActive, dll);
                return File.ReadAllBytes(fullPath);
            }
        }
    }
}
