using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using Torch.Mod.Messages;
using TorchPlugin.EconomyAPI.Messages;

namespace EconomyAPI.Messages
{
    #region Includes
    [ProtoInclude(1, typeof(EconPayUser))]
    [ProtoInclude(2, typeof(EconPayUserResponse))]
    [ProtoInclude(3, typeof(EconCommandMessage))]
    #endregion

    [ProtoContract]
    public abstract class EconMessageBase
    {
        [ProtoMember(101)]
        public ushort CallbackModChannel;

        [ProtoMember(102)]
        public ulong SenderId;

        public virtual void ProcessClient() { }
        public virtual void ProcessServer() { }

        //members below not serialized, they're just metadata about the intended target(s) of this message
        internal MessageTarget TargetType;
        internal ulong Target;
        internal ulong[] Ignore;
        internal byte[] CompressedData;
    }

}
