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

namespace DistributedClientDll
{
    public class Client : IDistributedClient
    {
        private ConnectionManager _connectionManager;
        private DllMonitor _dllMonitor;

        private DllProcessor _dllProcessor;
        private JobManager _jobManager;
        private SelfMonitor _securityMonitor;

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

                _dllMonitor = new DllMonitor(clientTargetNewDirectory, clientTargetWorkingDirectory, "client");
                _connectionManager = new ConnectionManager(hostName, port, _dllMonitor);
                _dllProcessor = new DllProcessor(_dllMonitor, _connectionManager);

                _jobManager = new JobManager(_connectionManager, _dllMonitor);
                _securityMonitor = new SelfMonitor(_connectionManager);

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
            _securityMonitor = null;

            System.GC.Collect();
        }
    }
}
