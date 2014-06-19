using System;
using System.Collections.Generic;
using System.IO;
using DistributedShared.Network.Messages;
using DistributedShared.SystemMonitor;
using DistributedSharedInterfaces.Messages;
using DistributedClientDll.Networking;

namespace DistributedClientDll.SystemMonitor
{
    public class DllProcessor
    {
        private readonly HashSet<String> _requestedDlls = new HashSet<string>();
        private readonly ConnectionManager _connectionManager;
        private readonly DllMonitor _dllMonitor;


        public DllProcessor(DllMonitor dllMonitor, ConnectionManager connection)
        {
            _connectionManager = connection;
            _connectionManager.RegisterMessageListener(typeof(ServerDllContentMessage), HandleDllContent);
            _connectionManager.RegisterMessageListener(typeof(ServerDllMd5Message), HandleDllMd5Data);
            _connectionManager.RegisterMessageListener(typeof(ServerUnrecognisedDllMessage), HandleUnknownDll);

            _dllMonitor = dllMonitor;
            dllMonitor.DllUnavailable += MakeDllDownloadRequest;
        }


        public void MakeDllDownloadRequest(string dll)
        {
            lock (_requestedDlls)
            {
                if (_requestedDlls.Contains(dll))
                    return;
                _requestedDlls.Add(dll);
            }

            var msg = new ClientDllRequestMessage {DllName = dll};
            _connectionManager.SendMessage(msg);
        }


        private void HandleDllContent(Message data)
        {
            var msg = (ServerDllContentMessage) data;
            var tempFile = Path.GetTempFileName();
            var stream = new FileStream(tempFile, FileMode.Truncate);

            stream.Write(msg.Content, 0, msg.Content.Length);
            stream.Flush();
            stream.Close();
            File.Move(tempFile, Path.Combine(_dllMonitor.FolderToMonitor, msg.DllName));
        }


        private void HandleDllMd5Data(Message data)
        {
            var msg = (ServerDllMd5Message) data;

            for (var i=0; i<msg.Count; ++i)
            {
                var dllName = msg.DllNames[i];
                var md5 = msg.Md5Values[i];
                var currentMd5 = _dllMonitor.GetDllMd5(dllName);

                if (currentMd5 == md5)
                    return;

                _dllMonitor.DeleteDll(dllName);
                _connectionManager.SendMessage(new ClientDllRequestMessage { DllName = dllName });
            }
        }


        private void HandleUnknownDll(Message data)
        {
            var msg = (ServerUnrecognisedDllMessage)data;
            _dllMonitor.DeleteDll(msg.DllName);
        }
    }
}
