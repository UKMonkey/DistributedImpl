using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedSharedInterfaces.Messages;

using DistributedShared.Stream;

namespace DistributedShared.Network.Messages
{
    public class JobRequestMessage : Message
    {
        public short JobCount { get; set; }


        JobRequestMessage()
        {
            JobCount = 2;
        }


        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(JobCount);
        }


        protected override void Deserialise(IMessageOutputStream source)
        {
            JobCount = source.ReadShort();
        }
    }
}
