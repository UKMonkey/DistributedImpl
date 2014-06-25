using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedClientDll.Networking;
using DistributedShared.SystemMonitor;
using System.Threading;
using DistributedShared.SystemMonitor.Managers;
using DistributedClientDll.SystemMonitor.DllMonitoring;

namespace DistributedClientDll.Jobs
{
    public class JobManager : IDisposable
    {
        private readonly JobPool _jobPool;
        //private readonly JobWorker _jobWorker;
        private readonly ConnectionManager _connection;
        private readonly ClientDllMonitor _dllMonitor;

        private volatile bool _replaceDeadThreads;
        private readonly Thread _montiorThread;
        private readonly Dictionary<Thread, bool> _workerToKeepRunning;
        
        public int NumberOfWorkers { get; private set; }


        public JobManager(ConnectionManager connection, ClientDllMonitor dllMonitor)
        {
            _connection = connection;
            _dllMonitor = dllMonitor;
            _workerToKeepRunning = new Dictionary<Thread, bool>();

            NumberOfWorkers = Environment.ProcessorCount;
            _jobPool = new JobPool((short)(NumberOfWorkers+2), _connection);
            //_jobWorker = new JobWorker(_connection, dllMonitor);

            _montiorThread = new Thread(MonitorMain);
            _replaceDeadThreads = false;
            StaticThreadManager.Instance.StartNewThread(_montiorThread, "JobThreadManager");
        }


        public void Dispose()
        {
            StopDoingJobs();
            _montiorThread.Abort();
            _montiorThread.Join();
        }


        public void UpdateNumberOfWorkers(int newNumber)
        {
            lock (this)
            {
                var change = newNumber - NumberOfWorkers;
                NumberOfWorkers = newNumber;
                while (change > 0)
                {
                    AddNewWorkerThread();
                    change--;
                }

                while (change < 0)
                {
                    RemoveWorkerThread();
                    change++;
                }
            }
        }


        private void RemoveWorkerThread()
        {
            if (_workerToKeepRunning.Count == 0)
                return;
            
            var thread = _workerToKeepRunning.First().Key;
            _workerToKeepRunning[thread] = false;
        }


        private void AddNewWorkerThread()
        {
            var thread = new Thread(WorkerMain);
            _workerToKeepRunning.Add(thread, true);
            StaticThreadManager.Instance.StartNewThread(thread, "JobWorker");
        }


        public void StartDoingJobs()
        {
            lock (this)
            {
                _replaceDeadThreads = true;
            }
        }


        public void StopDoingJobs(bool waitForCompletion=false)
        {
            List<Thread> threads;
            lock (this)
            {
                _replaceDeadThreads = false;
                threads = _workerToKeepRunning.Keys.ToList();
                foreach (var thread in threads)
                    _workerToKeepRunning[thread] = false;
            }

            if (waitForCompletion)
            {
                foreach (var thread in threads)
                    thread.Join();
            }
        }


        public void ForceStopDoingJobs()
        {
            List<Thread> threads;
            lock (this)
            {
                _replaceDeadThreads = false;
                threads = _workerToKeepRunning.Keys.ToList();
                foreach (var thread in threads)
                    thread.Abort();
            }

            foreach (var thread in threads)
                thread.Join();
        }


        private void MonitorMain()
        {
            while (true)
            {
                Thread.Sleep(100);
                lock (this)
                {
                    if (!_replaceDeadThreads)
                        continue;

                    while (_workerToKeepRunning.Count < NumberOfWorkers)
                        AddNewWorkerThread();
                }
            }
        }


        private void WorkerMain()
        {
            var doWork = true;
            while (doWork)
            {
                var job = _jobPool.GetNextJob();
                //if (!_jobWorker.ProcessJob(job))
                //    _jobPool.ReturnJobToPool(job);
            }
        }
    }
}
