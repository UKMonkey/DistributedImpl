using System.Collections.Generic;
using DistributedShared.Jobs;
using DistributedSharedInterfaces.Jobs;
using DistributedSharedInterfaces.Messages;
using DistributedShared.Stream;

namespace DistributedShared.Network.Messages
{
    public class ServerJobMessage : Message
    {
        public List<WrappedJobData> JobData { get; private set; }


        public ServerJobMessage()
        {
            JobData = new List<WrappedJobData>();
        }


        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(JobData.Count);
            foreach (var t in JobData)
            {
                target.Write(t);
            }
        }


        protected override void Deserialise(IMessageOutputStream source)
        {
            var count = source.ReadInt();

            JobData = new List<WrappedJobData>(count);
            for (var i = 0; i < count; ++i)
            {
                JobData.Add(source.ReadJobData());
            }
        }
    }
}
