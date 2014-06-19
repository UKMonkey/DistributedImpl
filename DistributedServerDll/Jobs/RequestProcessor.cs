using System;
using System.Collections.Generic;
using DistributedServerInterfaces.Interfaces;
using System.Data.SQLite;
using System.Threading;
using DistributedServerInterfaces.Networking;
using System.Security.Cryptography;
using System.IO;
using DistributedShared.SystemMonitor.Managers;
using DistributedSharedInterfaces.Jobs;
using DistributedShared.Jobs;

namespace DistributedServerDll.Networking
{
    public class RequestProcessor : IServerApi, IDisposable
    {
        public readonly string DllName;

        private volatile bool _provideJobs = true;
        private volatile bool _doWork = true;

        private readonly IDllApi _brain;
        private readonly Queue<IJobData> _queuedJobs = new Queue<IJobData>();
        private readonly Thread _jobQueueMaintainer;
        private readonly IConnectionManager _conectionManager;
        private volatile String _supportingDataMd5;
        private volatile int NextJobId = 0;

        private readonly AutoResetEvent _jobsRequestedEvent;

        public RequestProcessor(string dllName, IDllApi jobBrain, IConnectionManager conectionManager)
        {
            DllName = dllName;
            _brain = jobBrain;
            _brain.SupportingDataChanged += RecalculateSupportingMd5;
            _jobQueueMaintainer = new Thread(JobQueueMaintainerMain);
            _conectionManager = conectionManager;
            _jobsRequestedEvent = new AutoResetEvent(true);
            
            RecalculateSupportingMd5(_brain);

            StaticThreadManager.Instance.StartNewThread(_jobQueueMaintainer, "JobQueueMonitor");
        }


        public void Dispose()
        {
            StopAllJobs();
            _brain.Dispose();
        }


        public IEnumerable<IJobData> GetJobs(int count)
        {
            var ret = new List<IJobData>(count);

            lock (this)
            {
                while (_queuedJobs.Count > 0 && count > 0)
                {
                    var job = _queuedJobs.Dequeue();
                    ret.Add(new JobData(NextJobId++, job.Data, DllName) { SupportingDataMd5 = _supportingDataMd5 });
                    --count;
                }
            }

            _jobsRequestedEvent.Set();

            return ret;
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


        public String GetSupportingDataMd5()
        {
            return _supportingDataMd5;
        }


        public byte[] GetSupportingData()
        {
            return _brain.SupportingData;
        }


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


        private void QueueNewJobs()
        {
            var jobs = _brain.GetNextJobGroup(100);
            lock (_queuedJobs)
            {
                foreach (var job in jobs.GetJobs())
                {
                    job.SupportingDataMd5 = _supportingDataMd5;
                    _queuedJobs.Enqueue(job);
                }
            }
        }


        private void JobQueueMaintainerMain()
        {
            while (_doWork)
            {
                _jobsRequestedEvent.WaitOne(500);
                var queueNewJobs = false;

                lock (this)
                {
                    if (_queuedJobs.Count < 1000)
                        queueNewJobs = true;
                }

                if (queueNewJobs)
                    QueueNewJobs();
            }
        }
    }
}
