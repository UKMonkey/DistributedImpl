using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DistributedSharedInterfaces.Jobs;
using DistributedClientDll.Networking;
using DistributedClientDll.SystemMonitor;
using DistributedShared.SystemMonitor;
using DistributedClientDll.Jobs;
using DistributedClientInterfaces.Interfaces;
using DistributedClientDll.SystemMonitor.DllMonitoring;

namespace DistributedClientDll
{
    public class Client : IDistributedClient
    {
        private ConnectionManager _connectionManager;
        private ClientDllMonitor _dllMonitor;

        private DllProcessor _dllProcessor;
        private JobManager _jobManager;

        private bool connected = false;


        public Client()
        {
        }


        public bool Connect(string hostName, int port, 
                            String clientTargetNewDirectory, String clientTargetWorkingDirectory,
                            String userName, String password)
        {
            lock (this)
            {
                Disconnect();

                _dllMonitor = new ClientDllMonitor(clientTargetNewDirectory, clientTargetWorkingDirectory, "client");
                _connectionManager = new ConnectionManager(hostName, port);
                _dllProcessor = new DllProcessor(_dllMonitor, _connectionManager);

                _jobManager = new JobManager(_connectionManager, _dllMonitor);

                var success = _connectionManager.Connect(userName);
                if (!success)
                    return false;

                connected = true;

                _dllMonitor.StartMonitoring();
                _jobManager.StartDoingJobs();

                return true;
            }
        }


        public void Disconnect()
        {
            if (!connected)
                return;
            connected = false;

                // garbage collector can clean up once we've set these to null
                // not particularly clean, but will do for now
            _connectionManager = null;
            _dllMonitor = null;
            _dllProcessor = null;
            _jobManager = null;

            System.GC.Collect();
        }
    }
}
