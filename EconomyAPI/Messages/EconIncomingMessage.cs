using System;
using System.Collections.Generic;
using System.Text;

namespace EconomyAPI.Messages
{
    /// <summary>
    /// shim to store incoming message data
    /// </summary>
    internal class EconIncomingMessage : EconMessageBase
    {
        public EconIncomingMessage()
        {
        }

        public override void ProcessClient()
        {
            throw new Exception();
        }

        public override void ProcessServer()
        {
            throw new Exception();
        }
    }
}
