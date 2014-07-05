using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedSharedInterfaces.Jobs;

namespace DistributedShared.Jobs
{
    public class WrappedJobData : IJobData
    {
        public string DllName { get; set; }
        public long SupportingDataVersion { get; set; }
        public long GroupId { get; set; }
        public long JobId { get; set; }
        public byte[] Data { get; set; }


        public static WrappedJobData WrapJob(IJobData item, String dllName, WrappedJobGroup grp)
        {
            var ret = new WrappedJobData();
            ret.DllName = dllName;
            ret.Data = item.Data;
            ret.JobId = 0;
            ret.GroupId = grp.GroupId;
            ret.SupportingDataVersion = grp.SupportingDataVersion;
            return ret;
        }
    }
}
