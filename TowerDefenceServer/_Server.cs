using LiteNetLib;
using LiteNetLib.Utils;
using TowerDefencePackets;
using System.Collections.Concurrent;
using TowerDefenceServer.ServerData;

namespace TowerDefenceServer
{
    internal sealed class _Server
    {
        private readonly ConcurrentDictionary<string, Player> knownPlayers;

        private readonly NetSerializer netSerializer = new();
        private readonly NetPacketProcessor packetProcessor = new();

        private short nextPlayerId = 0;
        private int GetNextPlayerId() => nextPlayerId++;

        private List<string> SearchForUnmatchedPlayers() => knownPlayers.Where(x => x.Value.gameSessionID == string.Empty).Select(x => x.Value.gameSessionID).ToList();

        public void Start()
        {
            Console.WriteLine("===| Server Started |===");
            EventBasedNetListener listener = new();
            NetManager server = new(listener);
            server.Start(9000);

            netSerializer.Register<WelcomePacket>();
            netSerializer.Register<UpdatePacket>();
            netSerializer.Register<ServerSnapshotPacket>();
            netSerializer.Register<EchoPacket>();

            packetProcessor.SubscribeReusable<UpdatePacket, NetPeer>(HandleClientUpdate);

            listener.ConnectionRequestEvent += request =>
            {
                //Only accept two players
                if (server.ConnectedPeersCount < 2) request.AcceptIfKey("__key__");
                else request.Reject();
            };

            listener.PeerConnectedEvent += peer =>
            {
                string key = peer.EndPoint.ToString();
                if (!knownPlayers.ContainsKey(key))
                {
                    //New player connected
                    Player pd = new()
                    {
                        //Generate a unique ID for the peer connecting which will be referenced in the future
                        clientID = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", ""),
                        gameSessionID = string.Empty,
                        ip = peer.EndPoint.Address,
                        port = peer.EndPoint.Port,
                        msSinceLastHeard = 0,
                        playerNumber = GetNextPlayerId()+1,
                        clientRef = peer,
                        health = 100,
                        money = 1000,
                    };
                }

                Player pdat = knownPlayers[key];
                Console.WriteLine($"Player Joined: {peer.EndPoint} as player {pdat.playerNumber} with id {pdat.clientID}");
                WelcomePacket wp = new()
                {
                    Health = pdat.health,
                    Money = pdat.money,
                    PlayerID = pdat.clientID,
                    PlayerNumber = pdat.playerNumber,
                };

                packetProcessor.Send<WelcomePacket>(peer, wp, DeliveryMethod.ReliableOrdered);
                Console.WriteLine("Packet Processor sent packet");
            };

            listener.NetworkReceiveEvent += ListenerNetworkReceiveEvent;

            while (!Console.KeyAvailable)
            {
                server.PollEvents();
                UpdateAllClients();
                Thread.Sleep(15);
            }
            server.Stop();
        }

        private void UpdateAllClients()
        {
            if (knownPlayers.IsEmpty) return;

            ServerSnapshotPacket snapshot = new();
            snapshot.Players = nextPlayerId;
            snapshot.Health = new byte[snapshot.Players];
            snapshot.Money = new ushort[snapshot.Players];

            for (int i = 0; i < snapshot.Players; i++)
            {
                Player? p = knownPlayers.Values.Where(x => x.playerNumber == (i + 1)).FirstOrDefault();
                if (p is null) break;

                snapshot.Health[i] = p.Value.health;
                snapshot.Money[i] = p.Value.money;
            }

            foreach (var client in knownPlayers.Values)
            {
                //Send a snapshot to each player
                if (client.clientRef.ConnectionState == ConnectionState.Connected)
                {
                    packetProcessor.Send<ServerSnapshotPacket>(client.clientRef, snapshot, DeliveryMethod.ReliableOrdered);
                }
            }
        }

        private void HandleClientUpdate(UpdatePacket update, NetPeer peer)
        {

        }

        private void ListenerNetworkReceiveEvent(NetPeer peer, NetDataReader reader, DeliveryMethod deliveryMethod)
        {
            packetProcessor.ReadAllPackets(reader, peer);

            string key = peer.EndPoint.ToString();
            if (knownPlayers.ContainsKey(key))
            {

            }
            else
            {
                Console.WriteLine($"Unknown client connected as {key}");
            }
        }

        public _Server()
        {
            knownPlayers = new();
        }
    }
}