namespace TowerDefenceServer.ServerData
{
    internal sealed class Lobby : IUpdatable
    {
        private readonly Player pA;
        private readonly Player pB;
        private readonly Map map;

        public bool IsOver { get; set; } = false;

        public Player PlayerA => pA;
        public Player PlayerB => pB;

        public Map Map => map;

        public void Update()
        {

        }

        public Player GetPlayerFromID(long id)
        {
            if (pA.playerNumber == id) return pA;
            return pB;
        }

        public Player GetOtherPlayerFromID(long id)
        {
            if (pA.playerNumber == id) return pB;
            return pA;
        }

        public Lobby(Player pA, Player pB, Map map)
        {
            this.pA = pA;
            this.pB = pB;
            this.map = map;
        }
    }
}