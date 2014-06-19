using DistributedShared.Stream;
using DistributedSharedInterfaces.Messages;

namespace DistributedShared.Network.Messages
{
    public class ServerSupportMd5Message : Message
    {
        public string DllName { get; set; }
        public string Md5 { get; set; }


        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(DllName);
            target.Write(Md5);
        }


        protected override void Deserialise(IMessageOutputStream source)
        {
            DllName = source.ReadString();
            Md5 = source.ReadString();
        }
    }
}
