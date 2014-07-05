﻿using System;
using System.Threading;
using DistributedClientInterfaces.Interfaces;
using DistributedClientDll.SystemMonitor;
using DistributedShared.Jobs;
using System.Collections.Generic;
using DistributedClientShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedClientShared.SystemMonitor.DllMonitoring.DllInteraction.Messages;

namespace DistributedClientWorker.Jobs
{
    public class DllWorker : IClientApi, IDisposable
    {
        private IDllApi _brain;
        private readonly ReaderWriterLock _workLock;
        private readonly WorkerDllCommunication _communication;
        private bool _awaitingSupportingDataUpdate = false;


        public DllWorker(WorkerDllCommunication communication)
        {
            _communication = communication;

            _communication.OnWorkRequest += QueueNewJob;
            _communication.OnSupportingDataUpdate += SetSupportingData;

            _workLock = new ReaderWriterLock();
        }


        public void SetDllApi(IDllApi api)
        {
            _brain = api;
        }


        private void QueueNewJob(WrappedJobData job)
        {
            while (_brain == null)
                Thread.Yield();

            int availWorkers, availPort;
            int maxWorkers, maxPort;
            ThreadPool.GetAvailableThreads(out availWorkers, out availPort);
            ThreadPool.GetMaxThreads(out maxWorkers, out maxPort);

            if (availWorkers == 0)
            {
                // update the thread pool to spawn some more threads for us.
                ThreadPool.SetMaxThreads(maxWorkers + 1, maxPort);
            }

            ThreadPool.QueueUserWorkItem(DoWork, job);
        }


        private void DoWork(object jb)
        {
            var job = (WrappedJobData)jb;

            var timer = new HiResTimer();
            long start;
            long end;
            byte[] result;

            try
            {
                while (_awaitingSupportingDataUpdate)
                    Thread.Yield();

                _workLock.AcquireReaderLock(500000);

                start = timer.Value;
                result = _brain.ProcessJob(job);
                end = timer.Value;
            }
            finally
            {
                _workLock.ReleaseReaderLock();
            }

            var fullResult = new WrappedResultData
                { JobId = job.JobId,
                GroupId = job.GroupId,
                CyclesSpentWorking = end - start,
                CyclesSpentWorkingIsReliable = timer.Reliable,
                Data = result,
                DllName = job.DllName };

            var msg = new ClientJobCompleteMessage() { Result = fullResult };
            _communication.SendMessage(msg);
        }


        private void SetSupportingData(Dictionary<String, byte[]> data)
        {
            while (_brain == null)
                Thread.Yield();
            var thread = new Thread(() => SetSupportingDataInternal(data));
            thread.Start();
        }


        private void SetSupportingDataInternal(Dictionary<String, byte[]> data)
        {
            try
            {
                _awaitingSupportingDataUpdate = true;
                _workLock.AcquireWriterLock(500000);
                _brain.SupportingData = data;
                _awaitingSupportingDataUpdate = false;
            }
            finally
            {
                _workLock.ReleaseWriterLock();
            }
        }


        public void Dispose()
        {
        }
    }
}
