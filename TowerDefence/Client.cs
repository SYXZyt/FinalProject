using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using TowerDefencePackets;
using TowerDefence.Settings;

namespace TowerDefence
{
    internal class Client
    {
        private static Client instance;
        public static Client Instance => instance;

        private readonly EventBasedNetListener listener;
        private readonly NetManager client;
        private NetDataWriter writer;
        private NetPeer server;

        private readonly List<string> messages;

        public NetManager NetManager => client;

        public long PlayerID { get; set; } = -1;
        public string PlayerName { get; set; } = string.Empty;

        public long EnemyID { get; set; } = -1;

        public string PeekOldest => messages.Count > 0 ? messages[0] : null;
        public string PeekLatest => messages.Count > 0 ? messages[^1] : null;

        public int MessageCount => messages.Count;

        public bool IsConnected => server is not null;

        private void OnPeerConnect(NetPeer peer) => server = peer;

        public void WaitForNewMessage()
        {
            int count = MessageCount;
            while (count == MessageCount) PollEvents();
        }

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

        public IPAddress IP { get; set; } = GlobalSettings.ServerIP;
        public int Port { get; set; } = GlobalSettings.Port;

        public void Connect()
        {
            IPEndPoint endPoint = new(IP, Port);

            client.Connect(endPoint, string.Empty);
            writer = new();

            listener.PeerConnectedEvent += OnPeerConnect;

            listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) =>
            {
                messages.Add(dataReader.GetString());
                Console.WriteLine($"Received From Server: {messages[^1]}");
                dataReader.Recycle();
            };
        }

        public void Disconnect()
        {
            SendMessage($"{Header.DISCONNECT}{PlayerName}");
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
        }
    }
}