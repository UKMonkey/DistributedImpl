using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedShared.Stream;
using DistributedSharedInterfaces.Messages;
using DistributedSharedInterfaces.Jobs;
using System.Collections.Generic;
using System;
using DistributedShared.Jobs;

namespace DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction.Messages
{
    public class ClientNewJobGroupMessage : DllMessage
    {
        public WrappedJobGroup JobGroup { get; set; }
        public Dictionary<string, byte[]> StatusChanged { get; set; }
        public Dictionary<string, byte[]> SupportingDataChanged { get; set; }


        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(JobGroup);
            target.Write(StatusChanged);
            target.Write(SupportingDataChanged);
        }


        protected override void Deserialise(IMessageOutputStream source)
        {
            JobGroup = source.ReadJobGroup();
            StatusChanged = source.ReadDataDict();
            SupportingDataChanged = source.ReadDataDict();
        }
    }
}
