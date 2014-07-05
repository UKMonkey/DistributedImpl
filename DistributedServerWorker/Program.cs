using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using DistributedServerWorker.Jobs;
using DistributedShared.Network;
using System.Threading;
using DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedShared.SystemMonitor;
using DistributedServerInterfaces.Interfaces;
using DistributedServerWorker.SystemMonitor;

namespace DistributedServerWorker
{
    public class Program : IServerApi, IDisposable
    {
        private volatile bool _doWork = true;

        private readonly String _dllPath;
        private readonly String _dllName;

        private readonly MessageManager _messageManager = new MessageManager();
        private readonly WorkerDllCommunication _communication;
        private readonly SecurityHandler _security;

        private readonly JobsHandler _JobCollector;
        private readonly EventWaitHandle _waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        private Assembly _dllLoaded;
        private IDllApi _dllApi;

        public Program(String dllPath, String dllName)
        {
            _dllPath = dllPath;
            _dllName = dllName;

            _communication = new WorkerDllCommunication(_messageManager, "Server") { DllName = _dllName };
            _communication.StopRequired += TerminateProgram;
            _JobCollector = new JobsHandler(_communication, _dllName);

            _communication.Connect();

            _security = new SecurityHandler(_communication);
            _security.RegisterValidDirectory(Path.GetDirectoryName(dllPath));
            _security.RegisterValidDirectory(@"C:\Windows");

            _dllLoaded = DllHelper.LoadDll(dllPath);
            _dllApi = DllHelper.GetNewTypeFromDll<IDllApi>(_dllLoaded);
            _JobCollector.SetDllApi(_dllApi);
        }


        public void DoWork()
        {
            while (_doWork)
            {
                _waitHandle.Reset();
                var didWork = false;
                
                didWork = _JobCollector.DoNewJobsWork();

                while (_doWork && _JobCollector.DoProcessResultsWork())
                    didWork = true;

                if (!didWork)
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
            Console.WriteLine("Started serverworker");

            var dllName = args[0];
            var fullDllPath = args[1];

            var worker = new Program(fullDllPath, dllName);
            worker.DoWork();
            worker.Dispose();
        }
    }
}
