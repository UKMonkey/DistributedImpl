using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedShared.Stream;
using DistributedSharedInterfaces.Messages;
using DistributedShared.Jobs;

namespace DistributedClientShared.SystemMonitor.DllMonitoring.DllInteraction.Messages
{
    public class ServerDoWorkMessage : DllMessage
    {
        public WrappedJobData Job { get; set; }

        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(Job);
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            Job = source.ReadJobData();
        }
    }
}
