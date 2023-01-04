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

        private Label labelFullscreen;
        private Label labelFullscreenSplash;
        private Label labelIP;
        private Label labelPort;

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            bkg.Draw(Vector2.Zero, spriteBatch, Color.White);
        }

        public override void DrawGUI(SpriteBatch spriteBatch, GameTime gameTime)
        {
            labelFullscreen.DrawWithShadow(spriteBatch);
            labelFullscreenSplash.DrawWithShadow(spriteBatch);
            labelIP.DrawWithShadow(spriteBatch);
            labelPort.DrawWithShadow(spriteBatch);
        }

        public override void LoadContent()
        {
            Console.WriteLine("LOAD Settings");
            bkg = AssetContainer.ReadTexture("sMenu");

            AABB fullScreenBox = new((short)(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2 - 16), 200, 32, 32);
            settingFullscreen = new(fullScreenBox, AssetContainer.ReadTexture("sSettingWindowed"), AssetContainer.ReadTexture("sSettingFullscreen"));
            settingFullscreen.SetState(GlobalSettings.Fullscreen);

            Vector2 ipBox = new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 300);
            settingIP = new(ipBox, AssetContainer.GetFont("fMain"), AssetContainer.ReadTexture("sTextboxBkg"), 20, 1f, Origin.TOP_CENTRE, GlobalSettings.ServerIP.ToString());

            ipBox = new(ipBox.X, 400);
            settingPort = new(ipBox, AssetContainer.GetFont("fMain"), AssetContainer.ReadTexture("sTextboxBkg"), 20, 1f, Origin.TOP_CENTRE, GlobalSettings.Port.ToString());

            settingIP.SetActive(true);
            settingPort.SetActive(true);

            UIManager.Add(settingFullscreen);
            UIManager.Add(settingIP);
            UIManager.Add(settingPort);

            labelFullscreen = new(AssetContainer.ReadString("LBL_SET_SCREEN_MODE"), 1f, new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 185), Color.White, AssetContainer.GetFont("fMain"), Origin.BOTTOM_CENTRE, 0f);
            labelFullscreenSplash = new(AssetContainer.ReadString("LBL_SET_SCREEN_MODE_SPLASH"), 0.7f, new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 200), Color.White, AssetContainer.GetFont("fMain"), Origin.BOTTOM_CENTRE, 0f);
            labelIP = new(AssetContainer.ReadString("LBL_SET_SERVER"), 1f, new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 300), Color.White, AssetContainer.GetFont("fMain"), Origin.BOTTOM_CENTRE, 0f);
            labelPort = new(AssetContainer.ReadString("LBL_SET_PORT"), 1f, new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 400), Color.White, AssetContainer.GetFont("fMain"), Origin.BOTTOM_CENTRE, 0f);
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