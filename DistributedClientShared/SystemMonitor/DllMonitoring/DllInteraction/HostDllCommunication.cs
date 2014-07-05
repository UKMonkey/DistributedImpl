using DistributedSharedInterfaces.Jobs;
using DistributedShared.Network;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction.Messages;
using DistributedClientInterfaces.Interfaces;
using DistributedShared.SystemMonitor.DllMonitoring;
using DistributedClientShared.SystemMonitor.DllMonitoring.DllInteraction.Messages;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using System.Collections.Generic;
using System;
using DistributedShared.Jobs;

namespace DistributedClientShared.SystemMonitor.DllMonitoring.DllInteraction
{
    public delegate void JobCompletedCallback(WrappedResultData result);

    /// <summary>
    /// On a job being provided, it is assumed that the supporting data will be updated before
    /// the job is provided...
    /// </summary>
    public class HostDllCommunication : DllCommunication
    {
        public event JobCompletedCallback JobCompleted;
        private int _workerCount;


        public HostDllCommunication(MessageManager messageManager, String namePrepend)
            : base(messageManager)
        {
            _workerCount = 0;
            NamePrepend = namePrepend;
            RegisterMessageListener(typeof(ClientJobCompleteMessage), JobCompletedHandler);
        }


        public void Connect()
        {
            base.Connect(true);
        }


        private void JobCompletedHandler(DllMessage msg)
        {
            lock (this)
            {
                _workerCount--;
            }

            var message = (ClientJobCompleteMessage)msg;
            JobCompleted(message.Result);
        }


        public void AddJobData(WrappedJobData data)
        {
            lock (this)
            {
                _workerCount++;
            }

            var msg = new ServerDoWorkMessage() { Job = data };
            SendMessage(msg);
        }


        public int GetCurrentWorkerCount()
        {
            lock (this)
            {
                return _workerCount;
            }
        }

        public void SetCurrentSupportingData(Dictionary<string, byte[]> dictionary)
        {
            var msg = new ServerNewSupportingDataMessage()
                {
                    Data = dictionary
                };
            SendMessage(msg);
        }
    }
}
