using DistributedSharedInterfaces.Jobs;

namespace DistributedShared.Jobs
{
    public class JobData : IJobData
    {
        public string DllName { get; set; }
        public long JobId { get; set; }
        public byte[] Data { get; set; }
        public string SupportingDataMd5 { get; set; }

        public JobData(long jobId, byte[] data, string dllName)
        {
            DllName = dllName;
            JobId = jobId;
            Data = data;
        }
    }
}
