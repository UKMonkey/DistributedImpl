using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedSharedInterfaces.Jobs;
using DistributedSharedInterfaces.Messages;
using DistributedShared.Stream;


namespace DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction.Messages
{
    public class ServerDeconstructJobGroupMessage: DllMessage
    {
        public IJobGroup JobGroup { get; set; }

        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(JobGroup);
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            JobGroup = source.ReadJobGroup();
        }
    }
}
