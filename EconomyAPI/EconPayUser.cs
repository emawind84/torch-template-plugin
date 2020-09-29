namespace EconomyAPI
{
    using EconomyAPI.Messages;
    using ProtoBuf;

    [ProtoContract]
    public class EconPayUser : EconMessageBase
    {
        [ProtoMember(201)]
        public ulong FromPlayerIdentity;

        [ProtoMember(202)]
        public ulong ToPlayerIdentity;

        [ProtoMember(203)]
        public decimal TransactionAmount;

        [ProtoMember(204)]
        public string Reason;

        [ProtoMember(205)]
        public long TransactionId;
        
    }
}
