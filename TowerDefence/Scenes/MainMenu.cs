using UILibrary;
using AssetStreamer;
using UILibrary.Scenes;
using UILibrary.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Scenes
{
    internal sealed class MainMenu : Scene
    {
        private Label title;
        private ImageTextButton exitButton;
        private ImageTextButton playButton;
        private ImageTextButton settingsButton;

#if DEBUG
        private ImageTextButton debugPlayButton;
#endif

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.Draw(AssetContainer.ReadTexture("sMenu"), Vector2.Zero, Color.White);
            title.DrawWithShadow(spriteBatch);
        }

        public override void DrawGUI(SpriteBatch spriteBatch, GameTime gameTime)
        {

        }

        public override void LoadContent()
        {
            Console.WriteLine($"LOAD MainMenu");
            if (Client.Instance is not null) Client.Instance.PlayerName = ""; //If the client is not null, set the name to blank

            Vector2 pos = new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 50);
            title = new(AssetContainer.ReadString("STR_GAME_NAME"), 2f, pos, Color.White, AssetContainer.GetFont("fMain"), Origin.MIDDLE_CENTRE, 0f);

            short bWidth = 310;
            short bHeight = 72;
            short bX = (short)(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2 - bWidth / 2);
            short bY = (short)(SceneManager.Instance.graphics.PreferredBackBufferHeight * 0.82);

            {
                AABB buttonBoundingBox = new(bX, bY, bWidth, bHeight);
                exitButton = new(buttonBoundingBox, AssetContainer.ReadTexture("sMenuButtonUnclicked"), AssetContainer.ReadTexture("sMenuButtonClicked"), AssetContainer.ReadString("STR_BTN_EXIT"), 1.4f, AssetContainer.GetFont("fMain"));
                uIManager.Add(exitButton);
            }

            {
                AABB buttonBoundingBox = new(bX, (short)(bY - (bHeight * 2)), bWidth, bHeight);
                playButton = new(buttonBoundingBox, AssetContainer.ReadTexture("sMenuButtonUnclicked"), AssetContainer.ReadTexture("sMenuButtonClicked"), AssetContainer.ReadString("STR_BTN_PLAY"), 1.4f, AssetContainer.GetFont("fMain"));
                uIManager.Add(playButton);
            }

            {
                AABB buttonBoundingBox = new(bX, (short)(bY - bHeight), bWidth, bHeight);
                settingsButton = new(buttonBoundingBox, AssetContainer.ReadTexture("sMenuButtonUnclicked"), AssetContainer.ReadTexture("sMenuButtonClicked"), AssetContainer.ReadString("STR_BTN_CFG"), 1.4f, AssetContainer.GetFont("fMain"));
                uIManager.Add(settingsButton);
            }

#if DEBUG
            {
                AABB buttonBoundingBox = new(bX, (short)(bY - bHeight*3), bWidth, bHeight);
                debugPlayButton = new(buttonBoundingBox, AssetContainer.ReadTexture("sMenuButtonUnclicked"), AssetContainer.ReadTexture("sMenuButtonClicked"), "__DEBUG_PLAY__", 1.4f, AssetContainer.GetFont("fMain"));
                uIManager.Add(debugPlayButton);
            }
#endif
        }

        public override void UnloadContent()
        {
            Console.WriteLine($"UNLOAD MainMenu");
            UIManager.Clear();
        }

        public override void Update(GameTime gameTime)
        {
            if (exitButton.IsClicked()) SceneManager.Instance.Exit();

            if (playButton.IsClicked()) SceneManager.Instance.LoadScene("getUsername");
            if (settingsButton.IsClicked()) SceneManager.Instance.LoadScene("settings");

#if DEBUG
            if (debugPlayButton.IsClicked())
            {
                Client c = new();
                c.Connect();
                Game.IsDebugPlay = true;
                c.PlayerName = "__DEBUG_PLAYER__";
                SceneManager.Instance.LoadScene("mainGame");
            }
#endif
        }

        public MainMenu()
        {
            uIManager = new();
        }
    }
}