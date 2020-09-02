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
                if (elapsed.TotalSeconds < 1)
                    return;

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

                {
                    BoundingSphereD boundingSphereD = new BoundingSphereD(new Vector3D(0, 0, 0), 1000.0);
                    var entities = new List<MyEntity>();
                    MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref boundingSphereD, entities, MyEntityQueryType.Dynamic);
                    var protectedGrid = entities.Find(
                        entity => {
                            bool flag = true;
                            if (entity is MyCubeGrid)
                            {
                                MyCubeGrid mcg = entity as MyCubeGrid;
                                long identityId = OwnershipUtils.GetOwner(entity as MyCubeGrid);
                                //MyPlayer.PlayerId playerId;
                                //MySession.Static.Players.TryGetPlayerId(identityId, out playerId);
                                var steamId = MySession.Static.Players.TryGetSteamId(identityId);
                                flag &= steamId != 0L && MySession.Static.IsUserSpaceMaster(steamId);
                                flag &= mcg.DestructibleBlocks == false;
                            }
                            else
                            {
                                flag = false;
                            }
                            return flag;
                        });
                    
                    Parallel.ForEach(entities, entity =>
                    {
                        Log.Debug(entity.GetType());
                        if (entity is MyCubeGrid)
                        {
                            var myCubeGrid = entity as MyCubeGrid;
                            myCubeGrid.DestructibleBlocks = !(protectedGrid != null);
                        }
                    });
                }

                var players = MySession.Static.Players.GetOnlinePlayers();

                {
                    var entities = new HashSet<IMyEntity>();
                    MyAPIGateway.Entities.GetEntities(entities, entity => entity.DisplayName != null && entity.DisplayName.IndexOf("TRACK") != -1);
                    foreach (var entity in entities)
                    {
                        MyGps gps = new MyGps
                        {
                            Coords = entity.GetPosition(),
                            Name = string.Format("@{0}: {1}", DateTime.UtcNow, entity.DisplayName),
                            //AlwaysVisible = true,
                            ShowOnHud = true,
                            GPSColor = Color.Green,
                            DiscardAt = TimeSpan.FromMinutes(30),
                            IsContainerGPS = false,
                        };

                        foreach (var player in players)
                        {
                            Torch.CurrentSession.KeenSession.Gpss.SendAddGps(player.Identity.IdentityId, ref gps, 0L, false);
                        }

                    }
                }

                //MyAPIGateway.Players.GetPlayers(players);

                IMyWeatherEffects effects = Torch.CurrentSession.KeenSession.WeatherEffects;
                foreach (var player in players)
                {
                    //Log.Info(player.DisplayName);
                    //MySession.Static.Players.TryGetSteamId()
                    //Torch.CurrentSession.Managers.GetManager<ChatManagerServer>()?.SendMessageAsOther("Bubba", "AAAAAA", Color.Red, player.Client.SteamUserId);
                    //Plugin.Instance.Torch.CurrentSession.Managers.GetManager<ChatManagerServer>()?.SendMessageAsOther(AuthorName, StringMsg, Color, ulong);

                }

                MyAPIGateway.Utilities.InvokeOnGameThread(() => {
                    //DO STUFF
                    

                });

                stopWatch.Restart();

                // do stuff here

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
