using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedSharedInterfaces.Messages;
using DistributedShared.SystemMonitor.DllMonitoring;
using DistributedShared.Stream;

namespace DistributedShared.SystemMonitor.DllMonitoring.DllInteraction.Messages
{
    public class ClientSecurityBreachMessage : DllMessage
    {
        public String ProblemSummary { get; set; }
        public StopReason Reason { get; set; }

        protected override void Serialise(IMessageInputStream target)
        {
            target.Write((short)Reason);
            target.Write(ProblemSummary);
        }


        protected override void Deserialise(IMessageOutputStream source)
        {
            Reason = (StopReason)source.ReadShort();
            ProblemSummary = source.ReadString();
        }
    }
}
