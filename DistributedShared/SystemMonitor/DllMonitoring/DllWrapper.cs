using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.MemoryMappedFiles;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace DistributedShared.SystemMonitor.DllMonitoring
{
    public delegate void LoadedDllCallback(String dllName);

    public class DllWrapper<T> : IDisposable
        where T : DllSharedMemory, new()
    {
        public readonly T SharedMemory;
        private Process _process;

        public String DllName { get { return SharedMemory.DllName; } }
        private readonly String DllPath;

        // callback when the process died
        public event LoadedDllCallback ProcessTerminatedGracefully;
        public event LoadedDllCallback ProcessTerminatedUnexpectedly;


        public DllWrapper(String dllPath, String memoryPath, string dllName)
        {
            SharedMemory = new T() { DllName = dllName, SharedMemoryPath = memoryPath };
            DllPath = dllPath;

            SharedMemory.Connect(true);
        }


        public void Dispose()
        {
            ForceStopExe();
        }


        public void StartExe(String exeName)
        {
            if (_process != null)
                throw new ArgumentException("Exe had already been started");

            _process = new Process();

            // Configure the process using the StartInfo properties.
            _process.StartInfo.FileName = exeName;
            _process.StartInfo.Arguments = Path.Combine(DllPath, DllName) + " " + SharedMemory.SharedMemoryFile;
            _process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            _process.Exited += ProcessTerminated;

            _process.Start();
        }


        public bool IsRunning()
        {
            return !_process.HasExited;
        }


        public void StopExe()
        {
            SharedMemory.RequestStop(StopReason.Requested);
        }


        public void ForceStopExe()
        {
            _process.Kill();
        }


        private void ProcessTerminated(object sender, EventArgs e)
        {
            // make sure that the stop reason is up to date - just incase
            // there was a security exception and we mustn't restart
            SharedMemory.ForceReloadOfProtectedData();

            if (SharedMemory.StopRequested)
                ProcessTerminatedGracefully(DllName);
            else
                ProcessTerminatedUnexpectedly(DllName);
        }
    }
}
