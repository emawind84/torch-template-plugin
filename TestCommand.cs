using EconomyAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace TorchPlugin
{

    public class TestCommand : CommandModule
    {

        [Command("test", "This is a Test Command.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Test()
        {
            if (Context.Player != null)
            {
                // we are in game
                EconCommunication.SendMessageToServer(new EconPayUser {
                    FromPlayerIdentity = Context.Player.SteamUserId,
                    ToPlayerIdentity = 1234,
                    Reason = "test20",
                    TransactionAmount = 55.4m,
                    TransactionId = 0
                });
                //EconPayUser.SendMessage(Context.Player.SteamUserId, 1234, 55.4m, "test20", 21384, 123);
            }
            else
            {
                // we are using console
            }
            Context.Respond("This is a Test from " + Context.Player);
        }

        [Command("testWithCommands", "This is a Test Command.")]
        [Permission(MyPromoteLevel.None)]
        public void TestWithArgs(string foo, string bar = null)
        {
            Context.Respond("This is a Test " + foo + ", " + bar);
        }

    }
}
