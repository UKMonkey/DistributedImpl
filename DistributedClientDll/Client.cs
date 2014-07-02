using System;
using DistributedClientDll.Networking;
using DistributedClientDll.SystemMonitor;
using DistributedClientDll.Jobs;
using DistributedClientInterfaces.Interfaces;
using DistributedClientDll.SystemMonitor.DllMonitoring;
using DistributedShared.Network;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;

namespace DistributedClientDll
{
    public class Client : IDistributedClient
    {
        private readonly MessageManager _messageManager;

        private ConnectionManager _connectionManager;
        private ClientDllMonitor _dllMonitor;

        private DllProcessor _dllProcessor;
        private JobManager _jobManager;

        private bool _connected;


        public Client()
        {
            _connected = false;
            _messageManager = new MessageManager();
        }


        private ClientDllCommunication GetSharedMemory()
        {
            return new ClientDllCommunication(_messageManager);
        }


        public bool Connect(string hostName, int port, 
                            String clientTargetNewDirectory, String clientTargetWorkingDirectory,
                            String userName, String password)
        {
            lock (this)
            {
                Disconnect();

                _dllMonitor = new ClientDllMonitor(clientTargetNewDirectory, clientTargetWorkingDirectory, GetSharedMemory);
                _connectionManager = new ConnectionManager(hostName, port, _messageManager);
                _dllProcessor = new DllProcessor(_dllMonitor, _connectionManager);

                _jobManager = new JobManager(_connectionManager, _dllMonitor);

                var success = _connectionManager.Connect(userName);
                if (!success)
                    return false;

                _connected = true;

                _dllMonitor.StartMonitoring();
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
