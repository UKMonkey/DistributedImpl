using DistributedSharedInterfaces.Jobs;
using DistributedShared.Network;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction.Messages;
using DistributedShared.SystemMonitor.DllMonitoring;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction.Messages;
using System.Collections.Generic;
using System;
using DistributedShared.Jobs;
using System.Threading;

namespace DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction
{
    public delegate void StopRequiredCallback();

    public delegate void JobRequiredCallback(int count);
    public delegate void JobResultCallback(WrappedResultData result);
    public delegate void JobGroupCallback(WrappedJobGroup item);
    public delegate void DataAvailableCallback(Dictionary<String, byte[]> status, Dictionary<String, byte[]> supportingData);

    public class WorkerDllCommunication : DllCommunication
    {
        public event StopRequiredCallback StopRequired;

        public event JobResultCallback JobCompleted;
        public event JobRequiredCallback NewJobGroupRequired;
        public event DataAvailableCallback OldDataAvailable;
        public event JobGroupCallback JobGroupDeconstructionRequired;


        public WorkerDllCommunication(MessageManager messageManager, String namePrepend)
            : base(messageManager)
        {
            NamePrepend = namePrepend;

            RegisterMessageListener(typeof(ServerRequestStopMessage), x => StopRequestedHandler());
            RegisterMessageListener(typeof(ServerRequestNewJobGroupMessage), GetNewJobGroupHandler);
            RegisterMessageListener(typeof(ServerOldStatusDataMessage), OldDataAvailableHandler);
            RegisterMessageListener(typeof(ServerDeconstructJobGroupMessage), DeconstructJobGroupHandler);

            PipeBroken += x => StopRequestedHandler();
        }


        public void Connect()
        {
            base.Connect(false);
        }


        private void StopRequestedHandler()
        {
            StopRequired();
        }


        private void DeconstructJobGroupHandler(DllMessage msg)
        {
            var message = (ServerDeconstructJobGroupMessage)msg;
            ThreadPool.QueueUserWorkItem((x) => JobGroupDeconstructionRequired(message.JobGroup));
        }


        private void GetNewJobGroupHandler(DllMessage msg)
        {
            var message = (ServerRequestNewJobGroupMessage)msg;
            NewJobGroupRequired(message.JobCount);
        }


        private void OldDataAvailableHandler(DllMessage msg)
        {
            var message = (ServerOldStatusDataMessage)msg;
            OldDataAvailable(message.Status, message.SupportingData);
        }
    }
}
