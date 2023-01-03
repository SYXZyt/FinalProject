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

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            bkg.Draw(Vector2.Zero, spriteBatch, Color.White);
            searchingLabel.DrawWithShadow(spriteBatch);
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
        }

        public override void UnloadContent()
        {
            Console.WriteLine("UNLOAD FindGame");
        }

        public override void Update(GameTime gameTime)
        {
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
        }
    }
}