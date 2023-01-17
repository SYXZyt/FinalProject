using UILibrary;
using AssetStreamer;
using UILibrary.Scenes;
using TowerDefencePackets;
using TowerDefence.Visuals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using TextureCollection = TowerDefence.Visuals.TextureCollection;

namespace TowerDefence.Scenes
{
    internal sealed class FindGame : Scene
    {
        private Texture2D bkg;
        private Label searchingLabel;
        private ulong tick;
        private Animation searchAnim;

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            bkg.Draw(Vector2.Zero, spriteBatch, Color.White);
            searchingLabel.DrawWithShadow(spriteBatch);

            Texture2D activeFrame = searchAnim.GetActiveFrame();
            activeFrame.Draw(new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2 - activeFrame.Width / 2, 330), spriteBatch, Color.White);
        }

        public override void DrawGUI(SpriteBatch spriteBatch, GameTime gameTime)
        {

        }

        public override void LoadContent()
        {
            Console.WriteLine("LOAD FindGame");
            bkg = AssetContainer.ReadTexture("sMenu");
            Vector2 c = new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 300);
            searchingLabel = new(AssetContainer.ReadString("LBL_FINDING_GAME"), 1.4f, c, Color.White, AssetContainer.GetFont("fMain"), Origin.MIDDLE_CENTRE, 0f);

            Client.Instance.SendMessage($"{Header.REQUEST_LOBBY}{Client.Instance.PlayerID}");
            tick = 0;

            TextureCollection textures = new();
            for (int i = 0; i < 8; i++) textures.AddTexture(AssetContainer.ReadTexture($"sLoad_{i}"));
            searchAnim = new(textures, 7, AnimationPlayType.LOOP);
        }

        public override void UnloadContent()
        {
            Console.WriteLine("UNLOAD FindGame");
        }

        public override void Update(GameTime gameTime)
        {
            searchAnim.Update(gameTime);

            //Check if we have a newest message
            if (Client.Instance.MessageCount > 0)
            {
                //If we have anything here then we want to check if it is a lobby confirm packet
                string packet = Client.Instance.ReadLatestMessage();
                byte op = (byte)packet[0];
                packet = packet[1..];

                if (op == (byte)Header.CONNECT_LOBBY)
                {
                    Console.WriteLine("Found enemy");

                    //The rest of the string is the opponent id, so we need to store that
                    long oID = long.Parse(packet);
                    Client.Instance.EnemyID = oID;
                    SceneManager.Instance.LoadScene("mainGame");
                }
            }

            Client.Instance.PollEvents();

            if ((++tick) % (60 * 5) == 0)
            {
                Client.Instance.SendMessage($"{Header.HAS_LOBBY}{Client.Instance.PlayerName}");
            }
        }
    }
}