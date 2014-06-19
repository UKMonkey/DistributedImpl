using System;
using DistributedServerDll.Jobs;
using DistributedServerDll.SystemMonitor;
using DistributedServerInterfaces.Networking;
using DistributedServerDll.Networking;
using DistributedShared.SystemMonitor;
using DistributedShared.Network;
using DistributedServerInterfaces.Interfaces;

namespace DistributedServerDll
{
    public class Server : IDistributedServer
    {
        private DllMonitor _dllMonitor;
        private DllManager _dataManager;
        private IConnectionManager _connectionManager;
        private MessageManager _messageManager;
        private JobManager _jobManager;
        private ClientSecurityManager _netSecurityManager;

        private bool _listening = false;


        public Server()
        {
        }


        public void StartListening(String serverTargetNewDirectory, String serverTargetWorkingDirectory,
                    String clientTargetNewDirectory, String clientTargetWorkingDirectory,
                    int port)
        {
            if (_listening)
                return;

            _dllMonitor = new DllMonitor(serverTargetNewDirectory, serverTargetWorkingDirectory);
            _messageManager = new MessageManager(_dllMonitor);
            _connectionManager = new ConnectionManager(_messageManager);

            _dataManager = new DllManager(_dllMonitor, _connectionManager,
                clientTargetNewDirectory, clientTargetWorkingDirectory);

            _jobManager = new JobManager(_dllMonitor, _connectionManager);
            _netSecurityManager = new ClientSecurityManager(_connectionManager);

            _dllMonitor.StartMonitoring();
            _connectionManager.ListenForConections(port);
        }

        public void StopListening()
        {
            _connectionManager.StopListening();

            _dllMonitor = null;
            _dataManager = null;
            _connectionManager = null;
            _messageManager = null;
            _jobManager = null;
            _netSecurityManager = null;

            System.GC.Collect();
        }
    }
}
