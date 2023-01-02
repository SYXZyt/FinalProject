using LiteNetLib;
using LiteNetLib.Utils;
using TowerDefencePackets;

namespace TowerDefenceServer
{
    internal static class Program
    {
        static readonly EventBasedNetListener listener = new();
        static NetManager server;
        static readonly NetPacketProcessor processor = new();
        
        static void Main()
        {
            UsernameDB.MakeKnown("Kase");

            server = new(listener);
            server.Start(9050);
            Console.WriteLine($"Server running {server.LocalPort}");

            listener.ConnectionRequestEvent += request =>
            {
                request.Accept();
            };

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine($"We got a connection {peer.EndPoint}");
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
            Console.Write($"[{DateTime.Now}] ");

            switch (op)
            {
                case 1:
                    Console.WriteLine($"Requested name availability of '{data}' from {peer.EndPoint}");

                    //Send data back of name availability
                    if (UsernameDB.UserIsKnown(data)) SendMessageToPeer(peer, "NEG");
                    else SendMessageToPeer(peer, "ACK");
                    break;
                default:
                    Console.WriteLine($"Received: {data}' from {peer.EndPoint} with unknown header 0x{op:x2}");
                    break;
            }
        }
    }
}