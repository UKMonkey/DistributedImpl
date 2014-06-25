using System;
using DistributedServerInterfaces.Networking;
using DistributedShared.Network.Messages;
using DistributedShared.SystemMonitor;
using DistributedSharedInterfaces.Messages;
using DistributedShared.Network;
using DistributedSharedInterfaces.Network;

namespace DistributedServerDll.SystemMonitor
{
    public class DllManager
    {
        private readonly IConnectionManager _conectionManager;
        private readonly DllMonitor _dllClientMonitor;

        public DllManager(DllMonitor dllMonitor, 
                          IConnectionManager connectionManager,
                          string clientDllLibraryUpdates,
                          string clientDllLibraryInUse)
        {
            dllMonitor.DllUnloaded += CancelAllClientWork;
            dllMonitor.DllLoaded += SendAllClientsNewDll;

            _conectionManager = connectionManager;

            connectionManager.NewConnectionMade += SendExistingDllMds;
            connectionManager.RegisterMessageListener(typeof(ClientDllRequestMessage), HandleClientDllRequest);

            _dllClientMonitor = new DllMonitor(clientDllLibraryUpdates, clientDllLibraryInUse, "serverClient") { PerformDllLoads = false };
            _dllClientMonitor.StartMonitoring();
        }


        private void SendAllClientsNewDll(String dll)
        {
            var msg = new ServerDllMd5Message();
            lock (_dllClientMonitor)
            {
                _dllClientMonitor.ForceUpdateDll(dll);
                msg.DllNames.Add(dll);
                msg.Md5Values.Add(_dllClientMonitor.GetDllMd5(dll));
            }

            _conectionManager.SendMessageToAll(msg);
        }


        private void CancelAllClientWork(String dll)
        {
            _conectionManager.SendMessageToAll(new ServerCancelWorkMessage {DllName = dll});
        }


        private void SendExistingDllMds(IConnection newCon)
        {
            var msg = new ServerDllMd5Message();
            lock (_dllClientMonitor)
            {
                foreach (var availableDll in _dllClientMonitor.GetAvailableDlls())
                {
                    msg.DllNames.Add(availableDll);
                    msg.Md5Values.Add(_dllClientMonitor.GetDllMd5(availableDll));
                }
            }

            _conectionManager.SendMessage(newCon, msg);
        }

        private void SendClientDll(IConnection con, string dll)
        {
            byte[] content;
            lock (_dllClientMonitor)
            {
                content = _dllClientMonitor.GetDllContent(dll);
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
