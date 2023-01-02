namespace TowerDefenceServer
{
    internal static class Program
    {
        private static void Main()
        {
            Server server = new();
            server.Start();

            Console.WriteLine("Server Shutdown");
        }
    }
}