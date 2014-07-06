using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.IO.Pipes;
using DistributedSharedInterfaces.Messages;
using DistributedShared.Network;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction.Messages;
using DistributedShared.SystemMonitor.Managers;

namespace DistributedShared.SystemMonitor.DllMonitoring
{
    public delegate void DllCommunicationCallback(DllCommunication item);
    public delegate void DllMessageReceivedCallback(DllMessage item);


    public enum StopReason : short
    {
        Unknown = 0,
        Requested,
        FileSecurityException,      // process opened a file
        ProcessSecurityException,   // process started another process
        PortSecurityException,      // process opened a port
        ThreadHandleExecption,      // process started a thread
    }


    public class DllCommunication : IDisposable
    {
        private PipeStream _pipe;

        private IMessageInputStream _inputStream;
        private IMessageOutputStream _outputStream;

        private readonly Dictionary<Type, List<DllMessageReceivedCallback>> _messageHandlers;

        // TODO - throw if trying to set the DLL name or shared memory path if we've
        // already connected
        // TODO - requre this to be set before running
        public String DllName { get; set; }
        public String NamePrepend { get; protected set; }

        public StopReason StopReason { get; private set; }
        public bool StopRequested { get; private set; }
        public event DllCommunicationCallback PipeBroken;

        // Thread that will fire off the various events
        private Thread _monitor;
        private bool _waitForConnection;
        private volatile bool _montiorFile;

        // We're sending / receiving messages - code reuse hurrah!
        private readonly MessageManager _msgManager;


        public DllCommunication(MessageManager msgManager)
        {
            _montiorFile = true;
            _msgManager = msgManager;
            _messageHandlers = new Dictionary<Type, List<DllMessageReceivedCallback>>();

            RegisterMessageListener(typeof(ClientStopMessage), UpdateStopReason);
        }


        public void Dispose()
        {
            _montiorFile = false;
            if (_monitor == null)
                return;

            _monitor.Join(1000);
            if (_monitor.IsAlive)
            {
                _monitor.Abort();
                _monitor.Join();
            }

            _pipe.Close();
            _pipe.Dispose();
        }


        public void UpdateStopReason(DllMessage msg)
        {
            var message = (ClientStopMessage)msg;
            StopReason = message.StopReason;
        }


        public void RegisterMessageListener(Type msgType, DllMessageReceivedCallback callback)
        {
            lock (this)
            {
                if (!_messageHandlers.ContainsKey(msgType))
                    _messageHandlers.Add(msgType, new List<DllMessageReceivedCallback>());

                var callbacks = _messageHandlers[msgType];
                callbacks.Add(callback);
            }
        }


        public virtual void Connect(bool isNew)
        {
            if (_pipe != null)
                throw new ArgumentException("DllCommunication already connected");

            _waitForConnection = isNew;
            if (isNew)
            {
                _pipe = new NamedPipeServerStream(NamePrepend + DllName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            }
            else
            {
                _pipe = new NamedPipeClientStream(".", NamePrepend + DllName, PipeDirection.InOut, PipeOptions.Asynchronous);
                try
                {
                    ((NamedPipeClientStream)_pipe).Connect(1000);
                }
                catch (System.Exception ex)
                {
                    PipeBroken(this);
                    return;
                }
            }

            _inputStream = new InputStream(_pipe);
            _outputStream = new OutputStream(_pipe);

            _monitor = new Thread(ThreadMonitorMain);
            StaticThreadManager.Instance.StartNewThread(_monitor, DllName + "_Communication");
        }


        private void WaitForConnection()
        {
            if (_waitForConnection)
            {
                var pipe = (NamedPipeServerStream)_pipe;
                pipe.WaitForConnection();
                _waitForConnection = false;
            }
        }


        private void ThreadMonitorMain()
        {
            WaitForConnection();
            Console.WriteLine("Dll communication thread working");

            while (_montiorFile)
            {
                if (!_pipe.IsConnected)
                {
                    if (PipeBroken != null)
                        PipeBroken(this);
                }
                    

                var buffer = new byte[4];
                var messageTypeRead = _outputStream.Read(buffer, 4);
                if (messageTypeRead == 0)
                    continue;

                var dllId = BitConverter.ToInt16(buffer, 0);
                var messageType = BitConverter.ToInt16(buffer, 2);
                var msg = (DllMessage)_msgManager.GetMessage(dllId, messageType);

                //Console.WriteLine("Receiving message " + msg.GetType().ToString());
                msg.LoadFromStream(_outputStream);
                //Console.WriteLine("Received");

                lock (this)
                {
                    if (!_messageHandlers.ContainsKey(msg.GetType()))
                        continue;

                    foreach (var callback in _messageHandlers[msg.GetType()])
                        callback(msg);
                }
            }
        }


        public void SendStopRequest()
        {
            if (StopRequested)
                return;

            StopRequested = true;
            SendMessage(new ServerRequestStopMessage());
        }


        public void SendMessage(DllMessage msg)
        {
            WaitForConnection();

            var dllId = _msgManager.GetDllIdForMessage(msg);
            var msgId = _msgManager.GetMessageId(msg);

            var buffer = BitConverter.GetBytes(dllId).Concat(BitConverter.GetBytes(msgId)).ToArray();

            lock (_inputStream)
            {
                //Console.WriteLine("Sending message " + msg.GetType().ToString());
                _inputStream.Write(buffer, 0, 4);
                msg.PushToStream(_inputStream);
                _inputStream.Flush();
                //Console.WriteLine("MessageSent");
            }
        }
    }
}
