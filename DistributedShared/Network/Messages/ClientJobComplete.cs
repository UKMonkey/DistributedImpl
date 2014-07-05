using System.Collections.Generic;
using DistributedShared.Jobs;
using DistributedShared.Stream;
using DistributedSharedInterfaces.Jobs;
using DistributedSharedInterfaces.Messages;

namespace DistributedShared.Network.Messages
{
    public class ClientJobComplete : Message
    {
        public List<WrappedResultData> JobResults { get; private set; }


        public ClientJobComplete()
        {
            JobResults = new List<WrappedResultData>();
        }

        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(JobResults.Count);
            foreach (var t in JobResults)
            {
                target.Write(t);
            }
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            var count = source.ReadInt();
            JobResults = new List<WrappedResultData>(count);

            for (var i=0; i<count; ++i)
            {
                JobResults.Add(source.ReadResultData());
            }
        }
    }
}
