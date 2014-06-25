using DistributedShared.SystemMonitor.DllMonitoring;
using DistributedSharedInterfaces.Jobs;

namespace DistributedClientDll.SystemMonitor.DllMonitoring
{
    public delegate void ClientSharedMemoryCallback(ClientSharedMemory item);

    public class ClientSharedMemory : DllSharedMemory
    {
        public event ClientSharedMemoryCallback JobCompleted;
        public event ClientSharedMemoryCallback JobAvailable;

        public ClientSharedMemory()
        {}


        public void AddJobData(IJobData job)
        {

        }


        public void AddCompletedData(IJobResultData result)
        {

        }


        public IJobResultData GetCompletedData()
        {

        }


        public IJobData GetNextJobData()
        {

        }
    }
}
