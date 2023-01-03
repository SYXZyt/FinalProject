using LiteNetLib;

namespace TowerDefenceServer.ServerData
{
    internal struct Player
    {
        public double msSinceLastHeard;
        public long playerNumber;
        public NetPeer clientRef;

        //Game data
        public ushort money;
        public byte health; 

        public Player()
        {
            msSinceLastHeard = 0;
            playerNumber = 0;
            clientRef = null;

            money = 1000;
            health = 100;
        }
    }
}