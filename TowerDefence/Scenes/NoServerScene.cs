using UILibrary;
using AssetStreamer;
using UILibrary.Scenes;
using UILibrary.Buttons;
using TowerDefence.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Scenes
{
    internal sealed class NoServerScene : Scene
    {
        private Label label;
        private ImageTextButton backButton;
        private Texture2D bkg;

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            bkg.Draw(Vector2.Zero, spriteBatch, Color.White);
        }

        public override void DrawGUI(SpriteBatch spriteBatch, GameTime gameTime)
        {
            label.DrawWithShadow(spriteBatch);
        }

        public override void LoadContent()
        {
            label = new(AssetContainer.ReadString("LBL_ERROR_NO_SERVER").Replace("{0}", GlobalSettings.ServerIP.ToString()).Replace("{1}", GlobalSettings.Port.ToString()), 1.4f, new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 150), Color.White, AssetContainer.GetFont("fMain"), Origin.MIDDLE_CENTRE, 0f);
            bkg = AssetContainer.ReadTexture("sMenu");

            short bWidth = 310;
            short bHeight = 72;
            short bX = (short)(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2 - bWidth / 2);
            short bY = (short)(SceneManager.Instance.graphics.PreferredBackBufferHeight * 0.82);
            AABB back = new(bX, bY, bWidth, bHeight);
            backButton = new(back, AssetContainer.ReadTexture("sMenuButtonUnclicked"), AssetContainer.ReadTexture("sMenuButtonClicked"), AssetContainer.ReadString("LBL_SET_BACK"), 1.4f, AssetContainer.GetFont("fMain"));

            uIManager.Add(backButton);
        }

        public override void UnloadContent() { }

        public override void Update(GameTime gameTime)
        {
            if (backButton.IsClicked())
            {
                SceneManager.Instance.LoadScene("mainMenu");
            }
        }
    }
}