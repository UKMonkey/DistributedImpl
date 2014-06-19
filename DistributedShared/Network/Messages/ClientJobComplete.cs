using System.Collections.Generic;
using DistributedShared.Jobs;
using DistributedShared.Stream;
using DistributedSharedInterfaces.Jobs;
using DistributedSharedInterfaces.Messages;

namespace DistributedShared.Network.Messages
{
    public class ClientJobComplete : Message
    {
        public List<IJobResultData> JobResults { get; private set; }


        public ClientJobComplete()
        {
            JobResults = new List<IJobResultData>();
        }

        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(JobResults.Count);
            for (var i = 0; i < JobResults.Count; ++i)
            {
                target.Write(JobResults[i].JobId);
                target.Write(JobResults[i].DllName);
                target.Write(JobResults[i].Data);
            }
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            var count = source.ReadInt();
            JobResults = new List<IJobResultData>(count);

            for (var i=0; i<count; ++i)
            {
                var id = source.ReadLong();
                var dll = source.ReadString();
                var data = source.ReadByteArray();

                var result = new JobResultData(id, dll, data);
                JobResults.Add(result);
            }
        }
    }
}
