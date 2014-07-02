using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using NT_STATUS = DistributedShared.SystemMonitor.HandleMonitoring.NativeMethods.NT_STATUS;
using SafeProcessHandle = DistributedShared.SystemMonitor.HandleMonitoring.NativeMethods.SafeProcessHandle;
using SafeObjectHandle = DistributedShared.SystemMonitor.HandleMonitoring.NativeMethods.SafeObjectHandle;
using OBJECT_INFORMATION_CLASS = DistributedShared.SystemMonitor.HandleMonitoring.NativeMethods.OBJECT_INFORMATION_CLASS;
using SYSTEM_HANDLE_ENTRY = DistributedShared.SystemMonitor.HandleMonitoring.NativeMethods.SYSTEM_HANDLE_ENTRY;
using ProcessAccessRights = DistributedShared.SystemMonitor.HandleMonitoring.NativeMethods.ProcessAccessRights;
using DuplicateHandleOptions = DistributedShared.SystemMonitor.HandleMonitoring.NativeMethods.DuplicateHandleOptions;


namespace DistributedShared.SystemMonitor.HandleMonitoring
{
    public class FileHandleProcessor
    {
        private static Dictionary<string, string> _deviceMap;
        private const string NetworkDevicePrefix = "\\Device\\LanmanRedirector\\";

        private const int MaxPath = 260;


        public static OpenHandle GetFileHandleInformation(IntPtr handle, SYSTEM_HANDLE_ENTRY handleEntry)
        {
            string devicePath;
            if (!GetFileNameFromHandle(handle, handleEntry.OwnerPid, out devicePath))
                return new OpenHandle(OpenHandle.HandleType.File);

            string dosPath;
            if (ConvertDevicePathToDosPath(devicePath, out dosPath))
            {
                if (File.Exists(dosPath))
                {
                    return new OpenHandle(new FileInfo(dosPath));
                }
                if (Directory.Exists(dosPath))
                {
                    return new OpenHandle(new DirectoryInfo(dosPath));
                }
            }
            return new OpenHandle(OpenHandle.HandleType.File);
        }


        private static bool GetFileNameFromHandle(IntPtr handle, int processId, out string fileName)
        {
            var currentProcess = NativeMethods.GetCurrentProcess();
            var remote = (processId != NativeMethods.GetProcessId(currentProcess));
            SafeProcessHandle processHandle = null;
            SafeObjectHandle objectHandle = null;
            try
            {
                if (remote)
                {
                    processHandle = NativeMethods.OpenProcess(ProcessAccessRights.PROCESS_DUP_HANDLE, true, processId);
                    if (NativeMethods.DuplicateHandle(processHandle.DangerousGetHandle(), handle, currentProcess, out objectHandle, 0, false, DuplicateHandleOptions.DUPLICATE_SAME_ACCESS))
                    {
                        handle = objectHandle.DangerousGetHandle();
                    }
                }
                return GetFileNameFromHandle(handle, out fileName, 200);
            }
            finally
            {
                if (remote)
                {
                    if (processHandle != null)
                    {
                        processHandle.Close();
                    }
                    if (objectHandle != null)
                    {
                        objectHandle.Close();
                    }
                }
            }
        }
        private static bool GetFileNameFromHandle(IntPtr handle, out string fileName, int wait)
        {
            var f = new FileNameFromHandleState(handle);
            ThreadPool.QueueUserWorkItem(new WaitCallback(GetFileNameFromHandle), f);
            if (f.WaitOne(wait))
            {
                fileName = f.FileName;
                return f.RetValue;
            }

            fileName = string.Empty;
            return false;
        }

        private class FileNameFromHandleState
        {
            private readonly ManualResetEvent _mr;
            private readonly IntPtr _handle;

            public IntPtr Handle
            {
                get
                {
                    return _handle;
                }
            }

            public string FileName { get; set; }
            public bool RetValue { get; set; }

            public FileNameFromHandleState(IntPtr handle)
            {
                _mr = new ManualResetEvent(false);
                _handle = handle;
            }

            public bool WaitOne(int wait)
            {
                return _mr.WaitOne(wait, false);
            }

            public void Set()
            {
                _mr.Set();
            }
        }

        private static void GetFileNameFromHandle(object state)
        {
            var s = (FileNameFromHandleState)state;
            string fileName;
            s.RetValue = GetFileNameFromHandle(s.Handle, out fileName);
            s.FileName = fileName;
            s.Set();
        }

        private static bool GetFileNameFromHandle(IntPtr handle, out string fileName)
        {
            IntPtr ptr = IntPtr.Zero;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                int length = 0x200;  // 512 bytes
                RuntimeHelpers.PrepareConstrainedRegions();
                try { }
                finally
                {
                    // CER guarantees the assignment of the allocated 
                    // memory address to ptr, if an ansynchronous exception 
                    // occurs.
                    ptr = Marshal.AllocHGlobal(length);
                }
                NT_STATUS ret = NativeMethods.NtQueryObject(handle, OBJECT_INFORMATION_CLASS.ObjectNameInformation, ptr, length, out length);
                if (ret == NT_STATUS.STATUS_BUFFER_OVERFLOW)
                {
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try { }
                    finally
                    {
                        // CER guarantees that the previous allocation is freed,
                        // and that the newly allocated memory address is 
                        // assigned to ptr if an asynchronous exception occurs.
                        Marshal.FreeHGlobal(ptr);
                        ptr = Marshal.AllocHGlobal(length);
                    }
                    ret = NativeMethods.NtQueryObject(handle, OBJECT_INFORMATION_CLASS.ObjectNameInformation, ptr, length, out length);
                }
                if (ret == NT_STATUS.STATUS_SUCCESS)
                {
                    fileName = Marshal.PtrToStringUni((IntPtr)((int)ptr + 8), (length - 9) / 2);
                    return fileName.Length != 0;
                }
            }
            finally
            {
                // CER guarantees that the allocated memory is freed, 
                // if an asynchronous exception occurs.
                Marshal.FreeHGlobal(ptr);
            }

            fileName = string.Empty;
            return false;
        }


        private static bool ConvertDevicePathToDosPath(string devicePath, out string dosPath)
        {
            EnsureDeviceMap();
            int i = devicePath.Length;
            while (i > 0 && (i = devicePath.LastIndexOf('\\', i - 1)) != -1)
            {
                string drive;
                if (_deviceMap.TryGetValue(devicePath.Substring(0, i), out drive))
                {
                    dosPath = string.Concat(drive, devicePath.Substring(i));
                    return dosPath.Length != 0;
                }
            }
            dosPath = string.Empty;
            return false;
        }


        private static void EnsureDeviceMap()
        {
            if (_deviceMap == null)
            {
                Dictionary<string, string> localDeviceMap = BuildDeviceMap();
                Interlocked.CompareExchange<Dictionary<string, string>>(ref _deviceMap, localDeviceMap, null);
            }
        }


        private static Dictionary<string, string> BuildDeviceMap()
        {
            string[] logicalDrives = Environment.GetLogicalDrives();
            var localDeviceMap = new Dictionary<string, string>(logicalDrives.Length);
            var lpTargetPath = new StringBuilder(MaxPath);
            foreach (string drive in logicalDrives)
            {
                string lpDeviceName = drive.Substring(0, 2);
                NativeMethods.QueryDosDevice(lpDeviceName, lpTargetPath, MaxPath);
                localDeviceMap.Add(NormalizeDeviceName(lpTargetPath.ToString()), lpDeviceName);
            }
            localDeviceMap.Add(NetworkDevicePrefix.Substring(0, NetworkDevicePrefix.Length - 1), "\\");
            return localDeviceMap;
        }


        private static string NormalizeDeviceName(string deviceName)
        {
            if (string.Compare(deviceName, 0, NetworkDevicePrefix, 0, NetworkDevicePrefix.Length, StringComparison.InvariantCulture) == 0)
            {
                string shareName = deviceName.Substring(deviceName.IndexOf('\\', NetworkDevicePrefix.Length) + 1);
                return string.Concat(NetworkDevicePrefix, shareName);
            }
            return deviceName;
        }
    }
}
