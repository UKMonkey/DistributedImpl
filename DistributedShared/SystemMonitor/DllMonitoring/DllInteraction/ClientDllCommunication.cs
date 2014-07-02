using DistributedSharedInterfaces.Jobs;
using DistributedShared.Network;

namespace DistributedShared.SystemMonitor.DllMonitoring.DllInteraction
{
    public delegate void JobResultCallback (IJobResultData item);
    public delegate void JobDataCallback (IJobData item);
    public delegate void JobGroupCallback(IJobGroup item);

    public class ClientDllCommunication : DllCommunication
    {
        public event JobResultCallback JobCompleted;
        public event JobDataCallback JobAvailable;


        public ClientDllCommunication(MessageManager messageManager)
            :base(messageManager)
        {}


        /// <summary>
        /// Adds a job to be immediately be processed by the worker process
        /// </summary>
        /// <param name="job"></param>
        public void AddJobData(IJobData job)
        {
        }


        /// <summary>
        /// Adds job results to be immediately processed by the core process
        /// </summary>
        /// <param name="result"></param>
        public void AddCompletedData(IJobResultData result)
        {

        }


        /// <summary>
        /// Gets the total number of jobs that have passed into this process
        /// but not had a result provided.  It is assumed that this number has 1 thread
        /// working on it.
        /// </summary>
        /// <returns></returns>
        public int GetCurrentWorkerCount()
        {
            return 0;
        }
    }
}
