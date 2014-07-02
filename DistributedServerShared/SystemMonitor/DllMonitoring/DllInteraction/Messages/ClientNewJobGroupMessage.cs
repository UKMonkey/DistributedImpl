using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedShared.Stream;
using DistributedSharedInterfaces.Messages;
using DistributedSharedInterfaces.Jobs;

namespace DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction.Messages
{
    public class ClientNewJobGroupMessage : DllMessage
    {
        public IJobGroup JobGroup { get; set; }
        public byte[] Status { get; set; }
        public byte[] SupportingData { get; set; }


        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(JobGroup);
            target.Write(Status);
        }


        protected override void Deserialise(IMessageOutputStream source)
        {
            JobGroup = source.ReadJobGroup();
            Status = source.ReadByteArray();
        }
    }
}
