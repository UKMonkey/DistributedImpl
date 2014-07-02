using DistributedSharedInterfaces.Jobs;

namespace DistributedClientDll.Wrappers
{
    public class JobResultData : IJobResultData
    {
        public string DllName { get; set; }
        public long JobId { get; set; }
        public byte[] Data { get; set; }
        public long CyclesSpentWorking { get; set; }
        public bool CyclesSpentWorkingIsReliable { get; set; }
    }
}
