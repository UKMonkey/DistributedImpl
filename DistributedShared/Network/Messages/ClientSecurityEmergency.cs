using DistributedShared.Stream;
using DistributedSharedInterfaces.Messages;

namespace DistributedShared.Network.Messages
{
    public class ClientSecurityEmergency : Message
    {
        public string ProcessName { get; set; }
        public bool ThreadCreated { get; set; }

        public ClientSecurityEmergency()
        {
            ProcessName = "";
            ThreadCreated = false;
        }

        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(ProcessName);
            target.Write(ThreadCreated);
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            ProcessName = source.ReadString();
            ThreadCreated = source.ReadBoolean();
        }
    }
}
