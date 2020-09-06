using ALE_Core.Utils;
using NLog;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Commands;
using Torch.Managers.ChatManager;
using Torch.Session;
using VRage.Game.Entity;
using VRageMath;

namespace TorchPlugin
{
    public class MyPlugin : TorchPluginBase
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private Persistent<TestConfig> _config;
        public TestConfig Config => _config?.Data;

        private readonly Stopwatch stopWatch = new Stopwatch();

        /// <inheritdoc />
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            Log.Info("This is a Test if it works!");

            SetupConfig();

            var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");
        }
        
        private void SetupConfig()
        {

            var configFile = Path.Combine(StoragePath, "TestConfig.cfg");

            try
            {

                _config = Persistent<TestConfig>.Load(configFile);

            }
            catch (Exception e)
            {
                Log.Warn(e);
            }

            if (_config?.Data == null)
            {

                Log.Info("Create Default Config, because none was found!");

                _config = new Persistent<TestConfig>(configFile, new TestConfig());
                _config.Save();
            }
        }

        private void SessionChanged(ITorchSession session, TorchSessionState newState)
        {

            if (newState == TorchSessionState.Loaded)
            {
                stopWatch.Start();
                Log.Info("Session loaded, start backup timer!");
            }
            else if (newState == TorchSessionState.Unloading)
            {
                stopWatch.Stop();
                Log.Info("Session Unloading, suspend backup timer!");
            }
        }
    }
}
