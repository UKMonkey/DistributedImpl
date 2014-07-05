using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DistributedServerInterfaces.Interfaces;
using DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction.Messages;
using DistributedShared.SystemMonitor.Managers;
using DistributedSharedInterfaces.Jobs;
using DistributedShared.Jobs;

namespace DistributedServerWorker.Jobs
{
    public class JobsHandler : IDisposable, IServerApi
    {
        private readonly Queue<int> _jobsToProduce = new Queue<int>();
        private readonly Queue<WrappedResultData> _resultsToProcess = new Queue<WrappedResultData>();

        private readonly WorkerDllCommunication _communication;
        private readonly String _dllName;

        private DataMonitor _supportingDataMonitor;
        private DataMonitor _statusDataMonitor;
        private Dictionary<String, byte[]> _oldStatusData;
        private Dictionary<String, byte[]> _oldSupportData;

        private IDllApi _dllApi;


        public JobsHandler(WorkerDllCommunication communication, String dllName)
        {
            _communication = communication;

            _communication.NewJobGroupRequired += QueueNewJobRequest;
            _communication.JobCompleted += QueueJobCompleted;
            _communication.OldDataAvailable += QueueOldDataAvailable;
            _communication.JobGroupDeconstructionRequired += HandleDeconstruction;

            _dllName = dllName;
        }


        public void SetDllApi(IDllApi api)
        {
            lock (_jobsToProduce)
            {
                _dllApi = api;

                if (_oldSupportData != null && _oldStatusData != null)
                {
                    _dllApi.OnDllLoaded(this, _oldSupportData, _oldStatusData);

                    _supportingDataMonitor = new DataMonitor(_oldSupportData);
                    _dllApi.SupportingDataChanged += _supportingDataMonitor.DataChanged;

                    _statusDataMonitor = new DataMonitor(_oldStatusData);
                    _dllApi.StatusDataChanged += _statusDataMonitor.DataChanged;
                }
            }
        }


        private void QueueOldDataAvailable(Dictionary<String, byte[]> status, 
                                           Dictionary<String, byte[]> supportingData)
        {
            lock (_jobsToProduce)
            {
                _oldStatusData = status;
                _oldSupportData = supportingData;
                if (_dllApi != null)
                {
                    SetDllApi(_dllApi);
                }
            }
        }


        private void QueueNewJobRequest(int count)
        {
            lock (_jobsToProduce)
            {
                _jobsToProduce.Enqueue(count);
            }
        }


        private void QueueJobCompleted(WrappedResultData result)
        {
            lock (_resultsToProcess)
            {
                _resultsToProcess.Enqueue(result);
            }
        }


        private void HandleDeconstruction(WrappedJobGroup grp)
        {
            while (_dllApi == null)
            {
                Thread.Yield();
            }

            var processor = _dllApi.GetCleanJobGroup();
            processor.Data = grp.Data;
            var jobs = processor.GetJobs().Select(item => WrappedJobData.WrapJob(item, _dllName, grp)).ToList();
            var msg = new ClientJobGroupContentsMessage()
                {
                    JobData = jobs,
                    JobGroup = grp
                };
            _communication.SendMessage(msg);
        }


        public bool DoProcessResultsWork()
        {
            WrappedResultData toProcess = null;
            lock (_jobsToProduce)
            {
                if (_dllApi == null)
                    return false;
                if (_supportingDataMonitor == null)
                    return false;
            }

            lock (_resultsToProcess)
            {
                if (_resultsToProcess.Count == 0)
                    return false;
                toProcess = _resultsToProcess.Dequeue();
            }

            _dllApi.DataProvided(toProcess);
            var msg = new ClientProcessedResultsMessage()
                {
                    Results = toProcess,
                    StatusChanged = _statusDataMonitor.GetChanged(),
                    SupportingDataChanged = _supportingDataMonitor.GetChanged()
                };
            _communication.SendMessage(msg);
            _statusDataMonitor.Reset();
            _supportingDataMonitor.Reset();
            return true;
        }


        public bool DoNewJobsWork()
        {
            var count = 0;
            lock (_jobsToProduce)
            {
                if (_dllApi == null)
                    return false;
                if (_supportingDataMonitor == null)
                    return false;
                if (_jobsToProduce.Count == 0)
                    return false;
                count = _jobsToProduce.Dequeue();
            }

            var group = WrappedJobGroup.WrapGroup(_dllApi.GetNextJobGroup(count));
            group.DllName = _dllName;
            group.SupportingDataVersion = _supportingDataMonitor.CurrentVersion;

            var msg = new ClientNewJobGroupMessage()
                {
                    JobGroup = group,
                    StatusChanged = _statusDataMonitor.GetChanged(),
                    SupportingDataChanged = _supportingDataMonitor.GetChanged()
                };

            _communication.SendMessage(msg);
            _statusDataMonitor.Reset();
            _supportingDataMonitor.Reset();
            return true;
        }


        public void Dispose()
        {
        }
    }
}
