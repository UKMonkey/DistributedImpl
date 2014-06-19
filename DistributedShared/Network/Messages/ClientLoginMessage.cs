using System;
using DistributedShared.Stream;
using DistributedSharedInterfaces.Messages;

namespace DistributedShared.Network.Messages
{
    public class ClientLoginMessage : Message
    {
        public String Username;


        protected override void Serialise(IMessageInputStream target)
        {
            target.WriteEncrypted(Username);
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            Username = source.ReadEncrypted();
        }
    }
}
