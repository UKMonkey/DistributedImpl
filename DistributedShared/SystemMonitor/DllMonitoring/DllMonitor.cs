using System;
using System.Collections.Generic;
using System.IO;

namespace DistributedShared.SystemMonitor.DllMonitoring
{
    public delegate void DllCallback(string dllName);
    public delegate void DllSecurityCallback(string dllName, StopReason issue);

    /************************************************************************/
    /* Note that this doesn't actually load any dlls, but generates a class */
    /* that provides an easy way to generate some shared memory, and run    */
    /* an application that will get the full dll path as the argument       */
    /* This class neither knows nor cares about how that shared memory is   */
    /* implemented, it just monitors dlls and notifies them when to stop    */
    /* so that they can be replaced                                         */
    /************************************************************************/
    public class DllMonitor<SharedMemoryType> : DirectoryMonitor
        where SharedMemoryType : DllSharedMemory, new()
    {
        private readonly String _exeName;
        private readonly String _sharedMemoryPath;

        private readonly Dictionary<String, DllWrapper<SharedMemoryType>> _loadedDlls = new Dictionary<string, DllWrapper<SharedMemoryType>>();
        private readonly HashSet<String> _dllsToRestart = new HashSet<string>();


        public event DllCallback DllLoaded;
        public event DllCallback DllUnavailable;
        public event DllCallback DllDeleted;
        public event DllSecurityCallback DllSecurityException;


        public DllMonitor(String targetWorkingDirectory, String exeName, String sharedMemoryPath)
            : base(targetWorkingDirectory, DllHelper.GetDllExtension())
        {
            _exeName = exeName;
            _sharedMemoryPath = sharedMemoryPath;
        }


        /// <summary>
        /// Gets a list of dll names that are currently wrapped.  Note that they may change immediately as this is not updated
        /// </summary>
        /// <returns></returns>
        public HashSet<String> GetAvailableDlls()
        {
            HashSet<String> ret;
            lock (this)
            {
                ret = new HashSet<String>();
                foreach (var item in _loadedDlls.Keys)
                    ret.Add(item);
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
            DllWrapper<SharedMemoryType> ret;
            lock (this)
            {
                ret = _loadedDlls.ContainsKey(dll)
                    ? _loadedDlls[dll] : null;
            }

            if (ret == null && DllUnavailable != null)
                DllUnavailable(dll);

            return ret;
        }

        /// <summary>
        /// nicely kills the process with the loaded dll
        /// it will finish any jobs that are left to do and tidy everything up.
        /// </summary>
        /// <param name="dllName"></param>
        public void UnloadDll(string dllName)
        {
            lock (this)
            {
                if (!_loadedDlls.ContainsKey(dllName))
                    return;

                _dllsToRestart.Remove(dllName);
                var deadBin = _loadedDlls[dllName];
                deadBin.StopExe();
            }
        }


        /// <summary>
        /// kill the process without hesition.  Make it die, make it die now.
        /// </summary>
        /// <param name="dllName"></param>
        public void ForceUnloadDll(string dllName)
        {
            lock (this)
            {
                if (!_loadedDlls.ContainsKey(dllName))
                    return;

                _dllsToRestart.Remove(dllName);
                var deadBin = _loadedDlls[dllName];
                deadBin.ForceStopExe();
            }
        }


        /// <summary>
        /// Attempts to delete the given file; returns false if the file wsan't loaded
        /// this will always trigger the DLLDeleted event at some point
        /// </summary>
        /// <param name="dllName"></param>
        public void DeleteDll(string dllName)
        {
            bool deleteFile = false;
            lock (this)
            {
                deleteFile = _loadedDlls.ContainsKey(dllName);
                if (!deleteFile)
                {
                    var deadBin = _loadedDlls[dllName];
                    if (deadBin.IsRunning())
                    {
                        deadBin.ProcessTerminatedGracefully += DeleteFile;
                        deadBin.ProcessTerminatedUnexpectedly += DeleteFile;
                        UnloadDll(dllName);
                    }
                    else
                    {
                        deleteFile = true;
                    }
                }
            }

            if (deleteFile)
                DeleteFile(dllName);
        }


        /// <summary>
        /// Deletes a given monitored file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="move"></param>
        private void DeleteFile(String dllName)
        {
            lock (this)
            {
                File.Delete(Path.Combine(FolderToMonitor, dllName));
                RegisterRemovedFile(dllName);
            }

            DllDeleted(dllName);
        }


        /// <summary>
        /// Cleans out the knowledge of the given file, ensuring that if it still exists
        /// when the monitor next loops that the "ProcessFile" is called on it.
        /// </summary>
        /// <param name="fileName"></param>
        protected override void RegisterRemovedFile(String fileName)
        {
            base.RegisterRemovedFile(fileName);
            _dllsToRestart.Remove(fileName);
        }


        /// <summary>
        /// Called when a new file is found in the directory
        /// </summary>
        /// <param name="fullFileName"></param>
        /// <param name="fileName"></param>
        protected override void ProcessFile(String fullFileName, String fileName)
        {
            var wrapper = new DllWrapper<SharedMemoryType>(FolderToMonitor, _sharedMemoryPath, fileName);
            wrapper.ProcessTerminatedUnexpectedly += RestartDllIfOk;

            _loadedDlls.Add(fileName, wrapper);
            _dllsToRestart.Add(fileName);

            wrapper.StartExe(_exeName);

            DllLoaded(fileName);
        }


        /// <summary>
        /// 
        /// </summary>
        private void RestartDllIfOk(String dllName)
        {
            lock (this)
            {
                if (!_loadedDlls.ContainsKey(dllName))
                    return;

                var deadBin = _loadedDlls[dllName];
                if (!_dllsToRestart.Contains(dllName))
                    return;

                switch (deadBin.SharedMemory.StopReason)
                {
                    case StopReason.FileSecurityException:
                    case StopReason.ProcessSecurityException:
                    case StopReason.PortSecurityException:
                    case StopReason.ThreadHandleExecption:
                        // by not regestering it as removed, the file will not get reloaded
                        DllSecurityException(dllName, deadBin.SharedMemory.StopReason);
                        return;
                }

                RegisterRemovedFile(dllName);
            }            
        }
    }
}
