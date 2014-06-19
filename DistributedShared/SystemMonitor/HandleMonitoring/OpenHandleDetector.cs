// based on source at
// http://vmccontroller.codeplex.com/SourceControl/changeset/view/47386#195318

using System;
using System.EnterpriseServices;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;

using NT_STATUS = DistributedShared.SystemMonitor.HandleMonitoring.NativeMethods.NT_STATUS;
using SafeProcessHandle = DistributedShared.SystemMonitor.HandleMonitoring.NativeMethods.SafeProcessHandle;
using SafeObjectHandle = DistributedShared.SystemMonitor.HandleMonitoring.NativeMethods.SafeObjectHandle;
using SYSTEM_INFORMATION_CLASS = DistributedShared.SystemMonitor.HandleMonitoring.NativeMethods.SYSTEM_INFORMATION_CLASS;
using OBJECT_INFORMATION_CLASS = DistributedShared.SystemMonitor.HandleMonitoring.NativeMethods.OBJECT_INFORMATION_CLASS;
using SYSTEM_HANDLE_ENTRY = DistributedShared.SystemMonitor.HandleMonitoring.NativeMethods.SYSTEM_HANDLE_ENTRY;
using ProcessAccessRights = DistributedShared.SystemMonitor.HandleMonitoring.NativeMethods.ProcessAccessRights;
using DuplicateHandleOptions = DistributedShared.SystemMonitor.HandleMonitoring.NativeMethods.DuplicateHandleOptions;

namespace DistributedShared.SystemMonitor.HandleMonitoring
{
    // Monitors threads, file handles and network connections for this process
    // raises events when one is opened.
    [ComVisible(true), EventTrackingEnabled(true)]
    public class OpenHandleDetector
    {
        private enum SystemHandleType
        {
            OB_TYPE_UNKNOWN = 0,
            OB_TYPE_TYPE = 1,
            OB_TYPE_DIRECTORY,
            OB_TYPE_SYMBOLIC_LINK,
            OB_TYPE_TOKEN,
            OB_TYPE_PROCESS,
            OB_TYPE_THREAD,
            OB_TYPE_UNKNOWN_7,
            OB_TYPE_EVENT,
            OB_TYPE_EVENT_PAIR,
            OB_TYPE_MUTANT,
            OB_TYPE_UNKNOWN_11,
            OB_TYPE_SEMAPHORE,
            OB_TYPE_TIMER,
            OB_TYPE_PROFILE,
            OB_TYPE_WINDOW_STATION,
            OB_TYPE_DESKTOP,
            OB_TYPE_SECTION,
            OB_TYPE_KEY,
            OB_TYPE_PORT,
            OB_TYPE_WAITABLE_PORT,
            OB_TYPE_ALPC_PORT,
            OB_TYPE_UNKNOWN_21,
            OB_TYPE_UNKNOWN_22,
            OB_TYPE_UNKNOWN_23,
            OB_TYPE_UNKNOWN_24,
            //OB_TYPE_CONTROLLER,
            //OB_TYPE_DEVICE,
            //OB_TYPE_DRIVER,
            OB_TYPE_IO_COMPLETION,
            OB_TYPE_FILE,
            OB_TYPE_ETW_REGISTRATION
        };

        private const int handleTypeTokenCount = 29;
        private static readonly string[] handleTypeTokens = new string[] { 
                "", "", "Directory", "SymbolicLink", "Token",
                "Process", "Thread", "Unknown7", "Event", "EventPair", "Mutant",
                "Unknown11", "Semaphore", "Timer", "Profile", "WindowStation",
                "Desktop", "Section", "Key", "Port", "WaitablePort", "ALPC Port",
                "Unknown21", "Unknown22", "Unknown23", "Unknown24", 
                "IoCompletion", "File", "EtwRegistration"
            };


        public IEnumerable<OpenHandle> GetOpenFiles()
        {
            var processId = Process.GetCurrentProcess().Id;
            return GetOpenFiles(processId);
        }

        /// <summary>
        /// Gets the open files enumerator.
        /// </summary>
        /// <param name="processId">The process id.</param>
        /// <returns></returns>
        /// 
        public IEnumerable<OpenHandle> GetOpenFiles(int processId)
        {
            return new OpenFiles(processId).GetEnumerator();
        }

        private sealed class OpenFiles
        {
            private readonly int processId;

            internal OpenFiles(int processId)
            {
                this.processId = processId;
            }

            #region IEnumerable<FileSystemInfo> Members

            private static bool Is64Bits()
            {
                return Marshal.SizeOf(typeof(IntPtr)) == 8;
            }

            public IEnumerable<OpenHandle> GetEnumerator()
            {
                NT_STATUS ret;
                int length = 0x10000;
                // Loop, probing for required memory.

                do
                {
                    IntPtr ptr = IntPtr.Zero;
                    IntPtr allocataedPtr = IntPtr.Zero;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        RuntimeHelpers.PrepareConstrainedRegions();
                        try { }
                        finally
                        {
                            // CER guarantees that the address of the allocated 
                            // memory is actually assigned to ptr if an 
                            // asynchronous exception occurs.
                            allocataedPtr = ptr = Marshal.AllocHGlobal(length);
                        }
                        int returnLength;
                        ret = NativeMethods.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemHandleInformation, ptr, length, out returnLength);

                        while (ret == NT_STATUS.STATUS_INFO_LENGTH_MISMATCH)
                        {
                            // Round required memory up to the nearest 64KB boundary.
                            length = returnLength;
                            Marshal.FreeHGlobal(allocataedPtr);
                            allocataedPtr = ptr = Marshal.AllocHGlobal(length);
                            ret = NativeMethods.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemHandleInformation, ptr, length, out returnLength);
                        }
                            
                        if (ret == NT_STATUS.STATUS_SUCCESS)
                        {
                            long handleCount;
                            if (!Is64Bits())
                                handleCount = Marshal.ReadInt32(ptr);
                            else
                                handleCount = Marshal.ReadInt64(ptr);
                                
                            ptr += sizeof(int);
                            int size = Marshal.SizeOf(typeof(SYSTEM_HANDLE_ENTRY));
                            for (int i = 0; i < handleCount; i++)
                            {
                                SYSTEM_HANDLE_ENTRY handleEntry;
                                if (Is64Bits())
                                {
                                    handleEntry = (SYSTEM_HANDLE_ENTRY)Marshal.PtrToStructure((IntPtr)((int)ptr), typeof(SYSTEM_HANDLE_ENTRY));
                                    ptr = new IntPtr(ptr.ToInt64() + Marshal.SizeOf(handleEntry) + 8);
                                }
                                else
                                {
                                    handleEntry = (SYSTEM_HANDLE_ENTRY)Marshal.PtrToStructure(ptr, typeof(SYSTEM_HANDLE_ENTRY));
                                    ptr = new IntPtr(ptr.ToInt64() + Marshal.SizeOf(handleEntry));
                                }

                                if (handleEntry.OwnerPid != processId)
                                    continue;

                                IntPtr handle = (IntPtr)handleEntry.HandleValue;
                                SystemHandleType handleType;

                                if (!GetHandleType(handle, handleEntry.OwnerPid, out handleType))
                                    continue;

                                switch (handleType)
                                {
                                    case SystemHandleType.OB_TYPE_DIRECTORY:
                                    case SystemHandleType.OB_TYPE_FILE:
                                    {
                                        yield return FileHandleProcessor.GetFileHandleInformation(handle, handleEntry);
                                        break;
                                    }
                                    case SystemHandleType.OB_TYPE_ALPC_PORT:
                                    case SystemHandleType.OB_TYPE_PORT:
                                    case SystemHandleType.OB_TYPE_WAITABLE_PORT:
                                    {
                                        yield return GetPortInformation(handle, handleEntry);
                                        break;
                                    }
                                    default:
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        // CER guarantees that the allocated memory is freed, 
                        // if an asynchronous exception occurs. 
                        Marshal.FreeHGlobal(allocataedPtr);
                        //sw.Flush();
                        //sw.Close();
                    }
                }
                while (ret == NT_STATUS.STATUS_INFO_LENGTH_MISMATCH);
            }

            #endregion
        }

        #region Private Members

        private static OpenHandle GetPortInformation(IntPtr handle, SYSTEM_HANDLE_ENTRY handleEntry)
        {
                
            return new OpenHandle(OpenHandle.HandleType.PORT);
        }

        private static bool GetHandleType(IntPtr handle, int processId, out SystemHandleType handleType)
        {
            string token = GetHandleTypeToken(handle, processId);
            return GetHandleTypeFromToken(token, out handleType);
        }

        private static bool GetHandleTypeFromToken(string token, out SystemHandleType handleType)
        {
            for (int i = 1; i < handleTypeTokenCount; i++)
            {
                if (handleTypeTokens[i] == token)
                {
                    handleType = (SystemHandleType)i;
                    return true;
                }
            }
            handleType = SystemHandleType.OB_TYPE_UNKNOWN;
            return false;
        }

        private static string GetHandleTypeToken(IntPtr handle, int processId)
        {
            IntPtr currentProcess = NativeMethods.GetCurrentProcess();
            bool remote = (processId != NativeMethods.GetProcessId(currentProcess));
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
                return GetHandleTypeToken(handle);
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

        private static string GetHandleTypeToken(IntPtr handle)
        {
            int length;
            var result = NativeMethods.NtQueryObject(handle, OBJECT_INFORMATION_CLASS.ObjectTypeInformation, IntPtr.Zero, 0, out length);
            if (length < 0)
                return string.Empty;

            IntPtr ptr = IntPtr.Zero;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                try { }
                finally
                {
                    ptr = Marshal.AllocHGlobal(length);
                }
                if (NativeMethods.NtQueryObject(handle, OBJECT_INFORMATION_CLASS.ObjectTypeInformation, ptr, length, out length) == NT_STATUS.STATUS_SUCCESS)
                {
                    return Marshal.PtrToStringUni((IntPtr)((int)ptr + 0x60));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return string.Empty;
        }
        #endregion
    }
}