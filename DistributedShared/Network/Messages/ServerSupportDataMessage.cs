using DistributedShared.Stream;
using DistributedSharedInterfaces.Messages;
using System.Collections.Generic;
using System;

namespace DistributedShared.Network.Messages
{
    public class ServerSupportDataMessage: Message
    {
        public string DllName { get; set; }
        public long Version { get; set; }
        public Dictionary<String, byte[]> Data { get; set; }


        public ServerSupportDataMessage()
        {
            Data = new Dictionary<string, byte[]>();
        }


        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(DllName);
            target.Write(Version);
            target.Write(Data);
        }


        protected override void Deserialise(IMessageOutputStream source)
        {
            DllName = source.ReadString();
            Version = source.ReadLong();
            Data = source.ReadDataDict();
        }
    }
}
