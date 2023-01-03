namespace TowerDefenceServer
{
    internal static class UsernameDB
    {
        private static readonly Dictionary<string, long> knownNames;
        private static readonly List<long> playerIds;
        private static readonly Random rng;

        public static void FreeId(long id) => playerIds.Remove(id);

        public static long NewPlayerID
        {
            get
            {
                long x;
                do x = rng.NextInt64(0, long.MaxValue); while (playerIds.Contains(x));
                playerIds.Add(x);
                return x;
            }
        }

        public static bool UserIsKnown(string name) => knownNames.ContainsKey(name);
        public static long ReadPlayerId(string name) => knownNames[name];
        public static string GetNameFromId(long id) => knownNames.FirstOrDefault(x => x.Value == id).Key;
        public static void RemoveUser(string name) => knownNames.Remove(name);
        public static void MakeKnown(string name, long playerId) => knownNames[name] = playerId;

        static UsernameDB()
        {
            knownNames = new();
            playerIds = new();
            rng = new();
        }
    }
}