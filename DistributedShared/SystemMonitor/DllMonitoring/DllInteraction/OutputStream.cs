using DistributedSharedInterfaces.Messages;
using System.IO.Pipes;

namespace DistributedShared.SystemMonitor.DllMonitoring.DllInteraction
{
    public class OutputStream : IMessageOutputStream
    {
        private readonly PipeStream _pipe;
        private long _read;

        public OutputStream(PipeStream pipe)
        {
            _read = 0;
            _pipe = pipe;
        }

        public int Read(byte[] buffer, int amount)
        {
            if (!_pipe.CanRead)
                return 0;

            var read = _pipe.Read(buffer, 0, amount);
            _read += read;
            return read;
        }

        public void ResetStats()
        {
            _read = 0;
        }

        public long GetDataSize()
        {
            return _read;
        }
    }
}
