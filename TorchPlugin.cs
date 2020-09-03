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
                        BoundingSphereD boundingSphereD = new BoundingSphereD(player.GetPosition(), 20.0);
                        var entities = new List<MyEntity>();
                        MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref boundingSphereD, entities, MyEntityQueryType.Both);
                        IEnumerable<MyCubeGrid> grids = entities.OfType<MyCubeGrid>();

                        MyCubeGrid masterGrid = null;
                        foreach (var grid in grids)
                        {
                            if (masterGrid != null) break;
                            var safeZoneBlocks = grid.GetBlocks().Where(blk => blk.FatBlock is IMySafeZoneBlock);
                            foreach (var _slimBlock in safeZoneBlocks)
                            {
                                var _safeZone = _slimBlock.FatBlock as IMySafeZoneBlock;
                                var ownerSteamId = MySession.Static.Players.TryGetSteamId(_safeZone.OwnerId);
                                if (_safeZone.IsWorking 
                                && _safeZone.CustomData.IndexOf("[SAFEZONE]") != -1
                                && ownerSteamId != 0L && MySession.Static.IsUserSpaceMaster(ownerSteamId))
                                {
                                    masterGrid = grid;
                                    break;
                                }
                            }
                        }

                        if (masterGrid.DestructibleBlocks)
                        {
                            masterGrid.DestructibleBlocks = false;
                        }

                        Parallel.ForEach(grids, grid =>
                        {
                            if (grid != masterGrid && grid.BigOwners.Contains(player.Identity.IdentityId))
                            {
                                if (masterGrid != null && grid.DestructibleBlocks)
                                {
                                    Log.Debug("safezone in");
                                    Torch.CurrentSession.Managers.GetManager<ChatManagerServer>()?.SendMessageAsOther(masterGrid.DisplayName ?? "Server", "You entered a safe zone", Color.Green, player.Client.SteamUserId);
                                    grid.DestructibleBlocks = false;
                                }
                                else if (masterGrid == null && grid.DestructibleBlocks == false)
                                {
                                    Log.Debug("safezone out");
                                    Torch.CurrentSession.Managers.GetManager<ChatManagerServer>()?.SendMessageAsOther(masterGrid.DisplayName ?? "Server", "You left the safe zone", Color.Red, player.Client.SteamUserId);
                                    grid.DestructibleBlocks = true;
                                }
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
