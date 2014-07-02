using System;
using System.Runtime.InteropServices;


// largely from
// http://msdn.microsoft.com/en-us/library/aa964692(VS.80).aspx


namespace DistributedClientDll.SystemMonitor
{
    public class HiResTimer
    {
        private readonly bool _isPerfCounterSupported;
        private readonly long _frequency;

        // Windows CE native library with QueryPerformanceCounter().
        private const string Lib = "kernel32.dll";
        [DllImport(Lib)]
        private static extern int QueryPerformanceCounter(ref long count);
        [DllImport(Lib)]
        private static extern int QueryPerformanceFrequency(ref long frequency);

        public HiResTimer()
        {
            _frequency = 0;
            _isPerfCounterSupported = false;

            // Query the high-resolution timer only if it is supported.
            // A returned frequency of 1000 typically indicates that it is not
            // supported and is emulated by the OS using the same value that is
            // returned by Environment.TickCount.
            // A return value of 0 indicates that the performance counter is
            // not supported.
            var returnVal = QueryPerformanceFrequency(ref _frequency);

            if (returnVal != 0 && _frequency != 1000)
            {
                // The performance counter is supported.
                _isPerfCounterSupported = true;
            }
            else
            {
                // The performance counter is not supported. Use
                // Environment.TickCount instead.
                _frequency = 1000;
            }
        }

        public long Frequency
        {
            get
            {
                return _frequency;
            }
        }

        public long Value
        {
            get
            {
                long tickCount = 0;

                if (_isPerfCounterSupported)
                {
                    // Get the value here if the counter is supported.
                    QueryPerformanceCounter(ref tickCount);
                    return tickCount;
                }
                // Otherwise, use Environment.TickCount.
                return Environment.TickCount;
            }
        }

        public bool Reliable
        {
            get { return _isPerfCounterSupported;  }
        }
    }
}
