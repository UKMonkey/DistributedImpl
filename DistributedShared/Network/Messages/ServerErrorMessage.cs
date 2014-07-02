using System;
using DistributedSharedInterfaces.Messages;
using DistributedShared.Stream;

namespace DistributedShared.Network.Messages
{
    public enum ErrorValues : short
    {
        UnknownMessage
    }


    public class ServerErrorMessage : Message
    {
        public String Error;
        public ErrorValues ErrorCode;

        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(Error);
            target.Write((short)ErrorCode);

            Console.WriteLine(String.Format("Error ({0}): {1}", ErrorCode, Error));
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            Error = source.ReadString();
            ErrorCode = (ErrorValues)source.ReadShort();
        }
    }
}
