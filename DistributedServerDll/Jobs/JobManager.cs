using System;
using System.Collections.Generic;
using System.Linq;
using DistributedServerInterfaces.Networking;
using DistributedSharedInterfaces.Jobs;
using DistributedSharedInterfaces.Messages;
using DistributedShared.Network.Messages;
using DistributedSharedInterfaces.Network;
using DistributedServerDll.SystemMonitor.DllMonitoring;
using DistributedShared.Jobs;

namespace DistributedServerDll.Jobs
{
    public class JobManager
    {
        private readonly Dictionary<string, RequestProcessor> _requestProcessors = new Dictionary<string, RequestProcessor>();
        private readonly IConnectionManager _connectionManager;
        private readonly ServerDllMonitor _dllMonitor;
        private readonly Random _randGenerator = new Random();
        private readonly String _storePath;

        public JobManager(ServerDllMonitor dllMonitor, 
                          IConnectionManager connectionManager,
                          String storePath)
        {
            _storePath = storePath;
            _dllMonitor = dllMonitor;
            _dllMonitor.DllLoaded += PrepareNewJobHandler;

            _connectionManager = connectionManager;
            _connectionManager.RegisterMessageListener(typeof(ClientGetLatestSupportData), HandleGetLatestSupportDataRequest);
            _connectionManager.RegisterMessageListener(typeof(ClientGetNewJobsMessage), HandleClientRequestJobs);
            _connectionManager.RegisterMessageListener(typeof(ClientJobComplete), HandleClientCompleteJobs);
        }


        private void PrepareNewJobHandler(string dll)
        {
            lock (this)
            {
                var managedDll = _dllMonitor.GetLoadedDll(dll);
                _requestProcessors.Add(dll, new RequestProcessor(dll, managedDll, _storePath));

                managedDll.ProcessTerminatedGracefully += RemoveJobHandler;
                managedDll.ProcessTerminatedUnexpectedly += RemoveJobHandler;
            }
        }


        private void HandleClientCompleteJobs(IConnection con, Message data)
        {
            var msg = (ClientJobComplete)data;
            lock (this)
            {
                foreach (var result in msg.JobResults)
                {
                    if (!_requestProcessors.ContainsKey(result.DllName))
                        continue;

                    RequestProcessor proc = _requestProcessors[result.DllName];
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
            var processor = GetRandomProcessor();
            var msg = (ClientGetNewJobsMessage) data;

            IEnumerable<WrappedJobData> jobs = 
                processor == null ?
                    new List<WrappedJobData>() : 
                    processor.GetJobs(msg.NumberOfJobs).ToList();

            var reply = new ServerJobMessage();
            reply.JobData.AddRange(jobs);

            _connectionManager.SendMessage(con, reply);
        }


        private void HandleGetLatestSupportDataRequest(IConnection con, Message data)
        {
            var msg = (ClientGetLatestSupportData)data;
            Message reply;

            lock (this)
            {
                if (!_requestProcessors.ContainsKey(msg.DllName))
                {
                    reply = new ServerUnrecognisedDllMessage { DllName = msg.DllName };
                }
                else
                {
                    reply = new ServerSupportDataMessage
                                {
                                    DllName = msg.DllName,
                                };
                    _requestProcessors[msg.DllName].CopySupportingData(((ServerSupportDataMessage)reply).Data);
                }
            }

            _connectionManager.SendMessage(con, reply);
        }
    }
}
