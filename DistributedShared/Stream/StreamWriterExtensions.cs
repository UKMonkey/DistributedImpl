using System;
using System.Text;
using DistributedShared.Encryption;
using DistributedSharedInterfaces.Messages;

namespace DistributedShared.Stream
{
    public static class StreamWriterExtensions
    {
        public static void Write(this IMessageInputStream writer, bool value)
        {
            var buffer = new byte[1];
            if (value)
                buffer[0] = 1;
            else
                buffer[0] = 0;

            writer.Write(buffer, 0, 1);
        }

        public static void Write(this IMessageInputStream writer, short value)
        {
            var buffer = BitConverter.GetBytes(value);
            writer.Write(buffer, 0, buffer.Length);
        }

        public static void Write(this IMessageInputStream writer, int value)
        {
            var buffer = BitConverter.GetBytes(value);
            writer.Write(buffer, 0, buffer.Length);
        }

        public static void Write(this IMessageInputStream writer, long value)
        {
            var buffer = BitConverter.GetBytes(value);
            writer.Write(buffer, 0, buffer.Length);
        }

        public static void Write(this IMessageInputStream writer, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            writer.Write(bytes.Length);
            writer.Write(bytes, 0, bytes.Length);
        }

        public static void Write(this IMessageInputStream writer, byte[] value)
        {
            writer.Write(value.Length);
            writer.Write(value, 0, value.Length);
        }

        public static void WriteEncrypted(this IMessageInputStream writer, String value)
        {
            var encrypted = AESEncryption.EncryptString(value, "Invalid dll");
            writer.Write(encrypted);
        }
    }
}
