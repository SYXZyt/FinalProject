using TowerDefencePackets;

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
        private int round;

        private bool playerAReady;
        private bool playerBReady;

        public bool IsOver { get; set; } = false;

        public GameState GameState { get => gameState; set => gameState = value; }

        public Player PlayerA => pA;
        public Player PlayerB => pB;

        public Map Map => map;

        public void Update()
        {
            if (gameState == GameState.WAITING && playerAReady && playerBReady)
            {
                gameState = GameState.IN_PROGRESS;
                playerAReady = playerBReady = false;

                //Tell both clients to start the game
                Console.WriteLine($"{Program.GetDateTime} Round {round} started for Lobby {GetHashCode()}");

                Program.SendMessageToPeer(pA.clientRef, $"{Header.ROUND_BEGIN} {round}");
                Program.SendMessageToPeer(pB.clientRef, $"{Header.ROUND_BEGIN} {round}");

                round++;
            }
        }

        public void PlayerIsReady(long id)
        {
            if (pA.playerNumber == id)
            {
                playerAReady = true;
            }
            else
            {
                playerBReady = true;
            }
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
            round = 0;
            playerAReady = false;
            playerBReady = false;
        }
    }
}