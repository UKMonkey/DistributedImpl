using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedSharedInterfaces.Messages;
using DistributedShared.Stream;

namespace DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction.Messages
{
    public class ServerRequestNewJobGroupMessage : DllMessage
    {
        public int JobCount { get; set; }

        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(JobCount);
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            JobCount = source.ReadInt();
        }
    }
}
