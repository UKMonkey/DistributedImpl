using System;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Net;

namespace DistributedShared.SystemMonitor
{
    public class OpenHandle
    {
        public enum HandleType
        {
            PORT,
            FILE,
            DIRECTORY,
            PROCESS,
            THREAD
        }

        public enum ConnectionType
        {
            UDP,
            TCP
        }

        public struct PortInfo
        {
            public bool Listening;
            public int LocalPort;
            public string RemoteHost;
            public int RemoteHostPort;
            public ConnectionType ConnectionType;

            public PortInfo(bool listening, int localPort, int remoteHost, int remoteHostPort, ConnectionType connectionType)
            {
                Listening = listening;
                LocalPort = localPort;
                RemoteHost = new IPAddress(BitConverter.GetBytes(remoteHost)).ToString();
                RemoteHostPort = remoteHostPort;
                ConnectionType = connectionType;
            }

            public PortInfo(bool listening, int localPort, long remoteHost1, long remoteHost2, int remoteHostPort, ConnectionType connectionType)
            {
                Listening = listening;
                LocalPort = localPort;
                RemoteHost = new IPAddress(BitConverter.GetBytes(remoteHost1).
                    Concat(BitConverter.GetBytes(remoteHost2)).ToArray()).ToString();
                RemoteHostPort = remoteHostPort;
                ConnectionType = connectionType;
            }

            public new string ToString()
            {
                return string.Format("{0}:{1}:{2}:{3}:{4}", ConnectionType, Listening, RemoteHost, RemoteHostPort, LocalPort);
            }
        }

        public FileInfo FileInfo { get; private set; }
        public DirectoryInfo DirectoryInfo { get; private set; }
        public Process Process { get; private set; }
        public ProcessThread Thread { get; private set; }
        public PortInfo? Port { get; private set; }

        public HandleType Type { get; private set; }

        // registers a handle that is unknown
        public OpenHandle(HandleType handleType)
        {
            Type = handleType;
        }

        public OpenHandle(FileInfo info)
        {
            FileInfo = info;
            Type = HandleType.FILE;
        }

        public OpenHandle(DirectoryInfo info)
        {
            DirectoryInfo = info;
            Type = HandleType.DIRECTORY;
        }

        public OpenHandle(Process info)
        {
            Process = info;
            Type = HandleType.PROCESS;
        }

        public OpenHandle(ProcessThread info)
        {
            Thread = info;
            Type = HandleType.THREAD;
        }

        public OpenHandle(PortInfo info)
        {
            Port = info;
            Type = HandleType.PORT;
        }
    }
}
