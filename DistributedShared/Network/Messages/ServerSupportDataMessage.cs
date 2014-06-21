using DistributedShared.Stream;
using DistributedSharedInterfaces.Messages;

namespace DistributedShared.Network.Messages
{
    public class ServerSupportDataMessage: Message
    {
        public string DllName;

        public long Version { get; set; }
        public byte[] Data { get; set; }


        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(DllName);
            target.Write(Version);
            target.Write(Data);
        }


        protected override void Deserialise(IMessageOutputStream source)
        {
            DllName = source.ReadString();
            Version = source.ReadLong();
            Data = source.ReadByteArray();
        }
    }
}
