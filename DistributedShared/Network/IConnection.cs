using System;
using System.Net.Sockets;
using DistributedSharedInterfaces.Messages;

namespace DistributedShared.Network
{
    public interface IConnection : IDisposable
    {
        IMessageInputStream DataWriter { get; }
        IMessageOutputStream DataReader { get; }
        Socket Socket { get; }

        bool FullyEstablished { get; set; }
        long UserId { get; set; }
    }
}
