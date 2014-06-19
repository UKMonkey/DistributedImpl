using DistributedShared.Stream;
using DistributedSharedInterfaces.Messages;

namespace DistributedShared.Network.Messages
{
    public class ClientGetSupportDataMd5Message : Message
    {
        public string DllName { get; set; }

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
