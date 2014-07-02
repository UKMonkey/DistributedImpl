//http://vmccontroller.codeplex.com/SourceControl/changeset/view/47386#195318

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using DistributedShared.SystemMonitor.HandleMonitoring;
using DistributedShared.SystemMonitor.Managers;

namespace DistributedShared.SystemMonitor
{
    public delegate void FileCallback(FileInfo fileName);
    public delegate void ThreadCallback(ProcessThread thread);
    public delegate void NetworkCallback(OpenHandle.PortInfo portData);
    public delegate void ProcessCallback(Process processName);


    // Monitors threads, file handles and network connections for this process
    // raises events when one is opened.
    public class HandleMontior : IDisposable
    {
        public event FileCallback FileOpened;
        public event ThreadCallback ThreadCreated;
        public event NetworkCallback NetworkConnection;
        public event ProcessCallback ProcessCreated;

        private volatile bool _doWork;

        private HashSet<string> _filesAccessed = new HashSet<string>();
        private HashSet<int> _threadIds = new HashSet<int>();
        private HashSet<int> _processIds = new HashSet<int>();
        private HashSet<string> _usedPorts = new HashSet<string>(); 

        private readonly Thread _monitorThread;
        private readonly OpenHandleDetector _fileHandleDetector = new OpenHandleDetector();
        private readonly ProcessHandleDetector _processHandleDetector = new ProcessHandleDetector();
        private readonly ThreadHandleDetector _threadHandleDetector = new ThreadHandleDetector();
        private readonly PortHandleDetector _portHandleDetector = new PortHandleDetector();


        public HandleMontior()
        {
            _monitorThread = new Thread(MonitorThreadMain);
            StaticThreadManager.Instance.StartNewThread(_monitorThread, "MonitorThread");
            _doWork = true;

            DoWork();
        }


        public void Dispose()
        {
            _doWork = false;

            _monitorThread.Abort();
            _monitorThread.Join();
        }


        private IEnumerable<OpenHandle> GetOpenHandles()
        {
            return _processHandleDetector.GetProcessHandleInformation().Concat
                (_fileHandleDetector.GetOpenFiles()).Concat
                (_threadHandleDetector.GetThreadHandleInformation()).Concat
                (_portHandleDetector.GetPortHandles());
        }


        private void DoWork()
        {
            var filesAccessed = new HashSet<string>();
            var threadIds = new HashSet<int>();
            var processIds = new HashSet<int>();
            var usedPorts = new HashSet<string>();

            var openHandles = GetOpenHandles().ToList();

            foreach (var handle in openHandles)
            {
                if (handle.Thread != null && !_threadIds.Contains(handle.Thread.Id))
                {
                    threadIds.Add(handle.Thread.Id);
                    if (ThreadCreated != null)
                        ThreadCreated(handle.Thread);
                }

                else if (handle.FileInfo != null && !_filesAccessed.Contains(handle.FileInfo.FullName))
                {
                    filesAccessed.Add(handle.FileInfo.FullName);
                    if (FileOpened != null)
                        FileOpened(handle.FileInfo);
                }

                else if (handle.Process != null && !_processIds.Contains(handle.Process.Id))
                {
                    processIds.Add(handle.Process.Id);
                    if (ProcessCreated != null)
                        ProcessCreated(handle.Process);
                }

                else if (handle.Port != null && _usedPorts.Contains(handle.Port.ToString()))
                {
                    usedPorts.Add(handle.Port.ToString());
                    if (NetworkConnection != null)
                        NetworkConnection(handle.Port.Value);
                }
            }

            _filesAccessed = filesAccessed;
            _usedPorts = usedPorts;
            _threadIds = threadIds;
            _processIds = processIds;
        }

        private void MonitorThreadMain()
        {
            while (_doWork)
            {
                DoWork();
                Thread.Sleep(100);
            }
        }
   }
}