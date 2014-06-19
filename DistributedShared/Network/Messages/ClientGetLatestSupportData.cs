using System;
using DistributedShared.Stream;
using DistributedSharedInterfaces.Messages;

namespace DistributedShared.Network.Messages
{
    public class ClientGetLatestSupportData: Message
    {
        public string DllName;

        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(DllName);
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            DllName = source.ReadString();
        }
    }
}
