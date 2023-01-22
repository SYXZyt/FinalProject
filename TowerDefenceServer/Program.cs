using LiteNetLib;
using System.Text;
using LiteNetLib.Utils;
using TowerDefencePackets;
using TowerDefenceServer.ServerData;

namespace TowerDefenceServer
{
    internal static class Program
    {
        private static readonly Random rng = new();

        private static readonly EventBasedNetListener listener = new();
        private static NetManager server;

        private static NetPacketProcessor packetProcessor;

        private delegate void ServerTask();
        private static readonly List<ServerTask> tasks = new();

        private static readonly List<Lobby> lobbies = new();

        private static readonly List<(long id, NetPeer client)> playersWaiting = new();

        private static readonly List<Map> loadedMaps = new();

        private static string GetDateTime => $"[{DateTime.Now:T}]";

        /// <summary>
        /// Try to find an ongoing game using a player ID
        /// </summary>
        /// <param name="id">The id to look for</param>
        /// <returns>The lobby, or null if no lobby can be found</returns>
        private static Lobby FindLobbyWithID(long id)
        {
            foreach (Lobby lobby in lobbies)
            {
                if (lobby.PlayerA.playerNumber == id || lobby.PlayerB.playerNumber == id) return lobby;
            }

            return null;
        }

        /// <summary>
        /// Update all of the lobbies that the server has ongoing
        /// </summary>
        private static void UpdateLobbies()
        {
            //Remove any lobby where the IsOver flag is true
            lobbies.RemoveAll(l => l.IsOver);

            foreach (Lobby lobby in lobbies)
            {
                lobby.Update();

                //Check if either player has timed out
                if (lobby.PlayerA.clientRef.TimeSinceLastPacket >= lobby.PlayerA.clientRef.NetManager.DisconnectTimeout)
                {
                    //Tell Player B they win
                    SendMessageToPeer(lobby.PlayerB.clientRef, $"{Header.GAME_OVER}{0x01}");
                    SendMessageToPeer(lobby.PlayerA.clientRef, $"{Header.GAME_OVER}{0x00}");

                    lobby.PlayerA.clientRef.Disconnect();
                    UsernameDB.RemoveUser(UsernameDB.GetNameFromId(lobby.PlayerA.playerNumber));
                    lobby.IsOver = true;
                }
                else if (lobby.PlayerB.clientRef.TimeSinceLastPacket >= lobby.PlayerB.clientRef.NetManager.DisconnectTimeout)
                {
                    //Same but if player b left this time
                    SendMessageToPeer(lobby.PlayerA.clientRef, $"{Header.GAME_OVER}{0x01}");
                    SendMessageToPeer(lobby.PlayerB.clientRef, $"{Header.GAME_OVER}{0x00}");

                    lobby.PlayerB.clientRef.Disconnect();
                    UsernameDB.RemoveUser(UsernameDB.GetNameFromId(lobby.PlayerB.playerNumber));
                    lobby.IsOver = true;
                }
            }
        }

        /// <summary>
        /// Look through all users and try to pair them together into a lobby
        /// </summary>
        private static void TryToFindLobby()
        {
            //If there are not enough players, then we cannot do anything
            if (playersWaiting.Count < 2) return;

            var playerA = new { playersWaiting[0].id, playersWaiting[0].client };
            playersWaiting.RemoveAt(0);

            var playerB = new { playersWaiting[0].id, playersWaiting[0].client };
            playersWaiting.RemoveAt(0);

            //Tell both players the id of the other player
            SendMessageToPeer(playerA.client, $"{Header.CONNECT_LOBBY}{playerB.id}");
            SendMessageToPeer(playerB.client, $"{Header.CONNECT_LOBBY}{playerA.id}");

            Thread.Sleep(20);

            Player a = new()
            {
                clientRef = playerA.client,
                playerNumber = playerA.id,
            };

            Player b = new()
            {
                clientRef = playerB.client,
                playerNumber = playerB.id,
            };


            //Pick a map and send it to both players
            Map map = loadedMaps[rng.Next(loadedMaps.Count)];

            Lobby lobby = new(a, b, map);
            lobbies.Add(lobby);

            StringBuilder builder = new();
            builder.Append(Header.RECEIVE_MAP_DATA);
            foreach (char bite in map.Serialise()) builder.Append(bite);
            string mapData = builder.ToString();

            Thread.Sleep(50);
            SendMessageToPeer(playerA.client, mapData);
            SendMessageToPeer(playerB.client, mapData);

            Console.WriteLine($"{GetDateTime} Found lobby with {playerA.id} and {playerB.id}");
        }

        /// <summary>
        /// Send a message to a connected client
        /// </summary>
        /// <param name="peer">The client to connect to</param>
        /// <param name="message">The message to send</param>
        /// <param name="deliveryMethod">The method to use to send the message</param>
        private static void SendMessageToPeer(NetPeer peer, string message, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            NetDataWriter writer = new();
            writer.Put(message);
            peer?.Send(writer, deliveryMethod);
        }

        /// <summary>
        /// What to do when the server receives a new message
        /// </summary>
        /// <param name="peer">The client which sent the message</param>
        /// <param name="reader">The reader to read the sent message</param>
        /// <param name="deliveryMethod">The method used to send the message</param>
        private static void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            //Read the first byte as it will store what to do
            string data = reader.GetString();
            if (data == "")
            {
                Console.WriteLine($"{GetDateTime} Empty Data");
                return;
            }

            byte op = (byte)data[0];
            data = data[1..];

            //Write the current time of the server
            Console.Write($"{GetDateTime} ");

            switch (op)
            {
                case (byte)Header.REQUEST_USERNAME_AVAILABILITY:
                    Console.WriteLine($"Requested name availability of '{data}' from {peer.EndPoint}");

                    //Send data back of name availability
                    if (UsernameDB.UserIsKnown(data)) SendMessageToPeer(peer, "NEG");
                    else SendMessageToPeer(peer, "ACK");
                    break;
                case (byte)Header.REQUEST_USERNAME:
                    {
                        bool available = !UsernameDB.UserIsKnown(data);

                        long playerId = available ? UsernameDB.NewPlayerID : long.MaxValue;
                        Console.WriteLine($"Requested name '{data}' from {peer.EndPoint}. Assigned PlayerID {playerId}");

                        if (available) UsernameDB.MakeKnown(data, playerId);
                        SendMessageToPeer(peer, playerId.ToString());
                    }
                    break;
                case (byte)Header.DISCONNECT:
                    {
                        //If we are disconnecting a user, then we need to use their username to get their id, and deallocate it
                        //If the name doesn't exist, then skip everything
                        Console.WriteLine($"Disconnecting '{data}'");
                        if (UsernameDB.UserIsKnown(data))
                        {
                            long id = UsernameDB.ReadPlayerId(data);
                            UsernameDB.RemoveUser(data);
                            UsernameDB.FreeId(id);
                        }
                        SendMessageToPeer(peer, "ACK");
                    }
                    break;
                case (byte)Header.REQUEST_TOTAL_CONNECTIONS:
                    Console.WriteLine($"Requested total connections from {peer.EndPoint}");
                    SendMessageToPeer(peer, $"{server.GetPeersCount(ConnectionState.Connected)}");
                    break;
                case (byte)Header.REQUEST_LOBBY:
                    Console.WriteLine($"Player {data} requested lobby");
                    playersWaiting.Add((long.Parse(data), peer));
                    break;
                case (byte)Header.REQUEST_USERNAME_FROM_ID:
                    {
                        Console.WriteLine($"Getting username from ID {data}");
                        long id = long.Parse(data);

                        string username = UsernameDB.GetNameFromId(id);
                        SendMessageToPeer(peer, username);
                    }
                    break;
                case (byte)Header.GAME_OVER:
                    {
                        //We need to read the id of the player who won, and send them a unique message
                        Console.WriteLine($"Game over. {data} ({UsernameDB.GetNameFromId(long.Parse(data))}) is the winner");
                        long id = long.Parse(data);

                        //We need to find the lobby with this player
                        Lobby lobby = FindLobbyWithID(id);
                        if (lobby is null) break;

                        //Now message each user
                        NetPeer winner = lobby.GetPlayerFromID(id).clientRef;
                        NetPeer loser = lobby.GetOtherPlayerFromID(id).clientRef;

                        SendMessageToPeer(winner, $"{Header.GAME_OVER}{0x01}");
                        SendMessageToPeer(loser, $"{Header.GAME_OVER}{0x00}");

                        //Now that the lobby is over, we want to remove it from the active games
                        lobbies.Remove(lobby);
                    }
                    break;
                case (byte)Header.SNAPSHOT:
                    {
                        Console.Write($"Received Snapshot ");

                        Console.WriteLine(data);
                        Snapshot ss = new();
                        ss.Deserialize(data);

                        //Get the lobby that this player is in
                        Lobby lobby = FindLobbyWithID(ss.ID);
                        if (lobby is null) return; //If there is no game found, just stop

                        //Pass the packet onto the other player
                        SendMessageToPeer(lobby.GetOtherPlayerFromID(ss.ID).clientRef, $"{Header.SNAPSHOT}{data}");

                        //Now sync the player who sent the packet
                        SendMessageToPeer(lobby.GetPlayerFromID(ss.ID).clientRef, $"{Header.SYNC}{lobby.GetPlayerFromID(ss.ID).GenerateSync()}");
                    }
                    break;
                case (byte)Header.HAS_LOBBY:
                    {
                        Console.WriteLine("Received lobby search update");

                        //Check each lobby and see if the username is found. If it is, send the game data
                        foreach (Lobby lobby in lobbies)
                        {
                            if (data == UsernameDB.GetNameFromId(lobby.PlayerA.playerNumber))
                            {
                                SendMessageToPeer(peer, $"{Header.CONNECT_LOBBY}{lobby.PlayerB.playerNumber}");

                                StringBuilder builder = new();
                                builder.Append(Header.RECEIVE_MAP_DATA);
                                foreach (char bite in lobby.Map.Serialise()) builder.Append(bite);
                                string mapData = builder.ToString();

                                Thread.Sleep(100);
                                SendMessageToPeer(peer, $"{Header.RECEIVE_MAP_DATA}{mapData}");
                            }
                            else if (data == UsernameDB.GetNameFromId(lobby.PlayerB.playerNumber))
                            {
                                SendMessageToPeer(peer, $"{Header.CONNECT_LOBBY}{lobby.PlayerA.playerNumber}");

                                StringBuilder builder = new();
                                builder.Append(Header.RECEIVE_MAP_DATA);
                                foreach (char bite in lobby.Map.Serialise()) builder.Append(bite);
                                string mapData = builder.ToString();

                                Thread.Sleep(100);
                                SendMessageToPeer(peer, $"{Header.RECEIVE_MAP_DATA}{mapData}");
                            }
                        }
                    }
                    break;
                case (byte)Header.ADD_MONEY:
                    {
                        string[] split = data.Split(",");
                        long playerId = long.Parse(split[0]);
                        ushort amountToAdd = ushort.Parse(split[1]);

                        Lobby lobby = FindLobbyWithID(playerId);
                        if (lobby is null) break;

                        Player player = lobby.GetPlayerFromID(playerId);
                        player.money += amountToAdd;
                        lobby.UpdatePlayer(playerId, player);
                    }
                    break;
                default:
                    Console.WriteLine($"Received: {data}' from {peer.EndPoint} with unknown header 0x{op:x2}");
                    break;
            }
        }

        private static void LoadMaps()
        {
            //If the map folder does not exist, throw an error
            if (!Directory.Exists("Maps")) throw new DirectoryNotFoundException($"Could not find maps directory. Expected {Path.GetFullPath("maps")}");

            string[] mapPaths = Directory.GetFiles("maps", "*.map");

            //If no maps were loaded, throw an error as we need at least one map
            if (mapPaths.Length == 0) throw new Exception("Could not find any map files");

            foreach (string map in mapPaths)
            {
                Console.WriteLine($"Loaded {map}");
                Map _map = Map.LoadFromDisk(map);
                loadedMaps.Add(_map);
            }
        }

        private static void Main()
        {
            LoadMaps();
            server = new(listener);
            server.Start(9050);
            string title = $"Server running on port {server.LocalPort}";
            Console.WriteLine(title);
            Console.WriteLine(new string('~', title.Length));

            tasks.Add(server.PollEvents);
            tasks.Add(TryToFindLobby);
            tasks.Add(UpdateLobbies);

            packetProcessor = new();
            packetProcessor.SubscribeReusable<Snapshot>((snapshot) =>
            {
                Console.WriteLine($"Received game snapshot");

                Lobby lobby = FindLobbyWithID(snapshot.ID);
                if (lobby is not null)
                {
                        //Send the snapshot to the other player
                    NetPeer other = lobby.GetOtherPlayerFromID(snapshot.ID).clientRef;
                    packetProcessor.Send(other, new Snapshot() { Health = snapshot.Health, ID = snapshot.ID, Money = snapshot.Money }, DeliveryMethod.ReliableOrdered);
                }
            });

            listener.ConnectionRequestEvent += request =>
            {
                request.Accept();
            };

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine($"{GetDateTime} We got a connection {peer.EndPoint}");
                NetDataWriter writer = new();
                writer.Put("Connected");
                peer.Send(writer, DeliveryMethod.ReliableUnordered);
            };

            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;

            while (true)
            {
                foreach (ServerTask task in tasks)
                {
                    task.Invoke();
                    Thread.Sleep(15);
                }

                Console.CursorVisible = false;
            }
        }
    }
}