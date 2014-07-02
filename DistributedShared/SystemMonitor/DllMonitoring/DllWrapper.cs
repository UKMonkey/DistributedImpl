using System;
using System.IO;
using System.Diagnostics;

namespace DistributedShared.SystemMonitor.DllMonitoring
{
    public delegate void LoadedDllCallback(String dllName);

    public class DllWrapper<T> : IDisposable
        where T : DllCommunication
    {
        public readonly T SharedMemory;
        private Process _process;

        public String DllName { get { return SharedMemory.DllName; } }
        private readonly String _dllPath;

        // callback when the process died
        public event LoadedDllCallback ProcessTerminatedGracefully;
        public event LoadedDllCallback ProcessTerminatedUnexpectedly;


        public DllWrapper(String dllPath, string dllName, T sharedMemory)
        {
            SharedMemory = sharedMemory;
            sharedMemory.DllName = dllName;

            _dllPath = dllPath;
        }


        public void Dispose()
        {
            ForceStopExe();
            SharedMemory.Dispose();
        }


        public void StartExe(String exeName)
        {
            if (_process != null)
                throw new ArgumentException("Exe had already been started");

            try
            {
                _process = new Process();

                // Configure the process using the StartInfo properties.
                _process.StartInfo.FileName = exeName;
                _process.StartInfo.Arguments = DllName + " " + Path.Combine(_dllPath, DllName);
                //_process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                _process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

                _process.Exited += (a,b) => ProcessTerminated();
                _process.EnableRaisingEvents = true;

                SharedMemory.Connect(true);
                _process.Start();
            }
            catch
            {
                ProcessTerminated();
                throw;
            }
        }


        public bool IsRunning()
        {
            return !_process.HasExited;
        }


        public void StopExe()
        {
            SharedMemory.SendStopRequest();
        }


        public void ForceStopExe()
        {
            try
            {
                _process.Kill();
            }
            catch (System.Exception ex)
            {
                // exception means the process was already dead.  there's not much point in 
                // testing to see if the process was alive before because if it was it may have died in
                // between the test and the kill
            }            
        }


        private void ProcessTerminated()
        {
            if (SharedMemory.StopRequested)
                ProcessTerminatedGracefully(DllName);
            else
                ProcessTerminatedUnexpectedly(DllName);
        }
    }
}
