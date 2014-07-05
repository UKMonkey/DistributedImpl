using DistributedShared.Network;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction.Messages;
using DistributedShared.SystemMonitor.DllMonitoring;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedClientShared.SystemMonitor.DllMonitoring.DllInteraction.Messages;
using System.Collections.Generic;
using System;
using DistributedShared.Jobs;


namespace DistributedClientShared.SystemMonitor.DllMonitoring.DllInteraction
{
    public delegate void StopRequiredCallback();
    public delegate void JobAvailableCallback(WrappedJobData item);
    public delegate void SupportingDataCallback(Dictionary<String, byte[]> data);

    public class WorkerDllCommunication : DllCommunication
    {
        public event StopRequiredCallback StopRequired;
        public event JobAvailableCallback OnWorkRequest;
        public event SupportingDataCallback OnSupportingDataUpdate;

        public WorkerDllCommunication(MessageManager messageManager, String namePrepend)
            : base(messageManager)
        {
            NamePrepend = namePrepend;

            RegisterMessageListener(typeof(ServerDoWorkMessage), DoNewWorkHandler);
            RegisterMessageListener(typeof(ServerNewSupportingDataMessage), UpdateSupportingDataHandler);

            RegisterMessageListener(typeof(ServerRequestStopMessage), x => StopRequestedHandler());
            PipeBroken += x => StopRequestedHandler();
        }


        private void DoNewWorkHandler(DllMessage msg)
        {
            var message = (ServerDoWorkMessage)msg;
            OnWorkRequest(message.Job);
        }


        private void UpdateSupportingDataHandler(DllMessage msg)
        {
            var message = (ServerNewSupportingDataMessage)msg;
            OnSupportingDataUpdate(message.Data);
        }


        public void Connect()
        {
            base.Connect(false);
        }


        private void StopRequestedHandler()
        {
            StopRequired();
        }
    }
}
