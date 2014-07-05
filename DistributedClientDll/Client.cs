using System;
using DistributedClientDll.Networking;
using DistributedClientDll.SystemMonitor;
using DistributedClientDll.Jobs;
using DistributedClientInterfaces.Interfaces;
using DistributedClientDll.SystemMonitor.DllMonitoring;
using DistributedShared.Network;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedClientShared.SystemMonitor.DllMonitoring.DllInteraction;

namespace DistributedClientDll
{
    public class Client : IDistributedClient
    {
        private readonly MessageManager _messageManager;

        private ConnectionManager _connectionManager;
        private ClientDllMonitor _dllMonitor;
        private ClientDirectoryMonitor _directoryMonitor;

        private DllProcessor _dllProcessor;
        private JobManager _jobManager;

        private bool _connected;


        public Client()
        {
            _connected = false;
            _messageManager = new MessageManager();
        }


        public HostDllCommunication GetCommunication()
        {
            return new HostDllCommunication(_messageManager, "Client");
        }


        public bool Connect(string hostName, int port, 
                            String clientTargetNewDirectory, String clientTargetWorkingDirectory,
                            String userName, String password)
        {
            lock (this)
            {
                Disconnect();

                _connectionManager = new ConnectionManager(hostName, port, _messageManager);
                _dllMonitor = new ClientDllMonitor(clientTargetWorkingDirectory, "DistributedClientWorker.exe", GetCommunication);
                _directoryMonitor = new ClientDirectoryMonitor(clientTargetNewDirectory, _dllMonitor, clientTargetWorkingDirectory);

                _dllProcessor = new DllProcessor(_dllMonitor, _connectionManager);
                _jobManager = new JobManager(_connectionManager, _dllMonitor);
                _dllMonitor.StartMonitoring();

                var _connected = _connectionManager.Connect(userName);
                if (!_connected)
                    return false;

                _jobManager.StartDoingJobs();

                return true;
            }
        }


        public void Disconnect()
        {
            if (!_connected)
                return;
            _connected = false;

                // garbage collector can clean up once we've set these to null
                // not particularly clean, but will do for now
            _connectionManager = null;
            _dllMonitor = null;
            _dllProcessor = null;
            _jobManager = null;

            GC.Collect();
        }
    }
}
