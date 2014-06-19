using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DistributedSharedInterfaces.Messages;
using DistributedShared.Stream;


namespace DistributedShared.Network.Messages
{
    public class CancelWorkMessage : Message
    {
        public string DllName { get; set; }

        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(DllName);
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            DllName = source.ReadString();
        }
    }
}
