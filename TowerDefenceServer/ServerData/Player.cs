using LiteNetLib;

namespace TowerDefenceServer.ServerData
{
    internal struct Player
    {
        public long playerNumber;
        public NetPeer clientRef;

        //Game data
        public ushort money;
        public byte health;

        public string GenerateSync()
        {
            return $"{money},{health}";
        }

        public Player()
        {
            playerNumber = 0;
            clientRef = null;

            money = 1000;
            health = 100;
        }
    }
}