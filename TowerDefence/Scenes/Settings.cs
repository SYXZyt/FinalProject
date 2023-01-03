using UILibrary;
using System.Net;
using UILibrary.IO;
using AssetStreamer;
using UILibrary.Scenes;
using UILibrary.Buttons;
using TowerDefence.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Scenes
{
    internal sealed class Settings : Scene
    {
        private Texture2D bkg;
        private Switch settingFullscreen;

        private Textbox settingIP;
        private Textbox settingPort;

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            bkg.Draw(Vector2.Zero, spriteBatch, Color.White);
        }

        public override void DrawGUI(SpriteBatch spriteBatch, GameTime gameTime) { }

        public override void LoadContent()
        {
            Console.WriteLine("LOAD Settings");
            bkg = AssetContainer.ReadTexture("sMenu");

            AABB fullScreenBox = new((short)(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2 - 32), 200, 32, 32);
            settingFullscreen = new(fullScreenBox, AssetContainer.ReadTexture("sMenuButtonUnclicked"), AssetContainer.ReadTexture("sMenuButtonClicked"));
            settingFullscreen.SetState(GlobalSettings.Fullscreen);

            Vector2 ipBox = new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2 - 150, 300);
            settingIP = new(ipBox, AssetContainer.GetFont("fMain"), AssetContainer.ReadTexture("sTextboxBkg"), 20, 1f, GlobalSettings.ServerIP.ToString());

            ipBox = new(ipBox.X, 400);
            settingPort = new(ipBox, AssetContainer.GetFont("fMain"), AssetContainer.ReadTexture("sTextboxBkg"), 20, 1f, GlobalSettings.Port.ToString());

            settingIP.SetActive(true);
            settingPort.SetActive(true);

            UIManager.Add(settingFullscreen);
            UIManager.Add(settingIP);
            UIManager.Add(settingPort);
        }

        public override void UnloadContent()
        {
            Console.WriteLine("UNLOAD Settings");
        }

        public override void Update(GameTime gameTime)
        {
            if (KeyboardController.IsPressed(Keys.Escape))
            {
                if (IPAddress.TryParse(settingIP.GetText().ToString(), out IPAddress ip)) GlobalSettings.ServerIP = ip;
                if (!int.TryParse(settingPort.GetText().ToString(), out int port)) GlobalSettings.Port = port;

                GlobalSettings.ApplySettings();
                SettingFileHandler.SaveSettingsFile();
                SceneManager.Instance.LoadScene("mainMenu");
            }

            if (settingIP.IsEntered)
            {
                //Check that we have a valid IP address
                if (IPAddress.TryParse(settingIP.GetText().ToString(), out IPAddress ip)) GlobalSettings.ServerIP = ip;
            }
            if (settingPort.IsEntered)
            {
                if (!int.TryParse(settingPort.GetText().ToString(), out int port)) GlobalSettings.Port = port;
            }

            if (settingFullscreen.State != GlobalSettings.Fullscreen)
            {
                GlobalSettings.Fullscreen = settingFullscreen.State;
                GlobalSettings.ApplySettings();
            }
        }
    }
}