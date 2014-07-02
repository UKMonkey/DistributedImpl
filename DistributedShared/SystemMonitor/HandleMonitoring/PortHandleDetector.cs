using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;

//http://msdn.microsoft.com/en-us/library/windows/desktop/aa365928(v=vs.85).aspx
//http://msdn.microsoft.com/en-us/library/windows/desktop/aa366386(v=vs.85).aspx
//http://msdn.microsoft.com/en-us/library/windows/desktop/aa366921(v=vs.85).aspx
//http://msdn.microsoft.com/en-us/library/windows/desktop/aa366913(v=vs.85).aspx


namespace DistributedShared.SystemMonitor.HandleMonitoring
{
    public class PortHandleDetector
    {
        public IEnumerable<OpenHandle> GetPortHandles(int processId = 0)
        {
            if (processId == 0)
                processId = Process.GetCurrentProcess().Id;

            return GetTCPv4PortListeners(processId).Concat
                (GetTCPv6PortListeners(processId)).Concat
                (GetUDPv4PortListeners(processId)).Concat
                (GetUDPv6PortListeners(processId));
        }


        private static IEnumerable<OpenHandle> GetTCPv4PortListeners(int processId)
        {
            var ptr = IntPtr.Zero;
            var modifiedPtr = IntPtr.Zero;
            int size = 1024;
            try
            {
                modifiedPtr = ptr = Marshal.AllocHGlobal(size);
                var ret = NativeMethods.GetExtendedTcpTable(ptr, ref size, false, NativeMethods.AF_TYPE.IPv4, NativeMethods.TCP_TYPE.TCP_TABLE_OWNER_PID_ALL);
                while (ret == NativeMethods.NT_ERROR.ERROR_INSUFFICIENT_BUFFER)
                {
                    Marshal.FreeHGlobal(ptr);
                    modifiedPtr = ptr = Marshal.AllocHGlobal(size);
                    ret = NativeMethods.GetExtendedTcpTable(ptr, ref size, false, NativeMethods.AF_TYPE.IPv4, NativeMethods.TCP_TYPE.TCP_TABLE_OWNER_PID_ALL);
                }

                if (ret != NativeMethods.NT_ERROR.ERROR_SUCCESS)
                    yield break;

                int count = Marshal.ReadInt32(ptr);
                modifiedPtr = modifiedPtr + Marshal.SizeOf(count);

                for (var i = 0; i < count; ++i)
                {
                    var entry = (NativeMethods.TCPv4Table_Owner_Pid_Entry)Marshal.PtrToStructure(modifiedPtr, typeof(NativeMethods.TCPv4Table_Owner_Pid_Entry));
                    modifiedPtr = modifiedPtr + Marshal.SizeOf(entry);

                    if (entry.OwningPid != processId)
                        continue;

                    if (entry.State == NativeMethods.TCP_STATE.CLOSED)
                        continue;

                    yield return new OpenHandle(new OpenHandle.PortInfo(entry.State == NativeMethods.TCP_STATE.LISTEN,
                        entry.LocalPort,
                        entry.RemoteAddr,
                        entry.RemotePort, OpenHandle.ConnectionType.TCP));
                }
            }
            finally
            {
            	if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }


        private IEnumerable<OpenHandle> GetTCPv6PortListeners(int processId)
        {
            IntPtr ptr = IntPtr.Zero;
            IntPtr modifiedPtr = IntPtr.Zero;
            int size = 1024;
            try
            {
                modifiedPtr = ptr = Marshal.AllocHGlobal(size);
                var ret = NativeMethods.GetExtendedTcpTable(ptr, ref size, false, NativeMethods.AF_TYPE.IPv6, NativeMethods.TCP_TYPE.TCP_TABLE_OWNER_PID_ALL);
                while (ret == NativeMethods.NT_ERROR.ERROR_INSUFFICIENT_BUFFER)
                {
                    Marshal.FreeHGlobal(ptr);
                    modifiedPtr = ptr = Marshal.AllocHGlobal(size);
                    ret = NativeMethods.GetExtendedTcpTable(ptr, ref size, false, NativeMethods.AF_TYPE.IPv6, NativeMethods.TCP_TYPE.TCP_TABLE_OWNER_PID_ALL);
                }

                if (ret != NativeMethods.NT_ERROR.ERROR_SUCCESS)
                    yield break;

                int count = Marshal.ReadInt32(ptr);
                modifiedPtr = modifiedPtr + Marshal.SizeOf(count);

                for (var i = 0; i < count; ++i)
                {
                    var entry = (NativeMethods.TCPv6Table_Owner_Pid_Entry)Marshal.PtrToStructure(modifiedPtr, typeof(NativeMethods.TCPv6Table_Owner_Pid_Entry));
                    modifiedPtr = modifiedPtr + Marshal.SizeOf(entry);

                    if (entry.OwningPid != processId)
                        continue;

                    if (entry.State == NativeMethods.TCP_STATE.CLOSED)
                        continue;

                    yield return new OpenHandle(new OpenHandle.PortInfo(entry.State == NativeMethods.TCP_STATE.LISTEN,
                        entry.LocalPort,
                        entry.RemoteAddr1,
                        entry.RemoteAddr2,
                        entry.RemotePort, OpenHandle.ConnectionType.TCP));
                }
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }


        private IEnumerable<OpenHandle> GetUDPv4PortListeners(int processId)
        {
            IntPtr ptr = IntPtr.Zero;
            IntPtr modifiedPtr = IntPtr.Zero;
            int size = 1024;
            try
            {
                modifiedPtr = ptr = Marshal.AllocHGlobal(size);
                var ret = NativeMethods.GetExtendedUdpTable(ptr, ref size, false, NativeMethods.AF_TYPE.IPv4, NativeMethods.UDP_TYPE.UDP_TABLE_OWNER_PID);
                while (ret == NativeMethods.NT_ERROR.ERROR_INSUFFICIENT_BUFFER)
                {
                    Marshal.FreeHGlobal(ptr);
                    modifiedPtr = ptr = Marshal.AllocHGlobal(size);
                    ret = NativeMethods.GetExtendedUdpTable(ptr, ref size, false, NativeMethods.AF_TYPE.IPv4, NativeMethods.UDP_TYPE.UDP_TABLE_OWNER_PID);
                }

                int count = Marshal.ReadInt32(ptr);
                modifiedPtr = modifiedPtr + Marshal.SizeOf(count);

                for (var i = 0; i < count; ++i)
                {
                    var entry = (NativeMethods.UDPv4Table_Owner_Pid_Entry)Marshal.PtrToStructure(modifiedPtr, typeof(NativeMethods.UDPv4Table_Owner_Pid_Entry));
                    modifiedPtr = modifiedPtr + Marshal.SizeOf(entry);

                    if (entry.OwningPid != processId)
                        continue;

                    yield return new OpenHandle(new OpenHandle.PortInfo(true,
                        entry.LocalPort,
                        0,
                        0, OpenHandle.ConnectionType.UDP));
                }
                yield break;
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }


        private static IEnumerable<OpenHandle> GetUDPv6PortListeners(int processId)
        {
            IntPtr ptr = IntPtr.Zero;
            IntPtr modifiedPtr = IntPtr.Zero;
            int size = 1024;
            try
            {
                modifiedPtr = ptr = Marshal.AllocHGlobal(size);
                var ret = NativeMethods.GetExtendedUdpTable(ptr, ref size, false, NativeMethods.AF_TYPE.IPv6, NativeMethods.UDP_TYPE.UDP_TABLE_OWNER_PID);
                while (ret == NativeMethods.NT_ERROR.ERROR_INSUFFICIENT_BUFFER)
                {
                    Marshal.FreeHGlobal(ptr);
                    modifiedPtr = ptr = Marshal.AllocHGlobal(size);
                    ret = NativeMethods.GetExtendedUdpTable(ptr, ref size, false, NativeMethods.AF_TYPE.IPv6, NativeMethods.UDP_TYPE.UDP_TABLE_OWNER_PID);
                }

                int count = Marshal.ReadInt32(ptr);
                modifiedPtr = modifiedPtr + Marshal.SizeOf(count);

                for (var i = 0; i < count; ++i)
                {
                    var entry = (NativeMethods.UDPv6Table_Owner_Pid_Entry)Marshal.PtrToStructure(modifiedPtr, typeof(NativeMethods.UDPv6Table_Owner_Pid_Entry));
                    modifiedPtr = modifiedPtr + Marshal.SizeOf(entry);

                    if (entry.OwningPid != processId)
                        continue;

                    yield return new OpenHandle(new OpenHandle.PortInfo(true,
                        entry.LocalPort,
                        0,
                        0, OpenHandle.ConnectionType.UDP));
                }
                yield break;
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
