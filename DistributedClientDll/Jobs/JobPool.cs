using System;
using System.Collections.Generic;
using System.Threading;
using DistributedShared.Network.Messages;
using DistributedShared.SystemMonitor.Managers;
using DistributedSharedInterfaces.Jobs;
using DistributedSharedInterfaces.Messages;
using DistributedClientDll.Networking;

namespace DistributedClientDll.Jobs
{
    public class JobPool : IDisposable
    {
        private readonly ConnectionManager _connection;
        private readonly Queue<IJobData> _jobPool;
        private volatile short _minJobCount;
        private readonly Thread _worker;
        private bool _awaitingNewJobs;

        private volatile bool _doWork;
        private readonly AutoResetEvent _jobsAvailable = new AutoResetEvent(false);

        public event Action NewJobsAvailable;

        public JobPool(short minJobCount, ConnectionManager connection)
        {
            _minJobCount = minJobCount;
            _connection = connection;
            _jobPool = new Queue<IJobData>();
            _worker = new Thread(MonitorJobPool);

            _connection.RegisterMessageListener(typeof(ServerJobMessage), HandleNewJobs);
            _connection.RegisterMessageListener(typeof(ServerCancelWorkMessage), HandleRemoveJobs);
            _doWork = true;
            _awaitingNewJobs = false;
        }


        public void Dispose()
        {
            _doWork = false;
            _worker.Join();
        }


        public void UpdateJobCount(short number)
        {
            lock (_jobPool)
            {
                _minJobCount = number;
            }
        }


        public void FillPool()
        {
            if (_worker.ThreadState != ThreadState.Unstarted)
                return;
            
            lock (_jobPool)
            {
                if (_worker.ThreadState == ThreadState.Unstarted)
                    StaticThreadManager.Instance.StartNewThread(_worker, "JobPoolMonitor");
            }
        }


        public IJobData GetNextJob(bool wait)
        {
            FillPool();
            while (true)
            {
                lock (_jobPool)
                {
                    if (_jobPool.Count > 0)
                        return _jobPool.Dequeue();
                }

                if (wait)
                    _jobsAvailable.WaitOne();
                else
                    return null;
            }
        }


        public void ReturnJobToPool(IJobData job)
        {
            lock (_jobPool)
            {
                _jobPool.Enqueue(job);
            }
        }


        private Message GetDownloadMoreJobsMessage()
        {
            if (_awaitingNewJobs == false)
            {
                var msg = new ClientGetNewJobsMessage 
                    {NumberOfJobs = (short) (_minJobCount*2)};

                _awaitingNewJobs = true;
                return msg;
            }

            return null;
        }


        private void HandleNewJobs(Message data)
        {
            var msg = (ServerJobMessage) data;
            lock (_jobPool)
            {
                foreach (var item in msg.JobData)
                    _jobPool.Enqueue(item);
            }

            _awaitingNewJobs = false;
            _jobsAvailable.Set();

            NewJobsAvailable();
        }


        private void RemoveJobs(string dllName)
        {
            var tmpQueue = new Queue<IJobData>();

            lock (_jobPool)
            {
                while (_jobPool.Count != 0)
                {
                    var item = _jobPool.Dequeue();
                    if (item.DllName != dllName)
                        tmpQueue.Enqueue(item);
                }

                while (tmpQueue.Count != 0)
                    _jobPool.Enqueue(tmpQueue.Dequeue());
            }
        }


        private void HandleRemoveJobs(Message data)
        {
            var msg = (ServerCancelWorkMessage) data;
            RemoveJobs(msg.DllName);
        }


        private void MonitorJobPool()
        {
            while (_doWork)
            {
                Message msg = null;
                lock (_jobPool)
                {
                    if (_jobPool.Count < _minJobCount)
                        msg = GetDownloadMoreJobsMessage();
                }

                if (msg != null)
                {
                    _connection.SendMessage(msg);
                }

                Thread.Sleep(500);
            }
        }
    }
}
