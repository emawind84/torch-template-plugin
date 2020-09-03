using ALE_Core.Utils;
using NLog;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
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
using Torch.Session;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
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
                if (elapsed.TotalSeconds < 2)
                    return;

                stopWatch.Restart();
                //var entities = new HashSet<IMyEntity>();
                //MyAPIGateway.Entities.GetEntities(entities);
                //foreach (var entity in entities)
                //{
                //    Log.Info("Found entity: " + entity.GetFriendlyName());
                //}

                ////MyVisualScriptLogicProvider.AddToPlayersInventory();

                //Log.Info("do stuff here");
                //foreach (var mod in MySession.Static.Mods)
                //{
                //    Log.Info("Mod loaded: " + mod.FriendlyName);
                //}

                //List<MyCubeGrid> grids = MyEntities.GetEntities().OfType<MyCubeGrid>().ToList();

                //MyPlayer.PlayerId playerId;
                //MySession.Static.Players.TryGetPlayerId(identityId, out playerId);

                {
                    var players = MySession.Static.Players.GetOnlinePlayers();
                    Parallel.ForEach(players, player => {
                        BoundingSphereD boundingSphereD = new BoundingSphereD(player.GetPosition(), 200.0);
                        var entities = new List<MyEntity>();
                        MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref boundingSphereD, entities, MyEntityQueryType.Both);
                        IEnumerable<MyCubeGrid> grids = entities.OfType<MyCubeGrid>();

                        MyCubeGrid masterGrid = null;
                        foreach (var grid in grids)
                        {
                            if (masterGrid != null) break;
                            bool flag = true;
                            //long ownerIdentityId = OwnershipUtils.GetOwner(grid);
                            //var ownerSteamId = MySession.Static.Players.TryGetSteamId(ownerIdentityId);
                            //flag &= ownerSteamId != 0L && MySession.Static.IsUserSpaceMaster(ownerSteamId);
                            flag &= grid.DestructibleBlocks == false;
                            var beacons = grid.GetBlocks().Where(blk => blk.FatBlock is IMyBeacon);

                            IMyBeacon beacon = null;
                            foreach (var blk in beacons)
                            {
                                if (blk.FatBlock?.DisplayName?.IndexOf("SAFEZONE") == -1) continue;
                                var ownerSteamId = MySession.Static.Players.TryGetSteamId(beacon.OwnerId);
                                if (ownerSteamId == 0L || !MySession.Static.IsUserSpaceMaster(ownerSteamId)) continue;

                                beacon = blk.FatBlock as IMyBeacon;
                                break;
                            }
                            if (beacon == null) flag = false;
                            if (flag) masterGrid = grid;
                        }

                        Parallel.ForEach(grids, grid =>
                        {
                            var cg = grid as MyCubeGrid;
                            if (grid != masterGrid && cg.BigOwners.Contains(player.Identity.IdentityId))
                            {
                                cg.DestructibleBlocks = !(masterGrid != null);
                            }
                        });

                    });
                }

                //{
                //    var players = MySession.Static.Players.GetOnlinePlayers();
                //    var entities = new HashSet<IMyEntity>();

                //    MyAPIGateway.Entities.GetEntities(entities, entity => entity.DisplayName != null && entity.DisplayName.IndexOf("TRACK") != -1);
                //    foreach (var entity in entities)
                //    {
                //        MyGps gps = new MyGps
                //        {
                //            Coords = entity.GetPosition(),
                //            Name = string.Format("@{0}: {1}", DateTime.UtcNow, entity.DisplayName),
                //            //AlwaysVisible = true,
                //            ShowOnHud = true,
                //            GPSColor = Color.Green,
                //            DiscardAt = TimeSpan.FromMinutes(30),
                //            IsContainerGPS = false,
                //        };

                //        foreach (var player in players)
                //        {
                //            Torch.CurrentSession.KeenSession.Gpss.SendAddGps(player.Identity.IdentityId, ref gps, 0L, false);
                //        }

                //    }
                //}

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
    }
}
