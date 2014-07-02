using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedShared.Stream;
using DistributedSharedInterfaces.Jobs;
using DistributedSharedInterfaces.Messages;

namespace DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction
{
    public static class StreamWriterExtensions
    {
        public static void Write(this IMessageInputStream writer, IJobGroup value)
        {

        }

        public static void Write(this IMessageInputStream writer, IJobData value)
        {

        }
    }
}
