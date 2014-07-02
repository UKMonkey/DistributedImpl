using DistributedSharedInterfaces.Messages;

namespace DistributedShared.SystemMonitor.DllMonitoring.DllInteraction.Messages
{
    public class ServerRequestStopMessage : DllMessage
    {
        protected override void Serialise(IMessageInputStream target)
        {
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
        }
    }
}
