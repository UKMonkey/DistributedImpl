using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedShared.SystemMonitor;
using DistributedShared.Network;
using System.Threading;
using DistributedSharedInterfaces.Messages;

namespace DistributedShared.Network
{
    public class MessageStatistics
    {
        private readonly Dictionary<Type, long> _totalSentMessages = new Dictionary<Type, long>();
        private readonly Dictionary<Type, long> _totalReceivedMessages = new Dictionary<Type, long>();

        private readonly Dictionary<Type, RollingAverage> _averageSentMessages = new Dictionary<Type, RollingAverage>();
        private readonly Dictionary<Type, RollingAverage> _averageReceivedMessages = new Dictionary<Type, RollingAverage>();

        private readonly Dictionary<Type, RollingAverage> _averageSentMessageSize = new Dictionary<Type, RollingAverage>();
        private readonly Dictionary<Type, RollingAverage> _averageReceivedMessageSize = new Dictionary<Type, RollingAverage>();

        private static int _msecRollingTime = 1000;

        private readonly MessageManager _msgManager;
        private readonly ReaderWriterLock _newTypeLock;


        public MessageStatistics(MessageManager messageManager)
        {
            _msgManager = messageManager;

            _newTypeLock = new ReaderWriterLock();
            _msgManager.NewMessageAvailable += AddNewType;

            foreach (var type in _msgManager.GetAvailableMessages())
                AddNewType(type);
        }


        public void MessageSent(Message msg)
        {
            try
            {
                var type = msg.GetType();
                var msgSize = msg.GetMessageSize();

                _newTypeLock.AcquireReaderLock(5000);
                _totalSentMessages[type] = _totalSentMessages[type] + 1;
                _averageSentMessages[type].AddPoint(1);
                _averageSentMessageSize[type].AddPoint(msgSize);

                Console.WriteLine(string.Format("Sent message {0}", msg.GetType().Name));
            }
            finally
            {
                _newTypeLock.ReleaseLock();
            }
        }


        public void MessageReceived(Message msg)
        {
            try
            {
                var type = msg.GetType();
                var msgSize = msg.GetMessageSize();

                _newTypeLock.AcquireReaderLock(5000);
                _totalReceivedMessages[type] = _totalSentMessages[type] + 1;
                _averageReceivedMessages[type].AddPoint(1);
                _averageReceivedMessageSize[type].AddPoint(msgSize);

                Console.WriteLine(string.Format("Got message {0}", msg.GetType().Name));
            }
            finally
            {
                _newTypeLock.ReleaseLock();
            }
        }


        private void AddNewType(Type msgType)
        {
            try
            {
                _newTypeLock.AcquireReaderLock(5000);

                if (_totalSentMessages.ContainsKey(msgType))
                    return;

                _newTypeLock.UpgradeToWriterLock(5000);

                _totalSentMessages.Add(msgType, 0);
                _totalReceivedMessages.Add(msgType, 0);

                _averageReceivedMessages.Add(msgType, new RollingAverage(_msecRollingTime));
                _averageReceivedMessageSize.Add(msgType, new RollingAverage(_msecRollingTime));

                _averageSentMessages.Add(msgType, new RollingAverage(_msecRollingTime));
                _averageSentMessageSize.Add(msgType, new RollingAverage(_msecRollingTime));
            }
            finally
            {
                _newTypeLock.ReleaseLock();
            }
        }
    }
}
