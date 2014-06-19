﻿using DistributedSharedInterfaces.Messages;
using DistributedShared.Stream;

namespace DistributedShared.Network.Messages
{
    public class ServerDllContentMessage : Message
    {
        public string DllName { get; set; }
        public byte[] Content { get; set; }

        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(DllName);
            target.Write(Content);
        }


        protected override void Deserialise(IMessageOutputStream source)
        {
            DllName = source.ReadString();
            Content = source.ReadByteArray();
        }
    }
}
