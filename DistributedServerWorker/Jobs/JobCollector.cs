using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DistributedServerInterfaces.Interfaces;
using DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction.Messages;
using DistributedShared.SystemMonitor.Managers;

namespace DistributedServerWorker.Jobs
{
    public class JobCollector : IDisposable, IServerApi
    {
        private readonly Queue<ServerRequestNewJobGroupMessage> _jobs = new Queue<ServerRequestNewJobGroupMessage>();
        private readonly WaitHandle _jobQueueProtection = new EventWaitHandle(false, EventResetMode.ManualReset);
        private readonly WorkerDllCommunication _communication;
        private readonly IDllApi _dllApi;
        private readonly Thread _workerThread;

        private volatile int _supportingDataVersion;
        private volatile bool _performingJobs = true;


        public JobCollector(WorkerDllCommunication communication, IDllApi dllApi)
        {
            _dllApi = dllApi;
            _communication = communication;

            _communication.NewJobGroupRequired += GetNewJobs;
            _dllApi.OnDllLoaded(this);
            _supportingDataVersion = 0;

            _workerThread = new Thread(JobThreadMain);
            StaticThreadManager.Instance.StartNewThread(_workerThread, "JobCollector");
        }


        public void Dispose()
        {
            _performingJobs = false;
            _workerThread.Join();
        }


        private int? GetNextJob()
        {
            lock (_jobQueueProtection)
            {
                if (_jobs.Count > 0)
                {
                    var job = _jobs.Dequeue();
                    return job.JobCount;
                }
            }

            _jobQueueProtection.WaitOne(1000);
            return null;
        }


        private void JobThreadMain()
        {
            while (_performingJobs)
            {
                var nextTaskCount = GetNextJob();
                if (nextTaskCount == null)
                    continue;

                var nextJob = _dllApi.GetNextJobGroup(nextTaskCount.Value);
            }
        }
    }
}
