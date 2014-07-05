using System;
using System.Text;
using DistributedShared.Encryption;
using DistributedSharedInterfaces.Messages;
using DistributedShared.Jobs;
using System.Collections.Generic;

namespace DistributedShared.Stream
{
    public static class StreamReaderExtensions
    {
        public static bool ReadBoolean(this IMessageOutputStream reader)
        {
            var buffer = new byte[1];
            reader.Read(buffer, 1);
            return buffer[0] != 0;
        }

        public static short ReadShort(this IMessageOutputStream reader)
        {
            var buffer = new byte[2];
            reader.Read(buffer, 2);
            return BitConverter.ToInt16(buffer, 0);
        }

        public static int ReadInt(this IMessageOutputStream reader)
        {
            var buffer = new byte[4];
            reader.Read(buffer, 4);
            return BitConverter.ToInt32(buffer, 0);
        }

        public static long ReadLong(this IMessageOutputStream reader)
        {
            var buffer = new byte[8];
            reader.Read(buffer, 8);
            return BitConverter.ToInt64(buffer, 0);
        }

        public static string ReadString(this IMessageOutputStream reader)
        {
            var length = reader.ReadInt();
            var buffer = new byte[length];
            reader.Read(buffer, length);

            return Encoding.UTF8.GetString(buffer);
        }

        public static byte[] ReadByteArray(this IMessageOutputStream reader)
        {
            var length = reader.ReadInt();
            var buffer = new byte[length];

            if (length == 0)
                return buffer;

            reader.Read(buffer, length);
            return buffer;
        }

        public static string ReadEncrypted(this IMessageOutputStream reader)
        {
            var data = reader.ReadString();
            return AESEncryption.DecryptString(data, "Invalid dll");
        }





        public static WrappedJobGroup ReadJobGroup(this IMessageOutputStream reader)
        {
            var ret = new WrappedJobGroup();

            ret.JobCount = reader.ReadInt();
            ret.Data = reader.ReadByteArray();
            ret.GroupId = reader.ReadLong();
            ret.SupportingDataVersion = reader.ReadLong();
            ret.DllName = reader.ReadString();

            return ret;
        }


        public static WrappedJobData ReadJobData(this IMessageOutputStream reader)
        {
            var ret = new WrappedJobData();

            ret.Data = reader.ReadByteArray();
            ret.DllName = reader.ReadString();
            ret.JobId = reader.ReadLong();
            ret.SupportingDataVersion = reader.ReadLong();

            return ret;
        }


        public static WrappedResultData ReadResultData(this IMessageOutputStream reader)
        {
            var ret = new WrappedResultData();

            ret.CyclesSpentWorking = reader.ReadLong();
            ret.CyclesSpentWorkingIsReliable = reader.ReadBoolean();
            ret.Data = reader.ReadByteArray();
            ret.DllName = reader.ReadString();
            ret.JobId = reader.ReadLong();

            return ret;
        }


        public static Dictionary<String, byte[]> ReadDataDict(this IMessageOutputStream source)
        {
            var count = source.ReadInt();
            var ret = new Dictionary<String, byte[]>();
            for (var i = 0; i < count; ++i)
            {
                var key = source.ReadString();
                var value = source.ReadByteArray();
                ret.Add(key, value);
            }
            return ret;
        }
    }
}
