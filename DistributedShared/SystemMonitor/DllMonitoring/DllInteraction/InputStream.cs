using DistributedSharedInterfaces.Messages;
using System.IO.Pipes;

namespace DistributedShared.SystemMonitor.DllMonitoring.DllInteraction
{
    public class InputStream : IMessageInputStream
    {
        private readonly PipeStream _pipe;
        private long _dataWritten;

        public InputStream(PipeStream pipe)
        {
            _dataWritten = 0;
            _pipe = pipe;
        }

        public void Write(byte[] buffer, int offset, int size)
        {
            _pipe.Write(buffer, offset, size);
            _dataWritten += size;
        }

        public void Flush()
        {
            _pipe.Flush();
        }

        public void ResetStats()
        {
            _dataWritten = 0;
        }

        public long GetDataSize()
        {
            return _dataWritten;
        }
    }
}
