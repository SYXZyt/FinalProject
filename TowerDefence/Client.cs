using LiteNetLib;
using LiteNetLib.Utils;
using TowerDefencePackets;

namespace TowerDefence
{
    internal class Client
    {
        private static Client instance;
        public static Client Instance => instance;

        private readonly EventBasedNetListener listener;
        private readonly NetManager client;
        private readonly NetPacketProcessor processor;
        private NetDataWriter writer;
        private NetPeer server;

        private readonly List<string> messages;

        private void OnPeerConnect(NetPeer peer) => server = peer;

        public string ReadOldestMessage()
        {
            if (MessageCount > 0)
            {
                string msg = messages[0]; messages.RemoveAt(0);
                return msg;
            } return null;
        }

        public string ReadLatestMessage()
        {
            if (MessageCount > 0)
            {
                string msg = messages[^1]; messages.RemoveAt(MessageCount - 1);
                return msg;
            }
            return null;
        }

        public int MessageCount => messages.Count;

        public bool IsConnected => server is not null;

        public void Connect()
        {
            client.Connect("localhost", 9050, string.Empty);
            writer = new();

            listener.PeerConnectedEvent += OnPeerConnect;

            listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) =>
            {
                messages.Add(dataReader.GetString());
                Console.WriteLine($"We got: {messages[^1]}");
                dataReader.Recycle();
            };
        }

        public void SendMessage(string message)
        {
            if (server is null) return;

            writer.Reset();
            writer.Put(message);
            server.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        public void PollEvents() => client.PollEvents();

        public Client()
        {
            instance = this;
            listener = new();
            client = new(listener);
            client.Start();
            messages = new();
            processor = new();
        }
    }
}