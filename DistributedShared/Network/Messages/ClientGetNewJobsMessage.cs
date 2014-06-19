using DistributedShared.Stream;
using DistributedSharedInterfaces.Messages;


namespace DistributedShared.Network.Messages
{
    public class ClientGetNewJobsMessage : Message
    {
        public short NumberOfJobs { get; set; }

        protected override void Serialise(IMessageInputStream target)
        {
            target.Write(NumberOfJobs);
        }

        protected override void Deserialise(IMessageOutputStream source)
        {
            NumberOfJobs = source.ReadShort();
        }
    }
}
