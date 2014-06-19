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
        private readonly short _minJobCount;
        private readonly Thread _worker;
        private bool _awaitingNewJobs = false;

        private volatile bool _doWork;
        private readonly AutoResetEvent _jobsAvailable = new AutoResetEvent(false);

        public JobPool(short minJobCount, ConnectionManager connection)
        {
            _minJobCount = minJobCount;
            _connection = connection;
            _jobPool = new Queue<IJobData>();
            _worker = new Thread(MonitorJobPool);

            _connection.RegisterMessageListener(typeof(ServerJobMessage), HandleNewJobs);
            _connection.RegisterMessageListener(typeof(ServerCancelWorkMessage), HandleRemoveJobs);
            _doWork = true;
        }


        public void Dispose()
        {
            _doWork = false;
            _worker.Join();
        }


        public IJobData GetNextJob()
        {
            while (true)
            {
                lock (_jobPool)
                {
                    if (_worker.ThreadState == ThreadState.Unstarted)
                        StaticThreadManager.Instance.StartNewThread(_worker, "JobPoolMonitor");
                    if (_jobPool.Count > 0)
                        return _jobPool.Dequeue();
                }

                _jobsAvailable.WaitOne();
            }
        }


        public void ReturnJobToPool(IJobData job)
        {
            lock (_jobPool)
            {
                _jobPool.Enqueue(job);
            }
        }


        private void DownloadMoreJobs()
        {
            if (_awaitingNewJobs == false)
            {
                var msg = new ClientGetNewJobsMessage { NumberOfJobs = (short)(_minJobCount * 2) };
                _connection.SendMessage(msg);

                _awaitingNewJobs = true;
            }
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
                var doWork = false;
                lock (_jobPool)
                {
                    if (_jobPool.Count < _minJobCount)
                        doWork = true;
                }

                if (doWork)
                    DownloadMoreJobs();

                Thread.Sleep(500);
            }
        }
    }
}
