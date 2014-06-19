using DistributedShared.Stream;
using DistributedSharedInterfaces.Messages;

namespace DistributedShared.Network.Messages
{
    public class ServerLoginResult : Message
    {
        public bool Result { get; set; }
        public string Message { get; set; }


        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(Result);
            target.Write(Message);
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            Result = source.ReadBoolean();
            Message = source.ReadString();
        }
    }
}
