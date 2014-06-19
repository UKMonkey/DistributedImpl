using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using DistributedServerInterfaces.Networking;
using DistributedShared.Stream;
using DistributedShared.SystemMonitor.Managers;
using DistributedSharedInterfaces.Messages;
using DistributedShared.Network.Messages;
using System.Threading.Tasks;
using DistributedShared.Network;

using MessageCallback=DistributedServerInterfaces.Networking.MessageCallback;
using DistributedSharedInterfaces.Network;


namespace DistributedServerDll.Networking
{

    public class ConnectionManager : IConnectionManager
    {
        private readonly List<IConnection> _connections = new List<IConnection>();

        private readonly HashSet<Type> _messagesBypassingSecurity = new HashSet<Type>(); 
        private readonly Dictionary<Type, List<MessageCallback>> _messageCallbackHandlers = new Dictionary<Type, List<MessageCallback>>();
        private readonly Thread _dataMonitor;
        private readonly MessageManager _messageManager;

        public event ConnectionCallback NewConnectionMade;
        public event ConnectionCallback ConnectionLost;

        public readonly MessageStatistics MessageStatistics;

        private volatile bool _performListening;


        /**
         * By default all messages will be rejected unless the connection is marked as fully established.
         */
        public ConnectionManager(MessageManager messageManager)
        {
            _dataMonitor = new Thread(MonitorClientsMain);
            StaticThreadManager.Instance.StartNewThread(_dataMonitor, "ConnectionDataManager");

            _messageManager = messageManager;
            MessageStatistics = new MessageStatistics(messageManager);
        }


        public void RegisterMessageBypassingSecurity(Type msgType)
        {
            _messagesBypassingSecurity.Add(msgType);
        }


        private void ProcessPendingData()
        {
            var clientsToRemove = new List<IConnection>();

            foreach (var item in _connections)
            {
                if (item.Socket.Poll(1, SelectMode.SelectError))
                {
                    clientsToRemove.Add(item);
                }
                else if (item.Socket.Poll(1, SelectMode.SelectRead))
                {
                    try
                    {
                        HandleSocketData(item);
                    }
                    catch (System.Exception)
                    {
                        item.Socket.Close();
                        clientsToRemove.Add(item);
                    }
                }
            }

            foreach (var toRemove in clientsToRemove)
            {
                _connections.Remove(toRemove);
                if (ConnectionLost != null)
                    ConnectionLost(toRemove);
            }
        }


        private void HandleSocketData(IConnection connection)
        {
            var dllId = connection.DataReader.ReadShort();
            var msgId = connection.DataReader.ReadShort();

            var msg = _messageManager.GetMessage(dllId, msgId);

            if (msg == null)
            {
                SendMessage(connection, new ServerErrorMessage
                                            {
                                                Error = "Unable to decode your last message",
                                                ErrorCode = ErrorValues.UnknownMessage
                                            });
                return;
            }

            msg.LoadFromStream(connection.DataReader);
            List<MessageCallback> callbacks;

            lock (_messageCallbackHandlers)
            {
                if (_messageCallbackHandlers.ContainsKey(msg.GetType()))
                    callbacks = _messageCallbackHandlers[msg.GetType()];
                else
                    callbacks = new List<MessageCallback>();
            }

            MessageStatistics.MessageReceived(msg);
            var process = _messagesBypassingSecurity.Contains(msg.GetType());
            var nowFullyEstablished = connection.FullyEstablished;

            if (connection.FullyEstablished)
                process = true;

            if (!process)
                return;

            foreach (var callback in callbacks)
                callback(connection, msg);

            if (connection.FullyEstablished &&
                !nowFullyEstablished && 
                NewConnectionMade != null)
                NewConnectionMade(connection);
        }


        private void MonitorClientsMain()
        {
            while (true)
            {
                lock (_connections)
                {
                    ProcessPendingData();
                }
                Thread.Sleep(100);
            }
        }


        public void RegisterMessageListener(Type msgType, MessageCallback messageHandler)
        {
            lock (_messageCallbackHandlers)
            {
                if (!_messageCallbackHandlers.ContainsKey(msgType))
                    _messageCallbackHandlers.Add(msgType, new List<MessageCallback>());

                var data = _messageCallbackHandlers[msgType];
                data.Add(messageHandler);
            }
        }


        public void SendMessage(IConnection connection, Message msg)
        {
            SendMessage(connection, msg, false);
        }


        private void SendMessage(IConnection connection, Message msg, bool invalidateMessage)
        {
            lock (connection)
            {
                try
                {
                    try { }
                    finally
                    {
                        // this provides protection against ThreadAbort exceptions, allowing the thread
                        // to write everything to the stream and push it before the exception is raised.

                        var dllId = _messageManager.GetDllIdForMessage(msg);
                        var msgId = _messageManager.GetMessageId(msg);

                        connection.DataWriter.Write(dllId);
                        connection.DataWriter.Write(msgId);

                        msg.PushToStream(connection.DataWriter);
                        connection.DataWriter.Flush();
                        MessageStatistics.MessageSent(msg);
                    }
                }
                catch (Exception)
                {
                	// any issues sending data should be ignored as the only reason that this would happen
                    // is if the connection has been closed
                    // and there's already a thread that will deal with closed connections.
                    // thread abort exceptions will be re-thrown automatically after this so we can still force terminate
                    // a thread here.
                }

                if (invalidateMessage)
                    msg.ValidToSerialise = false;
            }
        }


        public void SendMessageToAll(Message msg)
        {
            lock (_connections)
            {
                Parallel.ForEach(_connections, con => SendMessage(con, msg, false));
            }
            msg.ValidToSerialise = false;
        }


        public void StopListening()
        {
            _performListening = false;
        }


        public void ListenForConections(int port)
        {
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            _performListening = true;

            while (_performListening)
            {
                lock (_connections)
                {
                    while (listener.Pending())
                    {
                        var newSoc = listener.AcceptSocket();
                        var newConnection = new Connection(newSoc);
                        _connections.Add(newConnection);
                    }
                }

                Thread.Sleep(100);
            }
        }
    }
}
