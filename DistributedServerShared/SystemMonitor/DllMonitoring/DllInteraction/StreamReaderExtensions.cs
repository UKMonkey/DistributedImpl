using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedShared.Stream;
using DistributedSharedInterfaces.Jobs;
using DistributedSharedInterfaces.Messages;

namespace DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction
{
    public static class StreamReaderExtensions
    {
        public static IJobGroup ReadJobGroup(this IMessageOutputStream writer)
        {
            return null;
        }

        public static IJobData ReadJobData(this IMessageOutputStream writer)
        {
            return null;
        }
    }
}
