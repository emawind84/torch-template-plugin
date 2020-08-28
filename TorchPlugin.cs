using NLog;
using Sandbox.Game;
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
using VRage.Game.ModAPI;
using VRage.ModAPI;

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

                MyAPIGateway.Utilities.InvokeOnGameThread(() => {
                    //DO STUFF

                    //MyAPIGateway.Players.GetPlayers(players);

                    IMyWeatherEffects effects = Torch.CurrentSession.KeenSession.WeatherEffects;
                    var players = MySession.Static.Players.GetOnlinePlayers();
                    foreach (var player in players)
                    {
                        //Log.Info(player.DisplayName);
                        //MySession.Static.Players.TryGetSteamId()
                        //Torch.CurrentSession.Managers.GetManager<ChatManagerServer>()?.SendMessageAsOther("Bubba", "AAAAAA", Color.Red, player.Client.SteamUserId);
                        //Plugin.Instance.Torch.CurrentSession.Managers.GetManager<ChatManagerServer>()?.SendMessageAsOther(AuthorName, StringMsg, Color, ulong);

                        string weather = effects.GetWeather(player.GetPosition());
                        float intensity = effects.GetWeatherIntensity(player.GetPosition());
                        Log.Debug($"Weather {weather} intensity near {player.DisplayName} is {intensity}");

                    }

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
