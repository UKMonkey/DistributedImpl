using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedSharedInterfaces.Messages;
using DistributedSharedInterfaces.Jobs;
using DistributedShared.Stream;
using DistributedShared.Jobs;

namespace DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction.Messages
{
    public class ClientProcessedResultsMessage : DllMessage
    {
        public WrappedResultData Results { get; set; }

        public Dictionary<string, byte[]> StatusChanged { get; set; }
        public Dictionary<string, byte[]> SupportingDataChanged { get; set; }


        public ClientProcessedResultsMessage()
        {
            StatusChanged = new Dictionary<string, byte[]>();
        }


        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(Results);
            target.Write(StatusChanged);
            target.Write(SupportingDataChanged);
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            Results = source.ReadResultData();
            StatusChanged = source.ReadDataDict();
            SupportingDataChanged = source.ReadDataDict();
        }
    }
}
