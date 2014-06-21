using System;
using System.Collections.Generic;
using System.Linq;
using DistributedServerDll.Persistance;
using DistributedServerInterfaces.Interfaces;
using System.Data.SQLite;
using System.Threading;
using DistributedServerInterfaces.Networking;
using System.Security.Cryptography;
using System.IO;
using DistributedShared.SystemMonitor.Managers;
using DistributedSharedInterfaces.Jobs;
using DistributedShared.Jobs;

namespace DistributedServerDll.Jobs
{
    public class RequestProcessor : IServerApi, IDisposable
    {
        public readonly string DllName;

        private volatile bool _provideJobs = true;
        private volatile bool _doWork = true;
        private long _currentSupportingDataVersion = 0;

        private readonly IDllApi _brain;
        private readonly Queue<IJobGroup> _queuedJobGroups = new Queue<IJobGroup>();
        private readonly Thread _jobQueueMaintainer;
        private readonly IConnectionManager _conectionManager;
        private readonly DllJobGroupStore _jobStore;

        private readonly AutoResetEvent _jobsRequestedEvent;

        public RequestProcessor(string dllName, IDllApi jobBrain, IConnectionManager conectionManager)
        {
            DllName = dllName;
            _brain = jobBrain;

            _brain.SupportingDataChanged += IncreaseSupportingDataVersion;
            _jobQueueMaintainer = new Thread(JobQueueMaintainerMain);
            _conectionManager = conectionManager;
            _jobsRequestedEvent = new AutoResetEvent(true);

            _jobStore = new DllJobGroupStore(@"c:\stuff", dllName);
            var status = _jobStore.GetStoredStatus();

            if (status.Length != 0)
                jobBrain.StatusData = status;
            _currentSupportingDataVersion = _jobStore.GetStoredSupportingDataVersion();

            StaticThreadManager.Instance.StartNewThread(_jobQueueMaintainer, "JobQueueMonitor");
        }


        public void Dispose()
        {
            StopAllJobs();
            _brain.Dispose();
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
                _brain.DataProvided(jobResult);
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
            return _brain.SupportingData;
        }

        /*
        private void RecalculateSupportingMd5(IDllApi item)
        {
            // don't let any more jobs get added or processed until we've resolved this new supporting data MD5
            // we then also want to update all the existing jobs to have the latest MD5
            lock (this)
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = new MemoryStream(item.SupportingData))
                    {
                        _supportingDataMd5 = BitConverter.ToString(md5.ComputeHash(stream));
                    }
                }
            }
        }
        */

        private void QueueNewJobs()
        {
            var jobs = _brain.GetNextJobGroup(100);
            lock (_queuedJobGroups)
            {
                _queuedJobGroups.Enqueue(jobs);
                jobs.SupportingDataVersion = _currentSupportingDataVersion;
            }
        }


        private void IncreaseSupportingDataVersion(IDllApi dll)
        {
            _currentSupportingDataVersion += 1;
        }


        private void JobQueueMaintainerMain()
        {
            while (_doWork)
            {
                _jobsRequestedEvent.WaitOne(500);
                var queueNewJobs = false;

                lock (this)
                {
                    if (_queuedJobGroups.Count < 100)
                        queueNewJobs = true;
                }

                if (queueNewJobs)
                    QueueNewJobs();
            }
        }
    }
}
