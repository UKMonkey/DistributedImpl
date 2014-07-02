using DistributedSharedInterfaces.Jobs;
using DistributedShared.Network;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction.Messages;
using DistributedServerInterfaces.Interfaces;
using DistributedShared.SystemMonitor.DllMonitoring;
using DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction.Messages;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using System.Collections.Generic;

namespace DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction
{
    public delegate void SupportingVersionCallback(long version);
    public delegate void StopRequiredCallback();
    public delegate void JobGroupDataCallback(IJobGroup group, List<IJobData> jobData);

    /// <summary>
    /// On a job being provided, it is assumed that the supporting data will be updated before
    /// the job is provided...
    /// </summary>
    public class HostDllCommunication : DllCommunication
    {
        public event SupportingVersionCallback SupportingDataChanged;
        public event JobGroupCallback JobGroupAvailable;
        public event JobGroupDataCallback JobGroupDeconstructed;

        public byte[] SupportingData { get; private set; }
        public byte[] StatusData { get; set; } 

        public HostDllCommunication(MessageManager messageManager)
            : base(messageManager)
        {
        }


        public void Connect()
        {
            base.Connect(true);
        }


        /// <summary>
        /// Asks the worker to send us the data for a new job group
        /// </summary>
        /// <param name="jobCount"></param>
        public void GetNextJobGroup(int jobCount)
        {
            var msg = new ServerRequestNewJobGroupMessage() { JobCount = jobCount };
            SendMessage(msg);
        }


        /// <summary>
        /// Asks the worker to break down a job group into the individual jobs
        /// </summary>
        /// <param name="group"></param>
        public void DeconstructJobGroup(IJobGroup group)
        {
            var msg = new ServerDeconstructJobGroupMessage() { JobGroup = group };
            SendMessage(msg);
        }


        public void DataProvided(IJobResultData request)
        {
        }
    }
}
