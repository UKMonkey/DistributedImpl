using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
//using DistributedClientWorker.Jobs;
using DistributedShared.Network;
using System.Threading;
using DistributedShared.SystemMonitor;
using DistributedClientInterfaces.Interfaces;
using DistributedClientWorker.SystemMonitor;
using DistributedClientShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedClientWorker.Wrappers;

namespace DistributedClientWorker
{
    public class Program : IClientApi, IDisposable
    {
        private volatile bool _doWork = true;

        private readonly String _dllPath;
        private readonly String _dllName;

        private readonly MessageManager _messageManager = new MessageManager();
        private readonly WorkerDllCommunication _communication;
        private readonly SecurityHandler _security;

        private readonly DllWorker _jobWorker;
        private readonly EventWaitHandle _waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        private Assembly _dllLoaded;
        private IDllApi _dllApi;

        public Program(String dllPath, String dllName)
        {
            _dllPath = dllPath;
            _dllName = dllName;

            Debugger.Launch();

            _communication = new WorkerDllCommunication(_messageManager, "Client") { DllName = _dllName };
            _communication.StopRequired += TerminateProgram;

            _communication.Connect();

            _security = new SecurityHandler(_communication);
            _security.RegisterValidDirectory(Path.GetDirectoryName(dllPath));
            _security.RegisterValidDirectory(@"C:\Windows");

            _dllLoaded = DllHelper.LoadDll(dllPath);
            _dllApi = DllHelper.GetNewTypeFromDll<IDllApi>(_dllLoaded);
            _jobWorker = new DllWorker(_dllApi, _communication);
        }


        public void DoWork()
        {
            while (_doWork)
            {
               _waitHandle.WaitOne(500);
            }
        }


        public void Dispose()
        {
            _communication.Dispose();
            _security.Dispose();
            _dllLoaded = null;
            _dllApi = null;
            GC.Collect();
        }


        private void TerminateProgram()
        {
            _doWork = false;
            _waitHandle.Set();
        }


        static void Main(string[] args)
        {
            Console.WriteLine("Started client worker");

            var dllName = args[0];
            var fullDllPath = args[1];

            var worker = new Program(fullDllPath, dllName);
            worker.DoWork();
            worker.Dispose();
        }
    }
}
