using EconomyAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using TorchPlugin.EconomyAPI.Messages;
using VRage.Game.ModAPI;

namespace TorchPlugin
{
    [Category("econ")]
    public class TestCommand : CommandModule
    {

        [Command("pricelist", "This is a Test Command.")]
        [Permission(MyPromoteLevel.None)]
        public void Pricelist()
        {
            if (Context.Player != null)
            {
                EconCommunication.SendMessageTo(new EconCommandMessage { Command = "/pricelist " + Context.RawArgs }, Context.Player.SteamUserId);
            }
            else
            {
                EconCommunication.SendMessageToServer(new EconCommandMessage { Command = "/pricelist " + Context.RawArgs });
            }
        }

        [Command("set", "This is a Test Command.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void SetCommand()
        {
            if (Context.Player != null)
            {
                EconCommunication.SendMessageTo(new EconCommandMessage { Command = "/set " + Context.RawArgs }, Context.Player.SteamUserId);
            }
            else
            {
                EconCommunication.SendMessageToServer(new EconCommandMessage { Command = "/set " + Context.RawArgs });
            }
        }

    }
}
