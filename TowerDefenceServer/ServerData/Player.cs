using LiteNetLib;
using System.Net;

namespace TowerDefenceServer.ServerData
{
    internal struct Player
    {
        public double msSinceLastHeard;
        public IPAddress ip;
        public int port;
        public string clientID;
        public string gameSessionID;
        public int playerNumber;
        public NetPeer clientRef;

        //Game data
        public ushort money;
        public byte health; 
    }
}