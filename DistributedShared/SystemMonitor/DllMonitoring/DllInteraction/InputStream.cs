using DistributedSharedInterfaces.Messages;
using System.IO.Pipes;
using System;

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
            try
            {
                _pipe.Write(buffer, offset, size);
                _dataWritten += size;
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Unable to complete sending a message");
            }
        }

        public void Flush()
        {
            try
            {
                //_pipe.Flush();
            }
            catch (System.Exception)
            {
            }
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
