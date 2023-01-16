using UILibrary;
using UILibrary.IO;
using AssetStreamer;
using UILibrary.Scenes;
using UILibrary.Buttons;
using TowerDefencePackets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Scenes
{
    internal sealed class GetUsernameScene : Scene
    {
        private Label title;
        private Label usernameLabel;
        private Textbox usernameBox;
        private Label usernameAllowed;
        private ImageTextButton playButton;

        private string lastName;

        private Texture2D   tick;
        private Texture2D  cross;
        private Texture2D  tickD;
        private Texture2D crossD;

        private Vector2 textboxOffset;

        private Client client;
        private bool isNameAllowed = false;

        private void OnMainClick()
        {
            //If the play button is pressed, we need to ask the server is this name is allowed just to be super sure, as any amount of time may have passed since
            //  we last checked and the name may have been taken in that time
            int msgCount = client.MessageCount;
            Client.Instance.SendMessage($"{Header.REQUEST_USERNAME}{usernameBox.GetText().ToString().Trim()}"); //This will return our player id
            while (client.MessageCount == msgCount) { client.PollEvents(); } //Wait til we have a response

            //Now we have the response, read it
            string serverResponse = client.ReadLatestMessage();

            if (serverResponse is null || serverResponse == "-1")
            {
                lastName = ""; //Force an update for the label
                return;
            }

            client.PlayerID = long.Parse(serverResponse);
            Client.Instance.PlayerName = usernameBox.GetText().ToString().Trim();
            Console.WriteLine($"Player ID is {client.PlayerID}");
            SceneManager.Instance.LoadScene("findGame");
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.Draw(AssetContainer.ReadTexture("sMenu"), Vector2.Zero, Color.White);
            title.DrawWithShadow(spriteBatch);
            usernameLabel.DrawWithShadow(spriteBatch);
            usernameAllowed.DrawWithShadow(spriteBatch);

            int crossSize = usernameBox.GetBoundingBox.Height;

            //Make sure we draw the drop shadow first, as we want it on the bottom
            (isNameAllowed ? tickD : crossD).Draw(new(usernameBox.GetBoundingBox.X + usernameBox.GetBoundingBox.Width + crossSize / 2 - textboxOffset.X, 476), spriteBatch, Color.White);

            (isNameAllowed ? tick : cross).DrawWithShadow(new(usernameBox.GetBoundingBox.X + usernameBox.GetBoundingBox.Width + crossSize / 2 - textboxOffset.X, 468), spriteBatch, 150, 3, Color.Black, Color.White);
        }

        public override void DrawGUI(SpriteBatch spriteBatch, GameTime gameTime)
        {

        }

        public override void LoadContent()
        {
            lastName = "";
            TowerDefence.Scenes.Game.IsDebugPlay = false;

            Console.WriteLine("LOAD GetUsernameScene");

            tick =        AssetContainer.ReadTexture("sTick");
            cross =      AssetContainer.ReadTexture("sCross");
            crossD = AssetContainer.ReadTexture("sCrossDrop");
            tickD =   AssetContainer.ReadTexture("sTickDrop");

            Vector2 pos = new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 50);
            title = new(AssetContainer.ReadString("STR_GAME_NAME"), 2f, pos, Color.White, AssetContainer.GetFont("fMain"), Origin.MIDDLE_CENTRE, 0f);
            usernameBox = new(new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 468), AssetContainer.GetFont("fMain"), AssetContainer.ReadTexture("sTextboxBkg"), 20, 1f, Origin.TOP_CENTRE);
            usernameBox.SetFocus(true);
            usernameBox.SetActive(true);

            pos.Y = 468;
            usernameLabel = new(AssetContainer.ReadString("LBL_USERNAME"), 1f, pos, Color.White, AssetContainer.GetFont("fMain"), Origin.BOTTOM_CENTRE, 0f);
            
            pos.Y = usernameBox.GetBoundingBox.Y + usernameBox.GetBoundingBox.Height + 32;
            usernameAllowed = new(string.Empty, 1f, pos, Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_CENTRE, 0f);

            short bWidth = 310;
            short bHeight = 72;
            short bX = (short)(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2 - bWidth / 2);
            short bY = (short)(SceneManager.Instance.graphics.PreferredBackBufferHeight * 0.82);
            playButton = new(new(bX, bY, bWidth, bHeight), AssetContainer.ReadTexture("sMenuButtonUnclicked"), AssetContainer.ReadTexture("sMenuButtonClicked"), AssetContainer.ReadString("STR_BTN_FIND"), 1f, AssetContainer.GetFont("fMain"));

            uIManager.Add(usernameBox);
            uIManager.Add(playButton);

            client = new();
            client.Connect();
            Thread.Sleep(500);

            textboxOffset = usernameBox.CalculateOriginOffset();
        }

        public override void UnloadContent()
        {
            Console.WriteLine("UNLOAD GetUsernameScene");
            UIManager.Clear();
        }

        public override void Update(GameTime gameTime)
        {
            client.PollEvents();

            //Before we need to check the server, we should check if the name is valid
            if (usernameBox.GetText().ToString().Trim().Length < 3)
            {
                isNameAllowed = false;
                lastName = usernameBox.GetText().ToString();
            }
            else
            {
                //Ping server and check if name is allowed
                if (!client.IsConnected) throw new("Not connected to server");

                //If the user hasn't changed their username, just skip this check
                if (usernameBox.GetText().ToString().Trim() != lastName)
                {
                    lastName = usernameBox.GetText().ToString().Trim();

                    int msgCount = client.MessageCount;
                    Client.Instance.SendMessage($"{Header.REQUEST_USERNAME_AVAILABILITY}{usernameBox.GetText().ToString().Trim()}");
                    while (client.MessageCount == msgCount) { client.PollEvents(); } //Wait til we have a response

                    //Now we have the response, read it
                    string serverResponse = client.ReadLatestMessage();

                    if (serverResponse is null || serverResponse != "ACK") isNameAllowed = false;
                    else isNameAllowed = true;
                }
            }

            string labelName = isNameAllowed ? "LBL_USERNAME_AVAIL" : "LBL_USERNAME_TAKEN";
            usernameAllowed.SetLabelText(AssetContainer.ReadString(labelName).Replace("{}", $"{usernameBox.GetText()}"));

            if ((playButton.IsClicked() || usernameBox.IsEntered) && isNameAllowed) OnMainClick();
        }
    }
}