using System;
using DistributedSharedInterfaces.Jobs;

namespace DistributedShared.Jobs
{
    public class JobResultData : IJobResultData
    {
        public string DllName { get; private set; }
        public long JobId { get; private set; }
        public byte[] Data { get; set; }

        public long CyclesSpentWorking { get; set; }

        public bool CyclesSpentWorkingIsReliable
        {
            get { throw new NotImplementedException(); }
        }

        public JobResultData(long jobId, String dllName, byte[] data)
        {
            DllName = dllName;
            JobId = jobId;
            Data = data;
        }
    }
}
