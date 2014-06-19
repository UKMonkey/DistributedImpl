using System;
using System.Collections.Generic;
using System.Linq;
using DistributedServerDll.Networking;
using DistributedShared.SystemMonitor;
using DistributedServerInterfaces.Networking;
using DistributedServerInterfaces.Interfaces;
using DistributedSharedInterfaces.Jobs;
using DistributedSharedInterfaces.Messages;
using DistributedShared.Network.Messages;
using DistributedShared.Network;
using DistributedSharedInterfaces.Network;

namespace DistributedServerDll.Jobs
{
    public class JobManager
    {
        private readonly Dictionary<string, RequestProcessor> _requestProcessors = new Dictionary<string, RequestProcessor>();
        private readonly IConnectionManager _connectionManager;
        private readonly DllMonitor _dllMonitor;
        private readonly Random _randGenerator = new Random();

        public JobManager(DllMonitor dllMonitor, 
                          IConnectionManager connectionManager)
        {
            _dllMonitor = dllMonitor;
            _dllMonitor.DllLoaded += PrepareNewJobHandler;
            _dllMonitor.DllUnloaded += RemoveJobHandler;

            _connectionManager = connectionManager;
            _connectionManager.RegisterMessageListener(typeof(ClientGetSupportDataMd5Message), HandleGetSupportMd5Request);
            _connectionManager.RegisterMessageListener(typeof(ClientGetLatestSupportData), HandleGetLatestSupportDataRequest);
            _connectionManager.RegisterMessageListener(typeof(ClientGetNewJobsMessage), HandleClientRequestJobs);
            _connectionManager.RegisterMessageListener(typeof(ClientJobComplete), HandleClientCompleteJobs);
        }


        private void PrepareNewJobHandler(string dll)
        {
            lock (this)
            {
                var dllWorker = _dllMonitor.GetNewTypeFromDll<IDllApi>(dll);
                if (dllWorker == null)
                    return;

                _requestProcessors.Add(dll, new RequestProcessor(dll, dllWorker, _connectionManager));
                dllWorker.OnDllLoaded(_requestProcessors[dll]);
            }
        }


        private void HandleClientCompleteJobs(IConnection con, Message data)
        {
            var msg = (ClientJobComplete)data;
            lock (this)
            {
                foreach (var result in msg.JobResults)
                {
                    RequestProcessor proc;
                    if (!_requestProcessors.ContainsKey(result.DllName))
                        continue;

                    proc = _requestProcessors[result.DllName];
                    proc.JobComplete(result);
                }
            }
        }


        private void RemoveJobHandler(string dllName)
        {
            lock (this)
            {
                if (!_requestProcessors.ContainsKey(dllName))
                    return;
                _requestProcessors[dllName].Dispose();
                _requestProcessors.Remove(dllName);
            }
        }


        private RequestProcessor GetRandomProcessor()
        {
            lock (this)
            {
                var processors = _requestProcessors.Values.
                    Where(proc => proc.IsAcceptingRequests()).
                    ToList();

                if (processors.Count == 0)
                    return null;

                var random = _randGenerator.Next(processors.Count);
                return processors[random];
            }
        }


        private void HandleClientRequestJobs(IConnection con, Message data)
        {
            IEnumerable<IJobData> jobs;
            var processor = GetRandomProcessor();
            var msg = (ClientGetNewJobsMessage) data;

            jobs = processor == null ? 
                new List<IJobData>() : processor.GetJobs(msg.NumberOfJobs).ToList();

            var reply = new ServerJobMessage();
            reply.JobData.AddRange(jobs);

            _connectionManager.SendMessage(con, reply);
        }


        private void HandleGetSupportMd5Request(IConnection con, Message data)
        {
            var msg = (ClientGetSupportDataMd5Message) data;
            Message reply = null;

            lock (this)
            {
                if (!_requestProcessors.ContainsKey(msg.DllName))
                    reply = new ServerUnrecognisedDllMessage {DllName = msg.DllName};
                else
                    reply = new ServerSupportMd5Message
                                {
                                    DllName = msg.DllName,
                                    Md5 = _requestProcessors[msg.DllName].GetSupportingDataMd5(),
                                };
            }

            _connectionManager.SendMessage(con, reply);
        }


        private void HandleGetLatestSupportDataRequest(IConnection con, Message data)
        {
            var msg = (ClientGetLatestSupportData)data;
            Message reply = null;

            lock (this)
            {
                if (!_requestProcessors.ContainsKey(msg.DllName))
                    reply = new ServerUnrecognisedDllMessage {DllName = msg.DllName};
                else
                    reply = new ServerSupportDataMessage
                                {
                                    DllName = msg.DllName,
                                    Md5 = _requestProcessors[msg.DllName].GetSupportingDataMd5(),
                                    Data = _requestProcessors[msg.DllName].GetSupportingData()
                                };
            }

            _connectionManager.SendMessage(con, reply);
        }
    }
}
