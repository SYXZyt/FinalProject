namespace TowerDefenceServer.ServerData
{
    internal static class Usernames
    {
        private static readonly List<string> usernames;

        public static bool UsernameExists(string name) => usernames.Contains(name);

        public static bool AddName(string name)
        {
            if (UsernameExists(name)) return false;
            usernames.Add(name);
            return true;
        }

        public static void RemoveUsername(string name) => usernames.Remove(name);

        static Usernames()
        {
            usernames = new();
        }
    }
}