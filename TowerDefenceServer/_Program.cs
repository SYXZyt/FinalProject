namespace TowerDefenceServer
{
    internal static class _Program
    {
        private static void main()
        {
            _Server server = new();
            server.Start();

            Console.WriteLine("Server Shutdown");
        }
    }
}