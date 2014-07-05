using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedSharedInterfaces.Messages;
using DistributedSharedInterfaces.Jobs;
using DistributedShared.Stream;
using DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedShared.Jobs;

namespace DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction.Messages
{
    public class ClientJobGroupContentsMessage : DllMessage
    {
        public WrappedJobGroup JobGroup { get; set; }
        public List<WrappedJobData> JobData { get; set; }


        public ClientJobGroupContentsMessage()
        {
            JobData = new List<WrappedJobData>();
        }

        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(JobGroup);
            target.Write(JobData.Count);

            foreach (var item in JobData)
            {
                target.Write(item);
            }
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            JobGroup = source.ReadJobGroup();
            var count = source.ReadInt();

            JobData = new List<WrappedJobData>(count);
            for (var i = 0; i < count; ++i)
                JobData.Add(source.ReadJobData());
        }
    }
}
