using EconomyAPI.Messages;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorchPlugin.EconomyAPI.Messages
{
    [ProtoContract]
    class EconCommandMessage : EconMessageBase
    {
        [ProtoMember(201)]
        public string Command;
        
    }
}
