using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedShared.SystemMonitor;
using System.IO;
using System.Diagnostics;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction;
using DistributedServerShared.SystemMonitor.DllMonitoring.DllInteraction.Messages;
using DistributedShared.SystemMonitor.DllMonitoring;
using DistributedShared.SystemMonitor.DllMonitoring.DllInteraction.Messages;

namespace DistributedServerWorker.SystemMonitor
{
    public class SecurityHandler : IDisposable
    {
        private readonly HandleMontior _handleMonitor;
        private readonly DllCommunication _communication;
        private readonly HashSet<String> _dirExceptions;

        
        public SecurityHandler(DllCommunication communication)
        {
            _handleMonitor = new HandleMontior();
            _dirExceptions = new HashSet<string>();
            _communication = communication;
            
            // the server should never ... EVER have anything open
            // anything that does attempt this should be shut down immediately

            _handleMonitor.FileOpened += SecurityBreach;
            _handleMonitor.NetworkConnection += SecurityBreach;
            _handleMonitor.ProcessCreated += SecurityBreach;
            _handleMonitor.ThreadCreated += SecurityBreach;
        }


        public void Dispose()
        {
            _handleMonitor.Dispose();
        }


        /// <summary>
        /// register a file that is an exception (ie the dll being loaded!)
        /// </summary>
        /// <param name="fileName"></param>
        public void RegisterValidDirectory(String dirName)
        {
            _dirExceptions.Add(dirName);
        }


        private void SecurityBreach(FileInfo fileData)
        {
            // it all depends on the file on how significant this is...
            // but logging the access is more important than preventing it
            var dir = Path.GetFullPath(Path.GetDirectoryName(fileData.FullName));

            if (_dirExceptions.Contains(dir))
                return;

            var summary = "File " + fileData.FullName + " was accessed";
            var msg = new ClientSecurityBreachMessage() { Reason = StopReason.FileSecurityException, ProblemSummary = summary };
            NotifyOfSecurityBreach(msg);
        }


        private void SecurityBreach(ProcessThread threadData)
        {
            // A new thread is bad because it means that we don't have full control over the threading model
            // and then can't monitor how much time each process is spending doing what
            // it's not really a security risk; but none the less it's not good

            var summary = "A new thread was created";
            var msg = new ClientSecurityBreachMessage() { Reason = StopReason.ThreadHandleExecption, ProblemSummary = summary };
            NotifyOfSecurityBreach(msg);
        }


        private void SecurityBreach(OpenHandle.PortInfo portData)
        {
            // we actually expect the firewall to prevent any attempts succeeding
            // so there's no urgency to die (unlike the process security breach)
            // so we can afford to send our message and then die

            var summary = "A new " + portData.ConnectionType.ToString() + " was opened\n";
            summary += "Remote host: " + portData.RemoteHost + ":" + portData.RemoteHostPort + "\n";
            summary += "Local port: " + portData.LocalPort + "\n";
            summary += "Listening: " + (portData.Listening ? "Yes" : "No");

            var msg = new ClientSecurityBreachMessage() { Reason = StopReason.PortSecurityException, ProblemSummary = summary };
            NotifyOfSecurityBreach(msg);
        }


        private void SecurityBreach(Process processData)
        {
            // a new process is possibly the worst thing in the world to see
            // we must immediately kill that process and terminate ourselves
            // logging this is essential as it will provide the chance to establish 
            // if any harm was actually done

            processData.Kill();

            var summary = "A new process " + processData.ProcessName + " was started";

            var msg = new ClientSecurityBreachMessage() { Reason = StopReason.ProcessSecurityException, ProblemSummary = summary };
            NotifyOfSecurityBreach(msg);
        }


        // notifies the host that we're bailing, and then does so.
        // this function forces termination of the process
        private void NotifyOfSecurityBreach(ClientSecurityBreachMessage msg)
        {
            //Console.WriteLine(msg.Reason);
            //_communication.SendMessage(msg);
            //Environment.Exit(-1);
        }
    }
}
