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
using VRage.ModAPI;
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
                if (elapsed.TotalSeconds < 30)
                    return;

                stopWatch.Restart();
                
                {
                    var entities = new HashSet<IMyEntity>();
                    MyAPIGateway.Entities.GetEntities(entities, entity => entity.DisplayName != null 
                    && entity.DisplayName.IndexOf("[TRACK]", StringComparison.InvariantCultureIgnoreCase) != -1);
                    foreach (MyCubeGrid entity in entities)
                    {
                        MyGps gps = new MyGps
                        {
                            Coords = entity.WorldMatrix.Translation,
                            Name = string.Format("{0}: {1}", entity.DisplayName, DateTime.UtcNow),
                            ShowOnHud = true,
                            GPSColor = Color.Green,
                            DiscardAt = new TimeSpan?(TimeSpan.FromSeconds(MySession.Static.ElapsedPlayTime.TotalSeconds + 600))
                        };
                        gps.DiscardAt = new TimeSpan?(TimeSpan.FromSeconds(MySession.Static.ElapsedPlayTime.TotalSeconds + 600));

                        Torch.CurrentSession.KeenSession.Gpss.SendAddGps(GetOwner(entity), ref gps, 0L, false);
                    }
                }

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
                Log.Info("Session loaded, start backup timer!");
            }
            else if (newState == TorchSessionState.Unloading)
            {
                stopWatch.Stop();
                Log.Info("Session Unloading, suspend backup timer!");
            }
        }

        public static long GetOwner(MyCubeGrid grid)
        {

            var gridOwnerList = grid.BigOwners;
            var ownerCnt = gridOwnerList.Count;
            var gridOwner = 0L;

            if (ownerCnt > 0 && gridOwnerList[0] != 0)
                return gridOwnerList[0];
            else if (ownerCnt > 1)
                return gridOwnerList[1];

            return gridOwner;
        }
    }
}
