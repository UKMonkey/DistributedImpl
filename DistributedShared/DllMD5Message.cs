using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedSharedInterfaces.Messages;

using DistributedShared.Stream;

namespace DistributedShared.Network.Messages
{
    public class DllMD5Message : Message
    {
        public string DllName;
        public string Md5Value;

        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(DllName);
            target.Write(Md5Value);
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            DllName = source.ReadString();
            Md5Value = source.ReadString();
        }
    }
}
