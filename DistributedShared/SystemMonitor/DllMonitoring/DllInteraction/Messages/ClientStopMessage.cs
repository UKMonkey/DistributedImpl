using DistributedSharedInterfaces.Messages;
using DistributedShared.Stream;

namespace DistributedShared.SystemMonitor.DllMonitoring.DllInteraction.Messages
{
    public class ClientStopMessage: DllMessage
    {
        public StopReason StopReason { get; set; }



        protected override void Serialise(IMessageInputStream target)
        {
            target.Write((short)StopReason);
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            StopReason = (StopReason)source.ReadShort();
        }
    }
}
