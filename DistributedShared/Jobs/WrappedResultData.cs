using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedSharedInterfaces.Jobs;

namespace DistributedShared.Jobs
{
    public class WrappedResultData : IJobResultData
    {
        public string DllName { get; set; }
        public long JobId { get; set; }
        public long GroupId { get; set; }

        public long CyclesSpentWorking { get; set; }
        public bool CyclesSpentWorkingIsReliable { get; set; }
        public byte[] Data { get; set; }

        public IJobData Job { get { throw new InvalidOperationException(); } }
    }
}
