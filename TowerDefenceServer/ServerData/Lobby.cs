namespace TowerDefenceServer.ServerData
{
    internal sealed class Lobby : IUpdatable
    {
        private readonly Player pA;
        private readonly Player pB;

        public Player PlayerA => pA;
        public Player PlayerB => pB;

        public void Update()
        {

        }

        public void EndLobby(bool isPlayerAWinner)
        {

        }

        public Lobby(Player pA, Player pB)
        {
            this.pA = pA;
            this.pB = pB;
        }
    }
}