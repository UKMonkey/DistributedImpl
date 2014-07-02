using System;
using System.Collections.Generic;
using DistributedClientDll.Networking;
using DistributedShared.Network.Messages;
using DistributedClientDll.SystemMonitor.DllMonitoring;
using DistributedSharedInterfaces.Jobs;

namespace DistributedClientDll.Jobs
{
    /************************************************************************/
    /* This class is responsible for acting as a middle man between the     */
    /* job pool, which ensures that we have a certain number of jobs        */
    /* available to process, and the binaries that process the jobs.        */
    /* since the binaries have no understanding about each other or their   */
    /* existence, they are expected to do the jobs as and when they are     */
    /* given to them;  leaving this middle man class being the master       */
    /* of who is processing which job and how many; ie it can establish     */
    /* the ideal number of jobs to be run simultaneously and the other      */
    /* binaries will never know if they are being throttled because of      */
    /* CPU issues, bandwidth issues or anything else.                       */
    /************************************************************************/
    public class JobManager : IDisposable
    {
        private readonly JobPool _jobPool;              // keeps a number of jobs available in the cache
        private readonly ClientDllMonitor _dllMonitor;  // provides us access to the loaded dlls and their shared memory

        private readonly ConnectionManager _connection;
        private volatile int _jobsInProcess;

        private volatile int _numberOfWorkers;
        public int TargetNumberOfWorkers { get { return _numberOfWorkers; } set { _numberOfWorkers = value; } }

        private volatile bool _doWork = true;

        public JobManager(ConnectionManager connection, ClientDllMonitor dllMonitor)
        {
            _jobsInProcess = 0;
            _connection = connection;
            _dllMonitor = dllMonitor;

            TargetNumberOfWorkers = Environment.ProcessorCount;
            _jobPool = new JobPool((short)(TargetNumberOfWorkers * 2), _connection);

            // when there's new jobs available, make sure that we've got all the threads going
            _jobPool.NewJobsAvailable += QueueNewJobs;

            // when a job is complete, make sure we've got all the threads going
            _dllMonitor.DllLoaded += RegisterJobCompletedEvents;

            // start downloading the jobs, there's no point delaying, after all we assume that the connection
            // has been fully completed for this class to be created
            _jobPool.FillPool();
        }


        public void Dispose()
        {
            StopDoingJobs();
            _jobPool.Dispose();
        }


        public void StopDoingJobs()
        {
            lock (this)
            {
                _doWork = false;
            }
        }


        public void StartDoingJobs()
        {
            _doWork = true;
            QueueNewJobs();
        }


        private void QueueNewJobs()
        {
            lock (this)
            {
                if (_doWork == false)
                    return;

                var failed = new List<IJobData>();
                while (_jobsInProcess < TargetNumberOfWorkers)
                {
                    var nextJob = _jobPool.GetNextJob(false);
                    if (nextJob != null)
                    {
                        var success = QueueJob(nextJob);
                        if (!success)
                            failed.Add(nextJob);
                    }
                }

                foreach (var job in failed)
                {
                    _jobPool.ReturnJobToPool(job);
                }
            }
        }


        private bool QueueJob(IJobData data)
        {
            var dll = _dllMonitor.GetLoadedDll(data.DllName);
            if (dll == null || !dll.IsRunning())
                return false;

            _jobsInProcess++;
            dll.SharedMemory.AddJobData(data);
            return true;
        }


        private void SendJobResults(IJobResultData result)
        {
            var msg = new ClientJobComplete();
            msg.JobResults.Add(result);

            _connection.SendMessage(msg);
        }


        private void ForceUpdateWorkerCount()
        {
            lock (this)
            {
                _numberOfWorkers = 0;
                foreach (var dllName in _dllMonitor.GetAvailableDlls())
                {
                    var dll = _dllMonitor.GetLoadedDll(dllName);
                    if (dll == null)
                        continue;

                    _numberOfWorkers += dll.SharedMemory.GetCurrentWorkerCount();
                }
            }
        }


        private void RegisterJobCompletedEvents(string dllName)
        {
            var dll = _dllMonitor.GetLoadedDll(dllName);

            dll.SharedMemory.JobCompleted += x => QueueNewJobs();
            dll.SharedMemory.JobCompleted += SendJobResults;

            dll.ProcessTerminatedGracefully += p => ForceUpdateWorkerCount();
            dll.ProcessTerminatedUnexpectedly += p => ForceUpdateWorkerCount();
        }
    }
}
