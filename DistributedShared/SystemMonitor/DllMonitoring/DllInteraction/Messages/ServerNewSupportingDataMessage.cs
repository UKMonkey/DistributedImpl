using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedSharedInterfaces.Messages;
using DistributedShared.Stream;

namespace DistributedClientShared.SystemMonitor.DllMonitoring.DllInteraction.Messages
{
    public class ServerNewSupportingDataMessage : DllMessage
    {
        public Dictionary<String, byte[]> Data { get; set; }


        public ServerNewSupportingDataMessage()
        {
            Data = new Dictionary<string, byte[]>();
        }


        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(Data);
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            Data = source.ReadDataDict();
        }
    }
}
