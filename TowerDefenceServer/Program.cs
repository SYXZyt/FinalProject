using LiteNetLib;
using LiteNetLib.Utils;
using TowerDefencePackets;

namespace TowerDefenceServer
{
    internal static class Program
    {
        static readonly EventBasedNetListener listener = new();
        static NetManager server;
        
        private static string GetDateTime => $"[{DateTime.Now:T}]";

        private static void Main()
        {
            server = new(listener);
            server.Start(9050);
            Console.WriteLine($"Server running {server.LocalPort}");

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

            while (!Console.KeyAvailable)
            {
                server.PollEvents();
                Thread.Sleep(15);
            }

            server.Stop();
        }

        private static void SendMessageToPeer(NetPeer peer, string  message, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            NetDataWriter writer = new();
            writer.Put(message);
            peer.Send(writer, deliveryMethod);
        }

        private static void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            //Read the first byte as it will store what to do
            string data = reader.GetString();

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
                        } SendMessageToPeer(peer, "ACK");
                    }
                    break;
                case (byte)Header.REQUEST_TOTAL_CONNECTIONS:
                    Console.WriteLine($"Requested total connections from {peer.EndPoint}");
                    SendMessageToPeer(peer, $"{server.GetPeersCount(ConnectionState.Connected)}");
                    break;
                default:
                    Console.WriteLine($"Received: {data}' from {peer.EndPoint} with unknown header 0x{op:x2}");
                    break;
            }
        }
    }
}