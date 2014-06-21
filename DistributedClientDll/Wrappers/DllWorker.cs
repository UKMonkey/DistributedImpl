using System;
using System.Threading;
using DistributedClientInterfaces.Interfaces;
using System.Data.SQLite;
using DistributedSharedInterfaces.Jobs;
using DistributedClientDll.SystemMonitor;

namespace DistributedClientDll.Wrappers
{
    public class DllWorker : IClientApi, IDisposable
    {
        public long SupportingDataVersion { get; private set; }

        private readonly IDllApi _brain;
        private readonly ReaderWriterLock _workLock;

        public DllWorker(IDllApi brain)
        {
            _brain = brain;
            _workLock = new ReaderWriterLock();
        }


        public IJobResultData DoWork(IJobData job)
        {
            var timer = new HiResTimer();
            long start;
            long end;
            byte[] result;

            try
            {
                _workLock.AcquireReaderLock(1000);

                start = timer.Value;
                result = _brain.ProcessJob(job);
                end = timer.Value;
            }
            finally
            {
                _workLock.ReleaseReaderLock();
            }


            return new JobResultData
                { JobId = job.JobId,
                CyclesSpentWorking = end - start,
                CyclesSpentWorkingIsReliable = timer.Reliable,
                Data = result,
                DllName = job.DllName };
        }


        public void SetSupportingData(byte[] data, long version)
        {
            try
            {
                _workLock.AcquireWriterLock(5000);
                _brain.SupportingData = data;
                SupportingDataVersion = version;
            }
            finally
            {
                _workLock.ReleaseWriterLock();
            }

        }


        public void Dispose()
        {
            _brain.Dispose();
        }


        public SQLiteConnection GetSQInterface()
        {
            return null;
        }
    }
}
