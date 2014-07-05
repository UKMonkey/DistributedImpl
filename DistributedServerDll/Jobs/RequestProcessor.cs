using System;
using System.Collections.Generic;
using System.Linq;
using DistributedServerDll.Persistance;
using DistributedServerInterfaces.Interfaces;
using System.Data.SQLite;
using System.Threading;
using DistributedShared.SystemMonitor.Managers;
using DistributedSharedInterfaces.Jobs;
using DistributedShared.SystemMonitor.DllMonitoring;
using DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction.Messages;
using DistributedShared.Jobs;

namespace DistributedServerDll.Jobs
{
    public class RequestProcessor : IServerApi, IDisposable
    {
        public readonly string DllName;

        private volatile bool _provideJobs = true;
        private volatile bool _doWork = true;
        private volatile bool _awaitingJobs;

        private readonly DllWrapper<HostDllCommunication> _brain;
        private readonly Queue<WrappedJobGroup> _queuedJobGroups = new Queue<WrappedJobGroup>();
        private readonly Queue<WrappedJobData> _queuedJobs = new Queue<WrappedJobData>();

        private readonly Thread _jobQueueMaintainer;
        private readonly DllJobStore _jobStore;

        private readonly AutoResetEvent _jobsRequestedEvent;
        private Dictionary<String, byte[]> _supportingData;

        public RequestProcessor(string dllName, DllWrapper<HostDllCommunication> jobBrain, string storePath)
        {
            _awaitingJobs = false;

            DllName = dllName;
            _brain = jobBrain;

            _jobStore = new DllJobStore(storePath, dllName);

            _supportingData = _jobStore.GetStoredSupportingData();
            foreach (var group in _jobStore.GetStoredGroups())
                _queuedJobGroups.Enqueue(group);
            foreach (var job in _jobStore.GetStoredJobs())
                _queuedJobs.Enqueue(job);

            _brain.SharedMemory.JobGroupAvailable += (p, q, r) => ResetWaitingFlag();
            _brain.SharedMemory.JobGroupAvailable += QueueNewJobGroup;

            _brain.SharedMemory.JobGroupDeconstructed += (p, q) => ResetWaitingFlag();
            _brain.SharedMemory.JobGroupDeconstructed += QueueNewJobs;

            _jobQueueMaintainer = new Thread(JobQueueMaintainerMain);
            _jobsRequestedEvent = new AutoResetEvent(true);

            StaticThreadManager.Instance.StartNewThread(_jobQueueMaintainer, "JobQueueMonitor");
        }


        public void Dispose()
        {
            StopAllJobs();
            _brain.Dispose();
        }


        private void ResetWaitingFlag()
        {
            _awaitingJobs = false;
        }


        public IEnumerable<WrappedJobData> GetJobs(int count)
        {
            var grp = new List<WrappedJobData>();

            lock (_queuedJobs)
            {
                while (_queuedJobs.Count > 0 && count > 0)
                {
                    var job = _queuedJobs.Dequeue();
                    count--;
                    grp.Add(job);
                }
            }

            _jobsRequestedEvent.Set();

            return grp;
        }


        public void JobComplete(IJobResultData jobResult)
        {
            _brain.SharedMemory.DataProvided(jobResult);
        }


        public void ContinueProcessingRequests()
        {
            _provideJobs = true;
        }


        public bool IsAcceptingRequests()
        {
            return _provideJobs;
        }


        public void StopAllJobs()
        {
            _provideJobs = false;
            _doWork = false;
            _jobQueueMaintainer.Join();
        }


        public void HoldNewRequests()
        {
            _provideJobs = false;
        }


        public long GetSupportingDataVersion()
        {
            lock (_supportingData)
            {
                return BitConverter.ToInt64(_supportingData[ReservedKeys.Version], 0);
            }
        }


        private void QueueNewJobs(WrappedJobGroup group, List<WrappedJobData> jobs)
        {
            lock (_jobStore)
            {
                _jobStore.StoreGroupData(group, jobs);
            }

            lock (_queuedJobs)
            {
                foreach (var job in jobs)
                    _queuedJobs.Enqueue(job);
            }
        }


        private void QueueNewJobGroup(WrappedJobGroup group, Dictionary<String, byte[]> statusChanged, Dictionary<String, byte[]> supportChanged)
        {
            lock (_supportingData)
            {
                foreach (var item in supportChanged)
                    _supportingData[item.Key] = item.Value;
                group.SupportingDataVersion = BitConverter.ToInt64(_supportingData[ReservedKeys.Version], 0);
            }

            lock (_jobStore)
            {
                _jobStore.StoreNewGroup(group, statusChanged, supportChanged);
            }

            lock (_queuedJobGroups)
            {
                _queuedJobGroups.Enqueue(group);
            }
        }


        private void DeepCopyData(Dictionary<string, byte[]> from, Dictionary<string, byte[]> to)
        {
            to.Clear();
            lock (from)
            {
                foreach (var item in from)
                {
                    var newValue = new byte[item.Value.Length];
                    item.Value.CopyTo(newValue, 0);

                    to[item.Key] = newValue;
                }
            }
        }


        public void CopySupportingData(Dictionary<String, byte[]> target)
        {
            DeepCopyData(_supportingData, target);
        }


        private void SendDllStoredData()
        {
            var msg = new ServerOldStatusDataMessage();
            Dictionary<String, byte[]> data;
            lock (_jobStore)
            {
                data = _jobStore.GetStoredStatus();
            }

            DeepCopyData(data, msg.Status);
            DeepCopyData(_supportingData, msg.SupportingData);

            _brain.SharedMemory.SendMessage(msg);
        }


        private void JobQueueMaintainerMain()
        {
            SendDllStoredData();

            while (_doWork)
            {
                _jobsRequestedEvent.WaitOne(500);
                var queueNewJobs = false;
                WrappedJobGroup nextJobToExpand = null;

                if (_awaitingJobs)
                    continue;

                lock (_queuedJobGroups)
                {
                    if (_queuedJobGroups.Count < 5)
                        queueNewJobs = true;

                    lock (_queuedJobs)
                    {
                        if (_queuedJobs.Count < 100 && _queuedJobGroups.Count > 0)
                            nextJobToExpand = _queuedJobGroups.Dequeue();
                    }
                }

                if (queueNewJobs)
                {
                    _awaitingJobs = true;
                    _brain.SharedMemory.GetNextJobGroup(100);
                }

                if (nextJobToExpand != null)
                {
                    _awaitingJobs = true;
                    _brain.SharedMemory.DeconstructJobGroup(nextJobToExpand);
                }
            }
        }
    }
}
