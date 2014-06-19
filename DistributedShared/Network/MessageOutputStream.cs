using System.Net.Sockets;
using DistributedSharedInterfaces.Messages;

namespace DistributedShared.Network
{
    public class MessageOutputStream : IMessageOutputStream
    {
        private readonly NetworkStream _stream;
        private long _dataSize;

        public MessageOutputStream(NetworkStream stream)
        {
            _stream = stream;
        }


        public int Read(byte[] buffer, int amount)
        {
            _dataSize += amount;
            return _stream.Read(buffer, 0, amount);
        }


        public void ResetStats()
        {
            _dataSize = 0;
        }


        public long GetDataSize()
        {
            return _dataSize;
        }
    }
}
