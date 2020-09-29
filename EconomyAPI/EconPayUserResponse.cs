namespace EconomyAPI
{
    using EconomyAPI.Messages;
    using ProtoBuf;

    public enum EconPayUserMessage : byte
    {
        PaymentsNotEnabled = 0,
        InvalidRequest = 1,
        NoSenderAccount = 2,
        NoRepientAccount = 3,
        InsufficientFunds = 4,
        Success = 5
    }

    [ProtoContract]
    public class EconPayUserResponse : EconMessageBase
    {
        [ProtoMember(201)]
        public EconPayUserMessage Message;

        public override void ProcessClient()
        {
            throw new System.NotImplementedException();
        }

        public override void ProcessServer()
        {
            VRage.Utils.MyLog.Default.WriteLine(string.Format("Callback EconPayUserResponse: {0}", Message));
        }
    }
}
