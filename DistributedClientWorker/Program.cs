using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedClientDll.SystemMonitor.DllMonitoring;

namespace DistributedClientWorker
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Diagnostics.Debugger.Break();

            var sharedMemory = new ClientSharedMemory();
            sharedMemory.DllName = "";
            sharedMemory.SharedMemoryPath = "";

            sharedMemory.Connect(false);
        }
    }
}
