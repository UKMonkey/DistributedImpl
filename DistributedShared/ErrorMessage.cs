using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using DistributedSharedInterfaces.Messages;
using DistributedShared.Stream;

namespace DistributedShared.Network.Messages
{
    public enum ErrorValues : short
    {
        UnknownMessage
    }


    public class ErrorMessage : Message
    {
        public String Error;
        public ErrorValues ErrorCode;

        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(Error);
            target.Write((short)ErrorCode);
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            Error = source.ReadString();
            ErrorCode = (ErrorValues)source.ReadShort();
        }
    }
}
