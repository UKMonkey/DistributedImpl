using System;
using DistributedServerInterfaces.Networking;
using DistributedShared.Network.Messages;
using DistributedSharedInterfaces.Messages;
using DistributedSharedInterfaces.Network;

namespace DistributedServerDll.SystemMonitor.DllMonitoring
{
    public class DllManager
    {
        private readonly IConnectionManager _conectionManager;
        private readonly ServerDllMonitor _dllMonitor;
        private readonly ClientDirectoryMonitor _clientDllMonitor;

        public DllManager(ServerDllMonitor dllMonitor, 
                          IConnectionManager connectionManager,
                          string clientDllLibraryUpdates,
                          string clientDllLibraryInUse)
        {
            dllMonitor.DllLoaded += RegisterForCancelWork;
            dllMonitor.DllLoaded += SendAllClientsNewDll;

            _conectionManager = connectionManager;
            _dllMonitor = dllMonitor;

            connectionManager.NewConnectionMade += SendExistingDllMds;
            connectionManager.RegisterMessageListener(typeof(ClientDllRequestMessage), HandleClientDllRequest);

            _clientDllMonitor = new ClientDirectoryMonitor(clientDllLibraryUpdates, dllMonitor, clientDllLibraryInUse);
            _clientDllMonitor.FileUpdated += SendAllClientsNewDll;
            _clientDllMonitor.StartMonitoring();
        }


        private void RegisterForCancelWork(String dll)
        {
            var loadedDll = _dllMonitor.GetLoadedDll(dll);
            if (loadedDll == null)
                return;

            loadedDll.ProcessTerminatedGracefully   += p => CancelAllClientWork(dll);
            loadedDll.ProcessTerminatedUnexpectedly += p => CancelAllClientWork(dll);
        }


        private void SendAllClientsNewDll(String dll)
        {
            //var msg = new ServerDllMd5Message();

            //msg.DllNames.Add(dll);
            //msg.Md5Values.Add(_clientDllMonitor.GetFileMd5(dll));

            //_conectionManager.SendMessageToAll(msg);
        }


        private void CancelAllClientWork(String dll)
        {
            _conectionManager.SendMessageToAll(new ServerCancelWorkMessage {DllName = dll});
        }


        private void SendExistingDllMds(IConnection newCon)
        {
            var msg = new ServerDllMd5Message();

            // this prevents the monitor attempting to move files while we're working!
            lock (_clientDllMonitor)
            {
                foreach (var availableDll in _clientDllMonitor.GetAvailableFiles())
                {
                    msg.DllNames.Add(availableDll);
                    msg.Md5Values.Add(_clientDllMonitor.GetFileMd5(availableDll));
                }
            }

            _conectionManager.SendMessage(newCon, msg);
        }

        private void SendClientDll(IConnection con, string dll)
        {
            byte[] content;
            lock (_clientDllMonitor)
            {
                content = _clientDllMonitor.GetFileContent(dll);
            }

            _conectionManager.SendMessage(con, new ServerDllContentMessage { DllName = dll, Content = content });
        }

        private void HandleClientDllRequest(IConnection con, Message data)
        {
            var msg = (ClientDllRequestMessage) data;
            SendClientDll(con, msg.DllName);
        }
    }
}
