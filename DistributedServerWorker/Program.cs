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

namespace DistributedServerWorker
{
    public class Program : IServerApi, IDisposable
    {
        private volatile bool _doWork = true;

        private readonly String _dllPath;
        private readonly String _dllName;

        private readonly MessageManager _messageManager = new MessageManager();
        private readonly WorkerDllCommunication _communication;

        private readonly JobCollector _JobCollector;
        private readonly HandleMontior _handleMonitor;

        private Assembly _dllLoaded;
        private IDllApi _dllApi;

        public Program(String dllPath, String dllName)
        {
            _dllPath = dllPath;
            _dllName = dllName;

            _communication = new WorkerDllCommunication(_messageManager) { DllName = _dllName };
            _handleMonitor = new HandleMontior();

            _handleMonitor.FileOpened += SecurityBreach;
            _handleMonitor.NetworkConnection += SecurityBreach;
            _handleMonitor.ProcessCreated += SecurityBreach;
            _handleMonitor.ThreadCreated += SecurityBreach;

            _dllLoaded = DllHelper.LoadDll(dllPath);
            _dllApi = DllHelper.GetNewTypeFromDll<IDllApi>(_dllLoaded);
            _JobCollector = new JobCollector(_communication, _dllApi);

            _communication.StopRequired += TerminateProgram;
            
            _communication.Connect();
        }


        public void Sleep()
        {
            while (_doWork)
            {
                Thread.Sleep(1000);
            }            
        }


        public void Dispose()
        {
            _communication.Dispose();
            _dllLoaded = null;
            _dllApi = null;
            GC.Collect();
        }


        private void SecurityBreach(FileInfo fileData)
        {
        }


        private void SecurityBreach(ProcessThread threadData)
        {
        }


        private void SecurityBreach(OpenHandle.PortInfo portData)
        {
        }


        private void SecurityBreach(Process processData)
        {
        }


        private void TerminateProgram()
        {
            _doWork = false;
        }


        static void Main(string[] args)
        {
            Console.WriteLine("Started serverworker");

            var dllName = args[0];
            var fullDllPath = args[1];

            var worker = new Program(fullDllPath, dllName);
            worker.Sleep();
            worker.Dispose();
        }
    }
}
