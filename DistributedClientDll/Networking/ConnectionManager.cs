using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DistributedShared.Network.Messages;
using DistributedShared.Stream;
using DistributedShared.SystemMonitor;
using DistributedShared.SystemMonitor.Managers;
using DistributedSharedInterfaces.Messages;
using DistributedShared.Network;
using DistributedSharedInterfaces.Network;

namespace DistributedClientDll.Networking
{
    public delegate void MessageCallback(Message data);

    public class ConnectionManager
    {
        private readonly Dictionary<Type, List<MessageCallback>> _messageCallbackHandlers = new Dictionary<Type, List<MessageCallback>>();
        private readonly TcpClient _client;
        private Connection _connection;

        private Thread _dataHandler;

        private readonly string _hostname;
        private readonly int _port;
        private readonly MessageManager _messageManager;
        public readonly MessageStatistics MessageStatistics;

        private volatile bool _performWork;
        private readonly AutoResetEvent _loginReplyEvent = new AutoResetEvent(false);
        private ServerLoginResult _loginReply = null;
        

        public ConnectionManager(string hostname, int port)
        {
            _client = new TcpClient();
            _performWork = true;

            _hostname = hostname;
            _port = port;
            _messageManager = new MessageManager();
            MessageStatistics = new MessageStatistics(_messageManager);
        }


        public bool Connect(string userName)
        {
            _client.Connect(_hostname, _port);

            var soc = _client.Client;
            _connection = new Connection(soc);
            RegisterMessageListener(typeof(ServerLoginResult), HandleLoginResult);

            _dataHandler = new Thread(DataHandlerMain);
            StaticThreadManager.Instance.StartNewThread(_dataHandler, "ConnectionDataHandler");

            var msg = new ClientLoginMessage {Username = userName};
            SendMessage(msg);

            // wait here until the server has replied to our request
            _loginReplyEvent.WaitOne(3000);
            if (_loginReply == null)
            {
                _dataHandler.Abort();
                _loginReply = new ServerLoginResult { Result = false, Message = "Login request timed out" };
            }

            return _loginReply.Result;
        }


        private void HandleLoginResult(Message data)
        {
            _loginReply = (ServerLoginResult)data;
            _loginReplyEvent.Set();
        }


        private void DataHandlerMain()
        {
            while (_performWork)
            {
                if (_connection.Socket.Poll(1, SelectMode.SelectError))
                {
                    _performWork = false;
                }
                else if (_connection.Socket.Poll(1, SelectMode.SelectRead))
                {
                    try
                    {
                        HandleSocketData(_connection);
                    }
                    catch (System.Exception ex)
                    {
                        _performWork = false;
                    }
                }
            }
        }


        private void HandleSocketData(IConnection connection)
        {
            var dllId = connection.DataReader.ReadShort();
            var msgId = connection.DataReader.ReadShort();

            var msg = _messageManager.GetMessage(dllId, msgId);

            if (msg == null)
                return;

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

            foreach (var callback in callbacks)
                callback(msg);
        }


        public void SendMessage(Message msg)
        {
            lock (_connection)
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

                        _connection.DataWriter.Write(dllId);
                        _connection.DataWriter.Write(msgId);

                        msg.PushToStream(_connection.DataWriter);
                        _connection.DataWriter.Flush();
                        MessageStatistics.MessageSent(msg);
                    }
                }
                catch (System.Exception ex)
                {
                	/// if this is because of a write exception
                    /// then do nothing because it'll be because the connection is about to be terminated.
                    /// if it's because it's a thread abort, then don't worry it'll be re-thrown later
                }


                msg.ValidToSerialise = false;
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
    }
}
