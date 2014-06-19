using System.Net.Sockets;
using DistributedSharedInterfaces.Messages;
using DistributedSharedInterfaces.Network;

namespace DistributedShared.Network
{
    public class Connection : IConnection
    {
        public IMessageInputStream DataWriter { get; private set; }
        public IMessageOutputStream DataReader { get; private set; }
        public Socket Socket { get; private set; }

        public bool FullyEstablished { get; set; }
        public long UserId { get; set; }

        private readonly NetworkStream _netStream;

        public Connection(Socket socket)
        {
            _netStream = new NetworkStream(socket);
            DataReader = new MessageOutputStream(_netStream);
            DataWriter = new MessageInputStream(_netStream);

            Socket = socket;
        }


        public void Dispose()
        {
            DataReader = null;
            DataWriter = null;
            _netStream.Close();
        }
    }
}
