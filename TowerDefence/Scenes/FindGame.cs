using UILibrary;
using AssetStreamer;
using UILibrary.Scenes;
using TowerDefencePackets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Scenes
{
    internal sealed class FindGame : Scene
    {
        private Texture2D bkg;
        private Label searchingLabel;
        private Label playerCount;

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            bkg.Draw(Vector2.Zero, spriteBatch, Color.White);
            searchingLabel.DrawWithShadow(spriteBatch);
            playerCount.DrawWithShadow(spriteBatch);
        }

        public override void DrawGUI(SpriteBatch spriteBatch, GameTime gameTime)
        {
            
        }

        public override void LoadContent()
        {
            int messageCount = Client.Instance.MessageCount;
            Client.Instance.SendMessage($"{Header.REQUEST_TOTAL_CONNECTIONS}\0");
            while (messageCount == Client.Instance.MessageCount) Client.Instance.PollEvents();
            int playerCount = int.Parse(Client.Instance.ReadLatestMessage());
            string playerCountMsg = $"{AssetContainer.ReadString("LBL_PLAYER_COUNT")} {playerCount}";

            Console.WriteLine("LOAD FindGame");
            bkg = AssetContainer.ReadTexture("sMenu");
            Vector2 c = new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 300);
            searchingLabel = new(AssetContainer.ReadString("LBL_FINDING_GAME"), 1.4f, c, Color.White, AssetContainer.GetFont("fMain"), Origin.MIDDLE_CENTRE, 0f);
            this.playerCount = new(playerCountMsg, 1.2f, new(SceneManager.Instance.graphics.PreferredBackBufferWidth, 0), Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_RIGHT, 0f);
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