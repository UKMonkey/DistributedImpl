using DistributedSharedInterfaces.Messages;
using DistributedShared.Stream;


namespace DistributedShared.Network.Messages
{
    public class ServerCancelWorkMessage : Message
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
