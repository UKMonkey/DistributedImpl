using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedSharedInterfaces.Messages;
using DistributedShared.Stream;

namespace DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction.Messages
{
    public class ServerOldStatusDataMessage : DllMessage
    {
        public Dictionary<String, byte[]> Status { get; private set; }
        public Dictionary<String, byte[]> SupportingData { get; private set; }

        public ServerOldStatusDataMessage()
        {
            Status = new Dictionary<string, byte[]>();
            SupportingData = new Dictionary<string, byte[]>();
        }


        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(Status.Count);
            foreach (var key in Status.Keys)
            {
                target.Write(key);
                target.Write(Status[key]);
            }

            target.Write(SupportingData.Count);
            foreach (var key in SupportingData.Keys)
            {
                target.Write(key);
                target.Write(SupportingData[key]);
            }
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            Status.Clear();
            SupportingData.Clear();

            var count = source.ReadInt();
            for (var i = 0; i < count; ++i)
            {
                var key = source.ReadString();
                var value = source.ReadByteArray();
                Status.Add(key, value);
            }

            count = source.ReadInt();
            for (var i = 0; i < count; ++i)
            {
                var key = source.ReadString();
                var value = source.ReadByteArray();
                SupportingData.Add(key, value);
            }
        }
    }
}
