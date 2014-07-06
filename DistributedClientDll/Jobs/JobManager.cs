using System;
using System.Collections.Generic;
using DistributedClientDll.Networking;
using DistributedShared.Network.Messages;
using DistributedClientDll.SystemMonitor.DllMonitoring;
using DistributedSharedInterfaces.Jobs;
using DistributedShared.Jobs;
using DistributedSharedInterfaces.Messages;

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
        private readonly Dictionary<String, long> _supportingDataVersions; // keeps track of the supporting Data that's available
        private readonly HashSet<String> _awaitingSupportingData = new HashSet<String>();

        private readonly ConnectionManager _connection;
        private volatile int _jobsInProcess;

        private volatile int _numberOfWorkers;
        public int TargetNumberOfWorkers { get { return _numberOfWorkers; } set { _numberOfWorkers = value; } }

        private volatile bool _doWork = true;

        public JobManager(ConnectionManager connection, ClientDllMonitor dllMonitor)
        {
            _supportingDataVersions = new Dictionary<string, long>();
            _jobsInProcess = 0;
            _connection = connection;
            _dllMonitor = dllMonitor;

            TargetNumberOfWorkers = Environment.ProcessorCount;
            _jobPool = new JobPool((short)(TargetNumberOfWorkers * 2), _connection);

            // when there's new jobs available, make sure that we've got all the threads going
            _jobPool.NewJobsAvailable += QueueNewJobs;
            _dllMonitor.DllLoaded += x => QueueNewJobs();

            // when a job is complete, make sure we've got all the threads going
            _dllMonitor.DllLoaded += RegisterJobCompletedEvents;
            _dllMonitor.DllLoaded += RegisterDllRemovedEvents;

            // supporting data handling
            connection.RegisterMessageListener(typeof(SupportDataVersionMessage), HandleSupportingDataVersionMessage);
            connection.RegisterMessageListener(typeof(ServerSupportDataMessage),  HandleSupportingDataMessage);
        }


        public void Dispose()
        {
            StopDoingJobs();
            _jobPool.Dispose();
        }


        private void HandleSupportingDataVersionMessage(Message msg)
        {
            var message = (SupportDataVersionMessage)msg;
            var getNewVersion = true;

            if (_supportingDataVersions.ContainsKey(message.DllName))
            {
                if (_supportingDataVersions[message.DllName] == message.Version)
                    getNewVersion = false;
            }

            if (!getNewVersion)
                return;

            GetLatestSupportingData(message.DllName);
        }


        private void GetLatestSupportingData(String dllName)
        {
            lock (_awaitingSupportingData)
            {
                if (_awaitingSupportingData.Contains(dllName))
                    return;
                _awaitingSupportingData.Add(dllName);

                var reply = new ClientGetLatestSupportData();
                reply.DllName = dllName;
                _connection.SendMessage(reply);
            }
        }


        private void HandleSupportingDataMessage(Message msg)
        {
            lock (_awaitingSupportingData)
            {
                var message = (ServerSupportDataMessage)msg;
                _awaitingSupportingData.Remove(message.DllName);
                var dll = _dllMonitor.GetLoadedDll(message.DllName);
                if (dll == null)
                    return;

                dll.SharedMemory.SetCurrentSupportingData(message.Data);
                _supportingDataVersions[message.DllName] = message.Version;
            }
        }


        public void StopDoingJobs()
        {
            _doWork = false;
        }


        public void StartDoingJobs()
        {
            _doWork = true;
            QueueNewJobs();
        }


        private void JobCompleted()
        {
            lock (this)
            {
                _jobsInProcess--;
                QueueNewJobs();
            }
        }


        private void QueueNewJobs()
        {
            if (_doWork == false)
                return;

            var failed = new List<WrappedJobData>();

            lock (this)
            {
                var empty = false;
                while (!empty && _jobsInProcess < TargetNumberOfWorkers)
                {
                    var nextJob = _jobPool.GetNextJob(false);
                    if (nextJob != null)
                    {
                        var success = QueueJob(nextJob);
                        if (!success)
                            failed.Add(nextJob);
                    }
                    else
                    {
                        empty = true;
                    }
                }

                foreach (var job in failed)
                {
                    _jobPool.ReturnJobToPool(job);
                }
            }
        }


        private bool QueueJob(WrappedJobData data)
        {
            var dll = _dllMonitor.GetLoadedDll(data.DllName);
            if (dll == null || !dll.IsRunning())
                return false;

            lock (_supportingDataVersions)
            {
                if (!_supportingDataVersions.ContainsKey(data.DllName) ||
                 _supportingDataVersions[data.DllName] < data.SupportingDataVersion)
                {
                    GetLatestSupportingData(data.DllName);
                }
            }

            _jobsInProcess++;
            dll.SharedMemory.AddJobData(data);
            return true;
        }


        private void SendJobResults(WrappedResultData result)
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


        private void DllRemovedHandler(string dllName)
        {
            lock (_supportingDataVersions)
            {
                if (_supportingDataVersions.ContainsKey(dllName))
                    _supportingDataVersions.Remove(dllName);
            }
        }


        private void RegisterDllRemovedEvents(string dllName)
        {
            var dll = _dllMonitor.GetLoadedDll(dllName);
            dll.ProcessTerminatedGracefully += DllRemovedHandler;
            dll.ProcessTerminatedUnexpectedly += DllRemovedHandler;
        }


        private void RegisterJobCompletedEvents(string dllName)
        {
            var dll = _dllMonitor.GetLoadedDll(dllName);

            dll.SharedMemory.JobCompleted += x => JobCompleted();
            dll.SharedMemory.JobCompleted += SendJobResults;

            dll.ProcessTerminatedGracefully += p => ForceUpdateWorkerCount();
            dll.ProcessTerminatedUnexpectedly += p => ForceUpdateWorkerCount();
        }
    }
}
