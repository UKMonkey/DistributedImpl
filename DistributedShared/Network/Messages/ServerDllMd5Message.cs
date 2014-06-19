using DistributedSharedInterfaces.Messages;

using DistributedShared.Stream;
using System.Collections.Generic;

namespace DistributedShared.Network.Messages
{
    public class ServerDllMd5Message : Message
    {
        public List<string> DllNames { get; private set; }
        public List<string> Md5Values { get; private set; }

        public int Count { get { return DllNames.Count; } }

        public ServerDllMd5Message()
        {
            DllNames = new List<string>();
            Md5Values = new List<string>();
        }

        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(DllNames.Count);
            for (var i = 0; i < DllNames.Count; ++i)
            {
                target.Write(DllNames[i]);
                target.Write(Md5Values[i]);
            }
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            var count = source.ReadInt();
            DllNames = new List<string>(count);
            Md5Values = new List<string>(count);

            for (var i = 0; i < count; ++i)
            {
                DllNames.Add(source.ReadString());
                Md5Values.Add(source.ReadString());
            }
        }
    }
}
