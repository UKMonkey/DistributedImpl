using System;
using System.Collections.Generic;
using System.Linq;
using DistributedSharedInterfaces.Messages;
using System.Reflection;

namespace DistributedShared.Network
{
    public delegate void MessageCallback(Type msgType);

    public class MessageManager
    {
        private readonly Dictionary<String, Dictionary<short, Type>> _dllToMessageIdToMessage = new Dictionary<string, Dictionary<short, Type>>();
        private readonly Dictionary<String, Dictionary<Type, short>> _dllToMessageToMessageId = new Dictionary<string, Dictionary<Type, short>>();

        private readonly Dictionary<String, short> _dllToDllId = new Dictionary<string, short>();
        private readonly Dictionary<short, String> _dllIdToDll = new Dictionary<short, string>();

        private readonly Dictionary<Type, short> _messageToDllId = new Dictionary<Type, short>();


        public event MessageCallback NewMessageAvailable;


        public const short InvalidMessageId = -1;
        public const short InvalidDllId = -1;


        public MessageManager()
        {
            CalculateBaseMessageIds(AppDomain.CurrentDomain, "");
        }


        public List<Type> GetAvailableMessages()
        {
            List<Type> msgs;
            lock (this)
            {
                msgs = _messageToDllId.Keys.ToList();
            }

            return msgs;
        }


        public short GetDllIdForMessage(Message msg)
        {
            var type = msg.GetType();
            return GetDllIdForMessage(type);
        }


        public short GetDllIdForMessage(Type type)
        {
            lock (this)
            {
                if (!_messageToDllId.ContainsKey(type))
                    return -1;
                return _messageToDllId[type];
            }
        }


        public String GetDllName(short dllId)
        {
            lock (this)
            {
                if (_dllIdToDll.ContainsKey(dllId))
                    return _dllIdToDll[dllId];
                return null;
            }
        }


        public short GetDllId(string dllName)
        {
            lock (this)
            {
                if (_dllToDllId.ContainsKey(dllName))
                    return _dllToDllId[dllName];
                return InvalidDllId;
            }
        }


        public Message GetMessage(short dllId, short messageId)
        {
            lock (this)
            {
                if (!_dllIdToDll.ContainsKey(dllId))
                    return null;

                var dllName = _dllIdToDll[dllId];
                return GetMessage(dllName, messageId);
            }
        }


        public Message GetMessage(string dllName, short messageId)
        {
            lock (this)
            {
                if (!_dllToMessageIdToMessage.ContainsKey(dllName))
                    return null;

                var map = _dllToMessageIdToMessage[dllName];
                if (!map.ContainsKey(messageId))
                    return null;

                var messageType = map[messageId];
                var constructor = messageType.GetConstructor(Type.EmptyTypes);
                if (constructor == null)
                    throw new Exception("Message does not have default construtor");
                return constructor.Invoke(null) as Message;
            }
        }


        public short GetMessageId(Message msg)
        {
            return GetMessageId(msg.GetType());
        }


        public short GetMessageId(Type msg)
        {
            string dllName = GetDllName(GetDllIdForMessage(msg));
            lock (this)
            {
                if (!_dllToMessageToMessageId.ContainsKey(dllName))
                    return InvalidMessageId;

                var map = _dllToMessageToMessageId[dllName];
                if (!map.ContainsKey(msg))
                    return InvalidMessageId;

                return map[msg];
            }
        }


        private short CalculateDllId(string dllName)
        {
            var hash = dllName.GetHashCode();
            return (short)hash;
        }


        private void CalculateBaseMessageIds(AppDomain domain, string dllName)
        {
            var type = typeof(Message);

            var types = domain.GetAssemblies().
                SelectMany(s => s.GetTypes()).
                Where(p => p.IsClass).
                Where(type.IsAssignableFrom).ToList();

            types.Sort((x, y) => String.CompareOrdinal(x.Name, y.Name));

            lock (this)
            {
                _dllToMessageIdToMessage.Add(dllName, new Dictionary<short, Type>());
                _dllToMessageToMessageId.Add(dllName, new Dictionary<Type, short>());
                short dllId = CalculateDllId(dllName);

                if (_dllIdToDll.ContainsKey(dllId))
                    throw new Exception("Unable to establish unique id for this dll");

                _dllIdToDll.Add(dllId, dllName);
                _dllToDllId.Add(dllName, dllId);

                for (short i = 0; i < types.Count; ++i)
                {
                    if (_messageToDllId.ContainsKey(types[i]))
                        continue;

                    _dllToMessageIdToMessage[dllName].Add(i, types[i]);
                    _dllToMessageToMessageId[dllName].Add(types[i], i);

                    _messageToDllId.Add(types[i], dllId);
                    if (NewMessageAvailable != null)
                        NewMessageAvailable(types[i]);
                }
            }
        }
    }
}
