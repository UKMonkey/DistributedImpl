using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedSharedInterfaces.Jobs;

namespace DistributedShared.Jobs
{
    public class WrappedJobGroup : IJobGroup
    {
        public long GroupId { get; set; }
        public long SupportingDataVersion { get; set; }
        public int JobCount { get; set; }
        public byte[] Data { get; set; }
        public string DllName { get; set; }

        public IEnumerable<IJobData> GetJobs()
        {
            throw new NotImplementedException();
        }


        public static WrappedJobGroup WrapGroup(IJobGroup item)
        {
            var ret = new WrappedJobGroup();
            ret.JobCount = item.JobCount;
            ret.Data = item.Data;
            return ret;
        }
    }
}
