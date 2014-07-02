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

namespace DistributedServerDll.Jobs
{
    public class RequestProcessor : IServerApi, IDisposable
    {
        public readonly string DllName;

        private volatile bool _provideJobs = true;
        private volatile bool _doWork = true;
        private long _currentSupportingDataVersion;
        private bool _awaitingJobs;

        private readonly DllWrapper<HostDllCommunication> _brain;
        private readonly Queue<IJobGroup> _queuedJobGroups = new Queue<IJobGroup>();
        private readonly Thread _jobQueueMaintainer;
        private readonly DllJobGroupStore _jobStore;

        private readonly AutoResetEvent _jobsRequestedEvent;

        public RequestProcessor(string dllName, DllWrapper<HostDllCommunication> jobBrain, string storePath)
        {
            _currentSupportingDataVersion = 0;
            _awaitingJobs = false;

            DllName = dllName;
            _brain = jobBrain;

            _brain.SharedMemory.SupportingDataChanged += UpdateSupportingDataVersion;
            _brain.SharedMemory.JobGroupAvailable += p => ResetWaitingFlag();
            _brain.SharedMemory.JobGroupAvailable += QueueNewJobs;

            _jobQueueMaintainer = new Thread(JobQueueMaintainerMain);
            _jobsRequestedEvent = new AutoResetEvent(true);

            _jobStore = new DllJobGroupStore(storePath, dllName);
            var status = _jobStore.GetStoredStatus();

            if (status.Length != 0)
                jobBrain.SharedMemory.StatusData = status;
            _currentSupportingDataVersion = _jobStore.GetStoredSupportingDataVersion();

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


        public IEnumerable<IJobData> GetJobs(int count)
        {
            var grp = new List<IJobGroup>();

            lock (this)
            {
                while (_queuedJobGroups.Count > 0 && count > 0)
                {
                    var jobs = _queuedJobGroups.Dequeue();
                    count -= jobs.JobCount;
                    grp.Add(jobs);
                }
            }

            _jobsRequestedEvent.Set();

            return grp.SelectMany(x => x.GetJobs());
        }


        public void JobComplete(IJobResultData jobResult)
        {
            lock (this)
            {
                _brain.SharedMemory.DataProvided(jobResult);
            }
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


        public SQLiteConnection GetSQInterface()
        {
            return null;
        }


        public long GetSupportingDataVersion()
        {
            return _currentSupportingDataVersion;
        }


        public byte[] GetSupportingData()
        {
            return _brain.SharedMemory.SupportingData;
        }


        private void QueueNewJobs(IJobGroup group)
        {
            lock (_queuedJobGroups)
            {
                _queuedJobGroups.Enqueue(group);
                group.SupportingDataVersion = _currentSupportingDataVersion;
            }
        }


        private void UpdateSupportingDataVersion(long version)
        {
            _currentSupportingDataVersion = version;
        }


        private void JobQueueMaintainerMain()
        {
            while (_doWork)
            {
                _jobsRequestedEvent.WaitOne(500);
                var queueNewJobs = false;
                if (_awaitingJobs)
                    continue;

                lock (this)
                {
                    if (_queuedJobGroups.Count < 100)
                        queueNewJobs = true;
                }

                if (queueNewJobs)
                {
                    _awaitingJobs = true;
                    _brain.SharedMemory.GetNextJobGroup(100);
                }
            }
        }
    }
}
