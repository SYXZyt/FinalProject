namespace TowerDefenceServer
{
    internal static class UsernameDB
    {
        private static readonly List<string> knownNames;

        public static bool UserIsKnown(string name) => knownNames.Contains(name);
        public static void RemoveUser(string name) => knownNames.Remove(name);
        public static void MakeKnown(string name) => knownNames.Add(name);

        static UsernameDB()
        {
            knownNames = new();
        }
    }
}