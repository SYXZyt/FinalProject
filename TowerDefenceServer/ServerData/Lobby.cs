namespace TowerDefenceServer.ServerData
{
    internal enum GameState
    {
        IN_PROGRESS,
        WAITING,
    }

    internal sealed class Lobby : IUpdatable
    {
        private Player pA;
        private Player pB;
        private readonly Map map;
        private GameState gameState;

        public bool IsOver { get; set; } = false;

        public GameState  GameState { get => gameState; set => gameState = value; }

        public Player PlayerA => pA;
        public Player PlayerB => pB;

        public Map Map => map;

        public void Update()
        {

        }

        public void UpdatePlayer(long id, Player player)
        {
            if (pA.playerNumber == id) pA = player;
            else pB = player;
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
            gameState = GameState.WAITING;
        }
    }
}