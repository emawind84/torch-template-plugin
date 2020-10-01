﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EconomyAPI.Messages;
using Sandbox.ModAPI;
using Torch.Mod.Messages;
using VRage;
using VRage.Collections;
using VRage.Game.ModAPI;
using VRage.Network;
using VRage.Utils;
using Task = ParallelTasks.Task;

namespace EconomyAPI
{
    public static class EconCommunication
    {
        public const ushort NET_ID = 21384;
        private static bool _closing = false;
        private static BlockingCollection<EconMessageBase> _processing;
        private static MyConcurrentPool<EconIncomingMessage> _messagePool;
        private static List<IMyPlayer> _playerCache;

        public static void Register()
        {
            MyLog.Default.WriteLineAndConsole("TORCH MOD: Registering mod communication.");
            _processing = new BlockingCollection<EconMessageBase>(new ConcurrentQueue<EconMessageBase>());
            _playerCache = new List<IMyPlayer>();
            _messagePool = new MyConcurrentPool<EconIncomingMessage>(8);
            
            MyAPIGateway.Multiplayer.RegisterMessageHandler(NET_ID, MessageHandler);
            MyAPIGateway.Utilities.RegisterMessageHandler(NET_ID, MessageHandler);
            
            //background thread to handle de/compression and processing
            _closing = false;
            MyAPIGateway.Parallel.StartBackground(DoProcessing);
            MyLog.Default.WriteLineAndConsole("TORCH MOD: Mod communication registered successfully.");
        }

        public static void Unregister()
        {
            MyLog.Default.WriteLineAndConsole("TORCH MOD: Unregistering mod communication.");
            MyAPIGateway.Multiplayer?.UnregisterMessageHandler(NET_ID, MessageHandler);
            MyAPIGateway.Utilities?.RegisterMessageHandler(NET_ID, MessageHandler);

            _processing?.CompleteAdding();
            _closing = true;
            //_task.Wait();
        }

        private static void MessageHandler(object message)
        {
            var bytes = message as byte[];
            var m = _messagePool.Get();
            m.CompressedData = bytes;
#if TORCH
            m.SenderId = MyEventContext.Current.Sender.Value;
#endif

            _processing.Add(m);
        }

        public static void DoProcessing()
        {
            while (!_closing)
            {
                try
                {
                    EconMessageBase m;
                    try
                    {
                        m = _processing.Take();
                    }
                    catch
                    {
                        continue;
                    }

                    MyLog.Default.WriteLineAndConsole($"Processing message: {m.GetType().Name}");

                    if (m is EconIncomingMessage) //process incoming messages
                    {
                        EconMessageBase i;
                        try
                        {
                            var o = MyCompression.Decompress(m.CompressedData);
                            m.CompressedData = null;
                            _messagePool.Return((EconIncomingMessage)m);
                            i = MyAPIGateway.Utilities.SerializeFromBinary<EconMessageBase>(o);
                        }
                        catch (Exception ex)
                        {
                            MyLog.Default.WriteLineAndConsole($"TORCH MOD: Failed to deserialize message! {ex}");
                            continue;
                        }

                        MyAPIGateway.Utilities.ShowMessage("Torch", $"Received message of type {i.GetType().Name}");

                        if (MyAPIGateway.Multiplayer.IsServer)
                            i.ProcessServer();
                        else
                            i.ProcessClient();
                    }
                    else //process outgoing messages
                    {
                        MyAPIGateway.Utilities.ShowMessage("Torch", $"Sending message of type {m.GetType().Name}");

                        m.CallbackModChannel = NET_ID;
                        var b = MyAPIGateway.Utilities.SerializeToBinary(m);
                        m.CompressedData = MyCompression.Compress(b);

                        switch (m.TargetType)
                        {
                            case MessageTarget.Single:
                                MyAPIGateway.Multiplayer.SendMessageTo(NET_ID, m.CompressedData, m.Target);
                                break;
                            case MessageTarget.Server:
                                MyAPIGateway.Multiplayer.SendMessageToServer(NET_ID, m.CompressedData);
                                //MyAPIGateway.Utilities.SendModMessage(NET_ID, m.CompressedData);
                                break;
                            case MessageTarget.AllClients:
                                MyAPIGateway.Players.GetPlayers(_playerCache);
                                foreach (var p in _playerCache)
                                {
                                    if (p.SteamUserId == MyAPIGateway.Multiplayer.MyId)
                                        continue;
                                    MyAPIGateway.Multiplayer.SendMessageTo(NET_ID, m.CompressedData, p.SteamUserId);
                                }

                                break;
                            case MessageTarget.AllExcept:
                                MyAPIGateway.Players.GetPlayers(_playerCache);
                                foreach (var p in _playerCache)
                                {
                                    if (p.SteamUserId == MyAPIGateway.Multiplayer.MyId || m.Ignore.Contains(p.SteamUserId))
                                        continue;
                                    MyAPIGateway.Multiplayer.SendMessageTo(NET_ID, m.CompressedData, p.SteamUserId);
                                }

                                break;
                            default:
                                throw new Exception();
                        }

                        _playerCache.Clear();
                    }
                }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLineAndConsole($"TORCH MOD: Exception occurred in communication thread! {ex}");
                }
            }

            MyLog.Default.WriteLineAndConsole("TORCH MOD: INFO: Communication thread shut down successfully! THIS IS NOT AN ERROR");
            //exit signal received. Clean everything and GTFO
            _processing?.Dispose();
            _processing = null;
            _messagePool?.Clean();
            _messagePool = null;
            _playerCache = null;
        }

        public static void SendMessageTo(EconMessageBase message, ulong target)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                throw new Exception("Only server can send targeted messages");

            if (_closing)
                return;

            message.Target = target;
            message.TargetType = MessageTarget.Single;
            _processing.Add(message);
        }

        public static void SendMessageToClients(EconMessageBase message)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                throw new Exception("Only server can send targeted messages");

            if (_closing)
                return;

            message.TargetType = MessageTarget.AllClients;
            _processing.Add(message);
        }

        public static void SendMessageExcept(EconMessageBase message, params ulong[] ignoredUsers)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                throw new Exception("Only server can send targeted messages");

            if (_closing)
                return;

            message.TargetType = MessageTarget.AllExcept;
            message.Ignore = ignoredUsers;
            _processing.Add(message);
        }

        public static void SendMessageToServer(EconMessageBase message)
        {
            if (_closing)
                return;

            message.TargetType = MessageTarget.Server;
            _processing.Add(message);
        }
    }
}
