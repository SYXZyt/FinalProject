using UILibrary;
using AssetStreamer;
using UILibrary.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Scenes
{
    internal sealed class FindGame : Scene
    {
        private Texture2D bkg;
        private Label searchingLabel;

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            bkg.Draw(Vector2.Zero, spriteBatch, Color.White);
            searchingLabel.DrawWithShadow(spriteBatch);
        }

        public override void LoadContent()
        {
            Console.WriteLine("LOAD FindGame");
            bkg = AssetContainer.ReadTexture("sMenu");
            Vector2 c = new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 300);
            searchingLabel = new(AssetContainer.ReadString("LBL_FINDING_GAME"), 1.4f, c, Color.White, AssetContainer.GetFont("fMain"), Origin.MIDDLE_CENTRE, 0f);
        }

        public override void UnloadContent()
        {
            Console.WriteLine("UNLOAD FindGame");
        }

        public override void Update(GameTime gameTime)
        {

        }
    }
}