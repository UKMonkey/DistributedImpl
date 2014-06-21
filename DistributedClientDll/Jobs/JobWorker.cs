using System.Collections.Generic;
using System.Linq;
using DistributedShared.SystemMonitor;
using DistributedShared.SystemMonitor.Managers;
using DistributedSharedInterfaces.Jobs;
using DistributedShared.Network.Messages;
using System.Threading;
using DistributedSharedInterfaces.Messages;
using DistributedClientInterfaces.Interfaces;
using DistributedClientDll.Networking;
using DistributedClientDll.Wrappers;

namespace DistributedClientDll.Jobs
{
    public class JobWorker
    {
        private readonly DllMonitor _dllMonitor;
        private readonly ConnectionManager _connection;

        private readonly Dictionary<string, DllWorker> _brains = new Dictionary<string, DllWorker>();
        private readonly Dictionary<Thread, DllWorker> _threadToBrain = new Dictionary<Thread, DllWorker>();
        private readonly Dictionary<Thread, IJobData> _threadToJob = new Dictionary<Thread, IJobData>();
        private readonly HashSet<string> _requestedSupportData = new HashSet<string>();

        public JobWorker(ConnectionManager connection, DllMonitor dllMonitor)
        {
            _dllMonitor = dllMonitor;
            _connection = connection;

            _dllMonitor.DllLoaded += GenerateNewHandler;
            _dllMonitor.DllUnloaded += HandleDllUnloaded;

            connection.RegisterMessageListener(typeof(ServerCancelWorkMessage), HandleCancelWorkMessage);
            connection.RegisterMessageListener(typeof(SupportDataVersionMessage), HandleSupportingDataVersionMessage);
            connection.RegisterMessageListener(typeof(ServerSupportDataMessage), HandleSupportingDataMessage);

            _requestedSupportData = new HashSet<string>();
        }


        public bool ProcessJob(IJobData job)
        {
            Thread thread;
            lock (this)
            {
                if (!_brains.ContainsKey(job.DllName))
                    return false;

                var brain = _brains[job.DllName];

                // we can only do this job if the supporting data md5 required matches
                // the current supporting data md5

                if (brain.SupportingDataVersion != job.SupportingDataVersion)
                {
                    if (_requestedSupportData.Contains(job.DllName))
                        return false;

                    _requestedSupportData.Add(job.DllName);
                    _connection.SendMessage(new ClientGetLatestSupportData {DllName = job.DllName});
                    return false;
                }

                thread = new Thread(ProcessJobWorkerMain);
                _threadToBrain.Add(thread, brain);
                _threadToJob.Add(thread, job);
            }

            StaticThreadManager.Instance.StartNewThread(thread, "ShortTermJobWorker");
            thread.Join();
            return true;
        }


        private void ProcessJobWorkerMain()
        {
            try
            {
                var msg = new ClientJobComplete();
                IJobData job;
                DllWorker brain;

                lock (this)
                {
                    job = _threadToJob[Thread.CurrentThread];
                    brain = _threadToBrain[Thread.CurrentThread];
                }

                var result = brain.DoWork(job);
                msg.JobResults.Add(result);

                _connection.SendMessage(msg);
            }
            finally 
            {
                lock (this)
                {
                    _threadToBrain.Remove(Thread.CurrentThread);
                    _threadToJob.Remove(Thread.CurrentThread);
                }
            }
        }


        private void GenerateNewHandler(string dllName)
        {
            lock (this)
            {
                var brain = _dllMonitor.GetNewTypeFromDll<IDllApi>(dllName);

                if (brain == null)
                    return;

                _brains.Add(dllName, new DllWorker(brain));
                brain.OnDllLoaded(_brains[dllName]);
            }
        }


        private void AbortWorkForDll(string dllName)
        {
            List<Thread> threadsToTerminate;
            lock (this)
            {
                threadsToTerminate = _threadToJob.Where(item => item.Value.DllName == dllName).Select(item => item.Key).ToList();
            }

            foreach (var thread in threadsToTerminate)
                thread.Abort();

            foreach (var thread in threadsToTerminate)
                thread.Join();
        }


        private void HandleDllUnloaded(string dllName)
        {
            AbortWorkForDll(dllName);
        }


        private void HandleCancelWorkMessage(Message data)
        {
            var msg = (ServerCancelWorkMessage) data;
            AbortWorkForDll(msg.DllName);
        }


        private void HandleSupportingDataVersionMessage(Message data)
        {
            var msg = (SupportDataVersionMessage) data;
            lock (this)
            {
                if (!_brains.ContainsKey(msg.DllName))
                    return;

                var brain = _brains[msg.DllName];
                if (brain.SupportingDataVersion == msg.Version)
                    return;
            }

            var reply = new ClientGetLatestSupportData { DllName = msg.DllName };
            _connection.SendMessage(reply);
        }


        private void HandleSupportingDataMessage(Message data)
        {
            var msg = (ServerSupportDataMessage) data;
            DllWorker brain;

            lock (this)
            {
                if (_requestedSupportData.Contains(msg.DllName))
                    _requestedSupportData.Remove(msg.DllName);

                if (!_brains.ContainsKey(msg.DllName))
                    return;

                brain = _brains[msg.DllName];
            }

            brain.SetSupportingData(msg.Data, msg.Version);
        }
    }
}
