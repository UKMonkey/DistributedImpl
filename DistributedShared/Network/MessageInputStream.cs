using DistributedSharedInterfaces.Messages;
using System.Net.Sockets;


namespace DistributedShared.Network
{
    public class MessageInputStream : IMessageInputStream
    {
        private readonly NetworkStream _stream;
        private long _dataSize;

        public MessageInputStream(NetworkStream stream)
        {
            _stream = stream;
        }

        public void Write(byte[] buffer, int offset, int size)
        {
            _stream.Write(buffer, offset, size);
            _dataSize += size;
        }

        public void Flush()
        {
            _stream.Flush();
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
