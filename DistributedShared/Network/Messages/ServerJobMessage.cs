using System.Collections.Generic;
using DistributedShared.Jobs;
using DistributedSharedInterfaces.Jobs;
using DistributedSharedInterfaces.Messages;
using DistributedShared.Stream;

namespace DistributedShared.Network.Messages
{
    public class ServerJobMessage : Message
    {
        public List<IJobData> JobData { get; private set; }


        public ServerJobMessage()
        {
            JobData = new List<IJobData>();
        }


        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(JobData.Count);
            foreach (var t in JobData)
            {
                target.Write(t.JobId);
                target.Write(t.DllName);
                target.Write(t.Data);

                target.Write(t.SupportingDataVersion);
            }
        }


        protected override void Deserialise(IMessageOutputStream source)
        {
            var count = source.ReadInt();

            JobData = new List<IJobData>(count);
            for (var i = 0; i < count; ++i)
            {
                var id = source.ReadLong();
                var name = source.ReadString();
                var data = source.ReadByteArray();
                var version = source.ReadLong();

                JobData.Add(new JobData(id, data, name) 
                    { SupportingDataVersion = version });
            }
        }
    }
}
