using UILibrary.Scenes;
using TowerDefence.Scenes;

namespace TowerDefence
{
    internal static class Program
    {
        internal static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = args.ExceptionObject as Exception;
            Console.WriteLine($"ERROR {e.Message}");
            MessageBox.Display($"Unhandled exception.\n{e}");
            Environment.Exit(1);
        }

        [STAThread]
        internal static void Main()
        {
            AppDomain appDomain = AppDomain.CurrentDomain;
            //appDomain.UnhandledException += new UnhandledExceptionEventHandler(ExceptionHandler);

            Console.WriteLine("Starting Game");

            _ = new SceneManager(new(1920, 1080));

            GameLoadingScreen gameLoadingScreen = new();
            MainMenu mainMenu = new();
            GetUsernameScene getUsername = new();
            Game gameScene = new();
            FindGame findGame = new();

            SceneManager.Instance.AddScene("gameLoadingScreen", gameLoadingScreen);
            SceneManager.Instance.AddScene("mainMenu", mainMenu);
            SceneManager.Instance.AddScene("getUsername", getUsername);
            SceneManager.Instance.AddScene("mainGame", gameScene);
            SceneManager.Instance.AddScene("findGame", findGame);

            SceneManager.Instance.SetScene("gameLoadingScreen");
            SceneManager.Instance.Run();
        }
    }
}