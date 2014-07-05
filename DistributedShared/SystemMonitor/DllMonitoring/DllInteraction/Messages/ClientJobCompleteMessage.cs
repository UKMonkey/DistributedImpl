using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedSharedInterfaces.Messages;
using DistributedShared.Stream;
using DistributedShared.Jobs;

namespace DistributedClientShared.SystemMonitor.DllMonitoring.DllInteraction.Messages
{
    public class ClientJobCompleteMessage: DllMessage
    {
        public WrappedResultData Result { get; set; }

        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(Result);
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            Result = source.ReadResultData();
        }
    }
}
