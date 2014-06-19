using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;



// largely from
// http://msdn.microsoft.com/en-us/library/aa964692(VS.80).aspx


namespace DistributedClientDll.SystemMonitor
{
    public class HiResTimer
    {
        private bool isPerfCounterSupported = false;
        private long frequency = 0;

        // Windows CE native library with QueryPerformanceCounter().
        private const string lib = "kernel32.dll";
        [DllImport(lib)]
        private static extern int QueryPerformanceCounter(ref long count);
        [DllImport(lib)]
        private static extern int QueryPerformanceFrequency(ref long frequency);

        public HiResTimer()
        {
            // Query the high-resolution timer only if it is supported.
            // A returned frequency of 1000 typically indicates that it is not
            // supported and is emulated by the OS using the same value that is
            // returned by Environment.TickCount.
            // A return value of 0 indicates that the performance counter is
            // not supported.
            int returnVal = QueryPerformanceFrequency(ref frequency);

            if (returnVal != 0 && frequency != 1000)
            {
                // The performance counter is supported.
                isPerfCounterSupported = true;
            }
            else
            {
                // The performance counter is not supported. Use
                // Environment.TickCount instead.
                frequency = 1000;
            }
        }

        public long Frequency
        {
            get
            {
                return frequency;
            }
        }

        public long Value
        {
            get
            {
                long tickCount = 0;

                if (isPerfCounterSupported)
                {
                    // Get the value here if the counter is supported.
                    QueryPerformanceCounter(ref tickCount);
                    return tickCount;
                }
                else
                {
                    // Otherwise, use Environment.TickCount.
                    return (long)Environment.TickCount;
                }
            }
        }

        public bool Reliable
        {
            get { return isPerfCounterSupported;  }
        }
    }
}
