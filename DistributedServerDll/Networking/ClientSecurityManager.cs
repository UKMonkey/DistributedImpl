using DistributedServerInterfaces.Networking;
using DistributedSharedInterfaces.Messages;
using DistributedShared.Network.Messages;
using DistributedSharedInterfaces.Network;

namespace DistributedServerDll.Networking
{
    public class ClientSecurityManager
    {
        private readonly IConnectionManager _connectionManager;

        public ClientSecurityManager(IConnectionManager conManager)
        {
            _connectionManager = conManager;

            conManager.RegisterMessageBypassingSecurity(typeof(ClientLoginMessage));
            conManager.RegisterMessageListener(typeof(ClientLoginMessage), HandleLoginMessage);
        }


        public void HandleLoginMessage(IConnection con, Message msg)
        {
            // look up the username and get the userId ...
            // with that we can then move on and mark the connection as fully established
            // we don't work with passwords, just usernames so that we know who's done what
            var reply = new ServerLoginResult {Message = "", Result = true};
            _connectionManager.SendMessage(con, reply);
            con.FullyEstablished = reply.Result;
        }
    }
}
