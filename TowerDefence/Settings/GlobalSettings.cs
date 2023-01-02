using UILibrary.Scenes;

namespace TowerDefence.Settings
{
    internal static class GlobalSettings
    {
        public static bool Fullscreen { get; set; }
        public static string Username { get; set; } = "Unnamed";

        public static void ApplySettings()
        {
            SceneManager.Instance.graphics.IsFullScreen = Fullscreen;
            SceneManager.Instance.graphics.ApplyChanges();
        }
    }
}