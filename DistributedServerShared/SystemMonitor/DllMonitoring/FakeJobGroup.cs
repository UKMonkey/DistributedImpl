using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedSharedInterfaces.Jobs;

namespace DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction
{
    public class FakeJobGroup : IJobGroup
    {
        public long GroupId { get; set; }
        public long SupportingDataVersion { get; set; }
        public int JobCount { get; set; }
        public byte[] Data { get; set; }

        public IEnumerable<IJobData> GetJobs()
        {
            throw new NotImplementedException();
        }
    }
}
