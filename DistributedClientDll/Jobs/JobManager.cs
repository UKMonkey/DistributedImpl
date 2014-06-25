using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedClientDll.Networking;
using DistributedShared.Network.Messages;
using DistributedShared.SystemMonitor;
using System.Threading;
using DistributedShared.SystemMonitor.Managers;
using DistributedClientDll.SystemMonitor.DllMonitoring;
using DistributedSharedInterfaces.Jobs;
using DistributedShared.Network.Messages;

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
    /* the ideal number of jobs to be run simultaniously and the other      */
    /* binaries will never know if they are being throttled because of      */
    /* CPU issues, bandwidth issues or anything else.                       */
    /************************************************************************/
    public class JobManager : IDisposable
    {
        private readonly JobPool _jobPool;              // keeps a number of jobs available in the cache
        private readonly ClientDllMonitor _dllMonitor;  // provides us access to the loaded dlls and their shared memory

        private readonly ConnectionManager _connection;
        private int JobsInProcess = 0;

        private volatile int _numberOfWorkers;
        public int NumberOfWorkers { get { return _numberOfWorkers; } set { _numberOfWorkers = value; } }

        private volatile bool _doWork = true;

        public JobManager(ConnectionManager connection, ClientDllMonitor dllMonitor)
        {
            _connection = connection;
            _dllMonitor = dllMonitor;
            _workerToKeepRunning = new Dictionary<Thread, bool>();

            NumberOfWorkers = Environment.ProcessorCount;
            _jobPool = new JobPool((short)(NumberOfWorkers+2), _connection);

            // when there's new jobs available, make sure that we've got all the threads going
            _jobPool.NewJobsAvailable += QueueNewJobs;

            // when a job is complete, make sure we've got all the threads going
            _dllMonitor.DllLoaded += RegisterJobCompletedEvents;
        }


        public void Dispose()
        {
            StopDoingJobs();
            _jobPool.Dispose();
        }


        private void QueueNewJobs()
        {
            lock (this)
            {
                JobsInProcess--;
                if (JobsInProcess >= _numberOfWorkers)
                    return;

                var nextJob = _jobPool.GetNextJob(false);
                if (nextJob != null)
                    QueueJob(nextJob);
            }
        }


        private void QueueJob(IJobData data)
        {
            var dll = _dllMonitor.GetLoadedDll(data.DllName);
            if (dll == null)
                return;

            dll.SharedMemory.AddJobData(data);
        }


        private void SendJobResults(ClientSharedMemory item)
        {
            lock (this)
            {
                var result = item.GetCompletedData();
                while (result != null)
                {
                    var msg = new ClientJobComplete();
                }
            }
        }


        private void RegisterJobCompletedEvents(string dllName)
        {
            var dll = _dllMonitor.GetLoadedDll(dllName);
            dll.SharedMemory.JobCompleted += x => QueueNewJobs();
            dll.SharedMemory.JobCompleted += SendJobResults;
        }
    }
}
