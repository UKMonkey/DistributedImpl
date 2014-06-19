using DistributedShared.Stream;
using DistributedSharedInterfaces.Messages;

namespace DistributedShared.Network.Messages
{
    public class ServerSupportDataMessage: Message
    {
        public string DllName;

        public string Md5 { get; set; }
        public byte[] Data { get; set; }


        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(DllName);
            target.Write(Md5);
            target.Write(Data);
        }


        protected override void Deserialise(IMessageOutputStream source)
        {
            DllName = source.ReadString();
            Md5 = source.ReadString();
            Data = source.ReadByteArray();
        }
    }
}
