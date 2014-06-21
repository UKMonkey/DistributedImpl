using DistributedShared.Stream;
using DistributedSharedInterfaces.Messages;

namespace DistributedShared.Network.Messages
{
    public class SupportDataVersionMessage : Message
    {
        public string DllName { get; set; }
        public long Version { get; set; }


        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(DllName);
            target.Write(Version);
        }


        protected override void Deserialise(IMessageOutputStream source)
        {
            DllName = source.ReadString();
            Version = source.ReadLong();
        }
    }
}
