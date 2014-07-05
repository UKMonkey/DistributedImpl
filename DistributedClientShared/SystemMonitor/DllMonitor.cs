using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using DistributedShared.SystemMonitor.Managers;

namespace DistributedShared.SystemMonitor
{
    public delegate void DllCallback(string dllName);

    public class DllMonitor
    {
        public readonly String FolderToMonitor;
        public readonly String DomainPrepend;
        protected readonly String FolderWithActive;

        private Thread _montoringThread;
        private volatile bool _performWork = true;
        private readonly AppDomainSetup _domainInfo;

        private volatile bool _performDllLoads = true;
        public bool PerformDllLoads 
        {
            get { return _performDllLoads; } set { _performDllLoads = value; } 
        }

        private readonly List<String> _availableDlls = new List<string>();
        private readonly Dictionary<String, AppDomain> _loadedAsseblies = new Dictionary<string, AppDomain>();
        private readonly Dictionary<String, String> _dllToMd5 = new Dictionary<string, string>(); 

        public event DllCallback DllUnloaded;
        public event DllCallback DllLoaded;
        public event DllCallback DllUnavailable;

        public DllMonitor(String targetNewDirectory, String targetWorkingDirectory, String domainPrepend)
        {
            FolderToMonitor = targetNewDirectory;
            FolderWithActive = targetWorkingDirectory;

            Directory.CreateDirectory(targetNewDirectory);
            Directory.CreateDirectory(targetWorkingDirectory);

            DomainPrepend = domainPrepend;
            _domainInfo = DomainConfiguration();
        }


        public List<String> GetAvailableDlls()
        {
            var ret = new List<String>();
            lock (_loadedAsseblies)
            {
                ret.AddRange(_availableDlls);
            }
            return ret;
        }


        public virtual AppDomain GetLoadedDll(string dll)
        {
            AppDomain ret = null;
            lock (_loadedAsseblies)
            {
                ret = _loadedAsseblies.ContainsKey(dll) 
                    ? _loadedAsseblies[dll] : null;
            }

            if (ret == null && DllUnavailable != null)
                DllUnavailable(dll);

            return ret;
        }


        public String GetDllMd5(String dllName)
        {
            lock (_loadedAsseblies)
            {
                if (_dllToMd5.ContainsKey(dllName))
                    return _dllToMd5[dllName];
                if (File.Exists(Path.Combine(FolderWithActive, dllName)))
                    return CalculateMD5(Path.Combine(FolderWithActive, dllName));
                return "";
            } 
        }


        public void StartMonitoring()
        {
            ExamineFolder(FolderWithActive);
            _montoringThread = new Thread(MonitoringThreadMain);
            _performWork = true;
            StaticThreadManager.Instance.StartNewThread(_montoringThread, "DllMonitor");
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


        public void UpdateSingleDll(string dllName)
        {
            lock (_loadedAsseblies)
            {
                var path = Path.Combine(FolderToMonitor, dllName);
                if (File.Exists(path))
                    LoadDll(path, true);
            }
        }


        public T GetNewTypeFromDll<T>(string dll) where T : class
        {
            lock (_loadedAsseblies)
            {
                var domain = GetLoadedDll(dll);
                if (domain == null)
                    return null;

                var type = typeof(T);
                var typesMid = domain.GetAssemblies().
                    SelectMany(s => s.GetTypes()).
                    Where(p => p.IsClass).
                    Where(p => !p.FullName.StartsWith("System")).ToList();
                var types = typesMid.Where(type.IsAssignableFrom).ToList();

                if (types.Count == 0)
                    return null;
                if (types.Count > 1)
                    throw new Exception("Unable to load dll as the number of valid IDllApi items = " + types.Count);

                var constructor = types[0].GetConstructor(Type.EmptyTypes);
                Debug.Assert(constructor != null, "Dll Job Worker has no default constructor");
                return (T)constructor.Invoke(null);
            }
        }


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
            lock (_loadedAsseblies)
            {
                if (_loadedAsseblies.ContainsKey(dllName))
                {
                    if (DllUnloaded != null)
                        DllUnloaded(dllName);

                    var deadDomain = _loadedAsseblies[dllName];
                    AppDomain.Unload(deadDomain);
                    _loadedAsseblies.Remove(dllName);
                }
            }
            GC.Collect();
        }


        public void DeleteDll(string dllName)
        {
            lock (_loadedAsseblies)
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
                _loadedAsseblies.Add(fileName, LoadDll(file));
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
                lock (_loadedAsseblies)
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
            lock (_loadedAsseblies)
            {
                var fullPath = Path.Combine(FolderWithActive, dll);
                return File.ReadAllBytes(fullPath);
            }
        }
    }
}
