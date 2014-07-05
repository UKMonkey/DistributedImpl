using System;
using DistributedServerDll.Jobs;
using DistributedServerInterfaces.Networking;
using DistributedServerDll.Networking;
using DistributedShared.Network;
using DistributedServerInterfaces.Interfaces;
using DistributedServerDll.SystemMonitor.DllMonitoring;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction;
using System.IO;

namespace DistributedServerDll
{
    public class Server : IDistributedServer
    {
        private ServerDllMonitor _dllMonitor;
        private ServerDirectoryMonitor _serverDllMonitor;

        private DllManager _dataManager;
        private IConnectionManager _connectionManager;
        private MessageManager _messageManager;
        private JobManager _jobManager;
        private ClientSecurityManager _netSecurityManager;

        private bool _listening = false;


        public Server()
        {
            _messageManager = new MessageManager();
        }


        private HostDllCommunication GetSharedMemory()
        {
            return new HostDllCommunication(_messageManager, "Server");
        }


        public void StartListening(String serverTargetNewDirectory, String serverTargetWorkingDirectory,
                    String clientTargetNewDirectory, String clientTargetWorkingDirectory,
                    String storePath,
                    int port)
        {
            if (_listening)
                return;

            _listening = true;

            _dllMonitor = new ServerDllMonitor(serverTargetWorkingDirectory, @"ServerDllWrapper.exe", GetSharedMemory);
            _serverDllMonitor = new ServerDirectoryMonitor(serverTargetNewDirectory, _dllMonitor, serverTargetWorkingDirectory);
            
            _connectionManager = new ConnectionManager(_messageManager);

            _dataManager = new DllManager(_dllMonitor, _connectionManager,
                clientTargetNewDirectory, clientTargetWorkingDirectory);

            _jobManager = new JobManager(_dllMonitor, _connectionManager, storePath);
            _netSecurityManager = new ClientSecurityManager(_connectionManager);

            _dllMonitor.StartMonitoring();
            _serverDllMonitor.StartMonitoring();
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

            GC.Collect();
        }
    }
}
