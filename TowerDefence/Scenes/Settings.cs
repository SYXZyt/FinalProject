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
        private Switch settingPlayerSide;

        private Label labelFullscreen;
        private Label labelFullscreenSplash;
        private Label labelIP;
        private Label labelPort;
        private Label labelPlayerSide;

        private ImageTextButton backButton;
        private ImageTextButton applyButton;

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
            labelPlayerSide.DrawWithShadow(spriteBatch);
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

            AABB playerSideBox = new((short)(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2 - 16), 500, 32, 32);
            settingPlayerSide = new(playerSideBox, AssetContainer.ReadTexture("sSettingWindowed"), AssetContainer.ReadTexture("sSettingFullscreen"), !GlobalSettings.PlayerOnRight);

            settingIP.SetActive(true);
            settingPort.SetActive(true);

            UIManager.Add(settingFullscreen);
            UIManager.Add(settingIP);
            UIManager.Add(settingPort);
            UIManager.Add(settingPlayerSide);

            labelFullscreen = new(AssetContainer.ReadString("LBL_SET_SCREEN_MODE"), 1f, new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 185), Color.White, AssetContainer.GetFont("fMain"), Origin.BOTTOM_CENTRE, 0f);
            labelFullscreenSplash = new(AssetContainer.ReadString("LBL_SET_SCREEN_MODE_SPLASH"), 0.7f, new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 200), Color.White, AssetContainer.GetFont("fMain"), Origin.BOTTOM_CENTRE, 0f);
            labelIP = new(AssetContainer.ReadString("LBL_SET_SERVER"), 1f, new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 300), Color.White, AssetContainer.GetFont("fMain"), Origin.BOTTOM_CENTRE, 0f);
            labelPort = new(AssetContainer.ReadString("LBL_SET_PORT"), 1f, new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 400), Color.White, AssetContainer.GetFont("fMain"), Origin.BOTTOM_CENTRE, 0f);
            labelPlayerSide = new(AssetContainer.ReadString("LBL_SET_PLAYER_SIDE"), 1f, new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 500), Color.White, AssetContainer.GetFont("fMain"), Origin.BOTTOM_CENTRE, 0f);

            short bWidth = 310;
            short bHeight = 72;
            short bX = (short)(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2 - bWidth / 2);
            short bY = (short)(SceneManager.Instance.graphics.PreferredBackBufferHeight * 0.82);
            AABB apply = new(bX, bY, bWidth, bHeight);

            applyButton = new(apply, AssetContainer.ReadTexture("sMenuButtonUnclicked"), AssetContainer.ReadTexture("sMenuButtonClicked"), AssetContainer.ReadString("LBL_SET_APPLY"), 1.4f, AssetContainer.GetFont("fMain"));

            UIManager.Add(applyButton);

            bWidth = 96;
            bHeight = 48;
            bY = 0;
            bX = 0;
            AABB back = new(bX, bY, bWidth, bHeight);

            backButton = new(back, AssetContainer.ReadTexture("sMenuButtonUnclicked"), AssetContainer.ReadTexture("sMenuButtonClicked"), AssetContainer.ReadString("LBL_SET_BACK"), 1.4f, AssetContainer.GetFont("fMain"));

            UIManager.Add(backButton);
        }

        public override void UnloadContent()
        {
            Console.WriteLine("UNLOAD Settings");
            UIManager.Clear();
        }

        public override void Update(GameTime gameTime)
        {
            if (KeyboardController.IsPressed(Keys.Escape) || backButton.IsClicked())
            {
                Save();
                SceneManager.Instance.LoadScene("mainMenu");
            }

            if (settingIP.IsEntered)
            {
                //Check that we have a valid IP address
                if (IPAddress.TryParse(settingIP.GetText().ToString(), out IPAddress ip)) GlobalSettings.ServerIP = ip;
            }
            if (settingPort.IsEntered)
            {
                if (int.TryParse(settingPort.GetText().ToString(), out int port)) GlobalSettings.Port = port;
            }

            if (settingFullscreen.State != GlobalSettings.Fullscreen)
            {
                GlobalSettings.Fullscreen = settingFullscreen.State;
                GlobalSettings.ApplySettings();
            }

            if (settingPlayerSide.State == GlobalSettings.PlayerOnRight)
            {
                GlobalSettings.PlayerOnRight = !settingPlayerSide.State;
                GlobalSettings.ApplySettings();
            }

            if (applyButton.IsClicked()) Save();
        }

        private void Save()
        {
            if (IPAddress.TryParse(settingIP.GetText().ToString(), out IPAddress ip)) GlobalSettings.ServerIP = ip;
            if (int.TryParse(settingPort.GetText().ToString(), out int port)) GlobalSettings.Port = port;
            GlobalSettings.Fullscreen = settingFullscreen.State;
            GlobalSettings.PlayerOnRight = !settingPlayerSide.State;
            GlobalSettings.ApplySettings();
            SettingFileHandler.SaveSettingsFile();
        }
    }
}