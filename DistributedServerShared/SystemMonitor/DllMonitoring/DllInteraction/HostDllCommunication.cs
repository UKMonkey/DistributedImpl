using DistributedSharedInterfaces.Jobs;
using DistributedShared.Network;
using DistributedShared.SystemMonitor.DllMonitoring;
using DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction.Messages;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using System.Collections.Generic;
using System;
using DistributedShared.Jobs;

namespace DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction
{
    public delegate void JobGroupAvialableCallback(WrappedJobGroup item, Dictionary<String, byte[]> changedStatus, Dictionary<String, byte[]> changedSupportingData);
    public delegate void JobResultsProcessedCallback(WrappedResultData item, Dictionary<String, byte[]> changedStatus, Dictionary<String, byte[]> changedSupportingData);
    public delegate void JobGroupDataCallback(WrappedJobGroup group, List<WrappedJobData> jobData);

    /// <summary>
    /// On a job being provided, it is assumed that the supporting data will be updated before
    /// the job is provided...
    /// </summary>
    public class HostDllCommunication : DllCommunication
    {
        public event JobGroupAvialableCallback JobGroupAvailable;
        public event JobGroupDataCallback JobGroupDeconstructed;
        public event JobResultsProcessedCallback JobGroupProcessed;


        public HostDllCommunication(MessageManager messageManager, String namePrepend)
            : base(messageManager)
        {
            NamePrepend = namePrepend;

            RegisterMessageListener(typeof(ClientNewJobGroupMessage), NewJobGroupHandler);
            RegisterMessageListener(typeof(ClientJobGroupContentsMessage), JobContentsHandler);
            //RegisterMessageListener(typeof(ClientProcessedResultsMessage), OldDataAvailableHandler);
            //RegisterMessageListener(typeof(ClientSecurityBreachMessage), OldDataAvailableHandler);
        }


        public void Connect()
        {
            base.Connect(true);
        }


        private void NewJobGroupHandler(DllMessage msg)
        {
            var message = (ClientNewJobGroupMessage)msg;
            JobGroupAvailable(message.JobGroup, message.StatusChanged, message.SupportingDataChanged);
        }


        private void JobContentsHandler(DllMessage msg)
        {
            var message = (ClientJobGroupContentsMessage)msg;
            JobGroupDeconstructed(message.JobGroup, message.JobData);
        }


        /// <summary>
        /// Asks the worker to send us the data for a new job group
        /// </summary>
        /// <param name="jobCount"></param>
        public void GetNextJobGroup(int jobCount)
        {
            var msg = new ServerRequestNewJobGroupMessage { JobCount = jobCount };
            SendMessage(msg);
        }


        /// <summary>
        /// Asks the worker to break down a job group into the individual jobs
        /// </summary>
        /// <param name="group"></param>
        public void DeconstructJobGroup(WrappedJobGroup group)
        {
            var msg = new ServerDeconstructJobGroupMessage { JobGroup = group };
            SendMessage(msg);
        }


        public void DataProvided(IJobResultData request)
        {
        }
    }
}
