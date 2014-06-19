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
            for (var i = 0; i < JobData.Count; ++i)
            {
                target.Write(JobData[i].JobId);
                target.Write(JobData[i].DllName);
                target.Write(JobData[i].Data);

                target.Write(JobData[i].SupportingDataMd5);
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
                var md5 = source.ReadString();

                JobData.Add(new JobData(id, data, name)
                    {SupportingDataMd5 = md5});
            }
        }
    }
}
