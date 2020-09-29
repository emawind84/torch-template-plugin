using EconomyAPI;
using NLog;
using Sandbox.ModAPI;
using System;
using System.Diagnostics;
using System.IO;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Session;

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

        public override void Update()
        {
            base.Update();

            try
            {
                /* stopWatch not running? Nothing to do */
                if (!stopWatch.IsRunning)
                    return;

                /* Session not loaded? Nothing to do */
                if (Torch.CurrentSession == null || Torch.CurrentSession.State != TorchSessionState.Loaded)
                    return;

                var elapsed = stopWatch.Elapsed;
                if (elapsed.TotalSeconds < 10)
                    return;

                stopWatch.Restart();

                MyAPIGateway.Utilities.InvokeOnGameThread(() => {
                    //EconPayUser.SendMessage(0, 1234, 55.4m, "test20", 55555, 123);
                });

                Log.Debug($"Completed in {stopWatch.ElapsedMilliseconds}ms");
                stopWatch.Restart();
            }
            catch (Exception e)
            {
                Log.Error(e, "Something is not right");
            }
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
                EconCommunication.Register();
                Log.Info("Session loaded, start backup timer!");
            }
            else if (newState == TorchSessionState.Unloading || newState == TorchSessionState.Unloaded)
            {
                stopWatch.Stop();
                EconCommunication.Unregister();
                Log.Info("Session Unloading, suspend backup timer!");
            }
        }
    }
}
