using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace DistributedShared.SystemMonitor.HandleMonitoring
{
    public static class NativeMethods
    {
        public enum NT_STATUS
        {
            STATUS_SUCCESS = 0x00000000,
            STATUS_BUFFER_OVERFLOW = unchecked((int)0x80000005L),
            STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004L),
        }

        public enum NT_ERROR
        {
            ERROR_SUCCESS             = 0x00000000,
            ERROR_INSUFFICIENT_BUFFER = 0x0000007A
        }

        public enum SYSTEM_INFORMATION_CLASS
        {
            SystemBasicInformation = 0,
            SystemPerformanceInformation = 2,
            SystemTimeOfDayInformation = 3,
            SystemProcessInformation = 5,
            SystemProcessorPerformanceInformation = 8,
            SystemHandleInformation = 16,
            SystemInterruptInformation = 23,
            SystemExceptionInformation = 33,
            SystemRegistryQuotaInformation = 37,
            SystemLookasideInformation = 45
        }

        public enum OBJECT_INFORMATION_CLASS
        {
            ObjectBasicInformation = 0,
            ObjectNameInformation = 1,
            ObjectTypeInformation = 2,
            ObjectAllTypesInformation = 3,
            ObjectHandleInformation = 4
        }

        public enum TCP_STATE
        {
            CLOSED = 1,
            LISTEN,
            SYN_SENT,
            SYN_RECEIVED,
            ESTABLISHED,
            WAIT1,
            WAIT2,
            CLOSE_WAIT,
            CLOSING,
            LAST_ACK,
            TIME_WAIT,
            DELETE,
        }

        public enum AF_TYPE
        {
            IPv4 = 2,
            IPv6 = 23
        }

        public enum TCP_TYPE
        {
            TCP_TABLE_BASIC_LISTENER,
            TCP_TABLE_BASIC_CONNECTIONS,
            TCP_TABLE_BASIC_ALL,
            TCP_TABLE_OWNER_PID_LISTENER,
            TCP_TABLE_OWNER_PID_CONNECTIONS,
            TCP_TABLE_OWNER_PID_ALL,
            TCP_TABLE_OWNER_MODULE_LISTENER,
            TCP_TABLE_OWNER_MODULE_CONNECTIONS,
            TCP_TABLE_OWNER_MODULE_ALL
        }

        public enum UDP_TYPE
        {
            UDP_TABLE_BASIC,
            UDP_TABLE_OWNER_PID,
            UDP_TABLE_OWNER_MODULE
        }

        [Flags]
        public enum ProcessAccessRights
        {
            PROCESS_DUP_HANDLE        = 0x00000040,
            PROCESS_QUERY_INFORMATION = 0x00000400
        }

        [Flags]
        public enum DuplicateHandleOptions
        {
            DUPLICATE_CLOSE_SOURCE = 0x1,
            DUPLICATE_SAME_ACCESS = 0x2
        }


        public struct TCPv4Table_Owner_Pid_Entry
        {
            public TCP_STATE State;
            public int LocalAddr;
            public int LocalPort;
            public int RemoteAddr;
            public int RemotePort;
            public int OwningPid;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct TCPv6Table_Owner_Pid_Entry
        {
            public long LocalAddr1;
            public long LocalAddr2;
            public int LocalScopeId;
            public int LocalPort;
            public long RemoteAddr1;
            public long RemoteAddr2;
            public int RemoteScopeId;
            public int RemotePort;
            public TCP_STATE State;
            public int OwningPid;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct UDPv4Table_Owner_Pid_Entry
        {
            public int LocalAddress;
            public int LocalPort;
            public int OwningPid;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct UDPv6Table_Owner_Pid_Entry
        {
            public long LocalAddress1;
            public long LocalAddress2;
            public int LocalPort;
            public int OwningPid;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_HANDLE_ENTRY
        {
            public int OwnerPid;
            public byte ObjectType;
            public byte HandleFlags;
            public short HandleValue;
            public int ObjectPointer;
            public int AccessMask;
        }


        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        public sealed class SafeObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeObjectHandle()
                : base(true)
            { }

            internal SafeObjectHandle(IntPtr preexistingHandle, bool ownsHandle)
                : base(ownsHandle)
            {
                base.SetHandle(preexistingHandle);
            }

            protected override bool ReleaseHandle()
            {
                return NativeMethods.CloseHandle(base.handle);
            }
        }


        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        public sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeProcessHandle()
                : base(true)
            { }

            internal SafeProcessHandle(IntPtr preexistingHandle, bool ownsHandle)
                : base(ownsHandle)
            {
                base.SetHandle(preexistingHandle);
            }

            protected override bool ReleaseHandle()
            {
                return NativeMethods.CloseHandle(base.handle);
            }
        }


        [DllImport("ntdll.dll")]
        public static extern NT_STATUS NtQuerySystemInformation(
            [In] SYSTEM_INFORMATION_CLASS SystemInformationClass,
            [In] IntPtr SystemInformation,
            [In] int SystemInformationLength,
            [Out] out int ReturnLength);

        [DllImport("ntdll.dll")]
        public static extern NT_STATUS NtQueryObject(
            [In] IntPtr Handle,
            [In] OBJECT_INFORMATION_CLASS ObjectInformationClass,
            [In] IntPtr ObjectInformation,
            [In] int ObjectInformationLength,
            [Out] out int ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern SafeProcessHandle OpenProcess(
            [In] ProcessAccessRights dwDesiredAccess,
            [In, MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            [In] int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DuplicateHandle(
            [In] IntPtr hSourceProcessHandle,
            [In] IntPtr hSourceHandle,
            [In] IntPtr hTargetProcessHandle,
            [Out] out SafeObjectHandle lpTargetHandle,
            [In] int dwDesiredAccess,
            [In, MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            [In] DuplicateHandleOptions dwOptions);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        public static extern int GetLastError();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int GetProcessId(
            [In] IntPtr Process);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int GetThreadId(
            [In] IntPtr Thread);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(
            [In] IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int QueryDosDevice(
            [In] string lpDeviceName,
            [Out] StringBuilder lpTargetPath,
            [In] int ucchMax);

        [DllImport("Iphlpapi.dll")]
        public static extern NT_ERROR GetExtendedTcpTable(
            [Out] IntPtr hTcpTable,
            [In, Out] ref int size,
            [In, MarshalAs(UnmanagedType.Bool)] bool sort,
            [In] AF_TYPE IpType,
            [In] TCP_TYPE tableData,
            [In] int Reserved = 0);

        [DllImport("Iphlpapi.dll")]
        public static extern NT_ERROR GetExtendedUdpTable(
            [Out] IntPtr hUdpTable,
            [In, Out] ref int size,
            [In, MarshalAs(UnmanagedType.Bool)] bool sort,
            [In] AF_TYPE IpType,
            [In] UDP_TYPE tableData,
            [In] int Reserved = 0);
    }
}
