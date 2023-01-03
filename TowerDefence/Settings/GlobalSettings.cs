using System.Net;
using UILibrary.Scenes;

namespace TowerDefence.Settings
{
    internal static class GlobalSettings
    {
        public static bool Fullscreen { get; set; } = true;
        public static string Username { get; set; } = "Unnamed";
        public static IPAddress ServerIP { get; set; } = IPAddress.Loopback;
        public static int Port { get; set; } = 9050;

        public static void ApplySettings()
        {
            SceneManager.Instance.graphics.IsFullScreen = Fullscreen;
            SceneManager.Instance.graphics.ApplyChanges();

            if (Client.Instance is not null && Client.Instance.IsConnected) Client.Instance.Disconnect();
        }
    }
}