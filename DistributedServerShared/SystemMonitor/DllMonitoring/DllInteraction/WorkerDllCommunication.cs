using DistributedSharedInterfaces.Jobs;
using DistributedShared.Network;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction.Messages;
using DistributedShared.SystemMonitor.DllMonitoring;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction.Messages;

namespace DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction
{
    public delegate void JobRequiredCallback(int count);

    public class WorkerDllCommunication : DllCommunication
    {
        public event JobResultCallback JobCompleted;
        public event StopRequiredCallback StopRequired;
        public event JobRequiredCallback NewJobGroupRequired;

        public byte[] StatusData { get; set; }
        public byte[] SupportingData { get; set; }
        public int SupportingDataVersion { get; set; }

        public WorkerDllCommunication(MessageManager messageManager)
            : base(messageManager)
        {
            RegisterMessageListener(typeof(ServerRequestStopMessage), x => StopRequestedHandler());
            RegisterMessageListener(typeof(ServerRequestNewJobGroupMessage), GetNewJobGroupHandler);

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


        private void GetNewJobGroupHandler(DllMessage msg)
        {
            var message = (ServerRequestNewJobGroupMessage)msg;
            NewJobGroupRequired(message.JobCount);
        }
    }
}
