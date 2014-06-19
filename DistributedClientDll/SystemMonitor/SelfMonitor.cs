using System;
using System.Diagnostics;
using System.IO;
using DistributedClientDll.Networking;
using DistributedShared.Network.Messages;
using DistributedShared.SystemMonitor;
using DistributedShared.SystemMonitor.Managers;

namespace DistributedClientDll.SystemMonitor
{
    public class SelfMonitor : IDisposable
    {
        private readonly HandleMontior _monitor;
        private readonly ConnectionManager _connection;

        public SelfMonitor(ConnectionManager connection)
        {
            _connection = connection;
            _monitor = new HandleMontior();
            _monitor.FileOpened += FileOpened;
            _monitor.NetworkConnection += NetworkAccess;
            _monitor.ProcessCreated += ProcessCreated;
            _monitor.ThreadCreated += ThreadCreated;
        }

        private void FileOpened(FileInfo file)
        {}

        private void ThreadCreated(ProcessThread thread)
        {
            if (StaticThreadManager.Instance.IsValidThread(thread))
                return;
            StaticThreadManager.Instance.KillAllThreads();

            _connection.SendMessage(new ClientSecurityEmergency {ThreadCreated = true});
        }

        private void NetworkAccess(OpenHandle.PortInfo portData)
        {}

        private void ProcessCreated(Process process)
        {
            StaticThreadManager.Instance.KillAllThreads();
            process.Kill();

            _connection.SendMessage(new ClientSecurityEmergency {ProcessName = process.ProcessName});
            
            Process.GetCurrentProcess().Kill();
        }

        public void Dispose()
        {
            _monitor.Dispose();
        }
    }
}
