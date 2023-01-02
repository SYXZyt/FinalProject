using UILibrary;
using UILibrary.IO;
using AssetStreamer;
using UILibrary.Scenes;
using UILibrary.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Scenes
{
    internal sealed class GetUsernameScene : Scene
    {
        Label title;
        Label usernameLabel;
        Textbox usernameBox;
        Label usernameAllowed;
        ImageTextButton playButton;

        Texture2D   tick;
        Texture2D  cross;
        Texture2D  tickD;
        Texture2D crossD;

        bool isNameAllowed = false;

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.Draw(AssetContainer.ReadTexture("sMenu"), Vector2.Zero, Color.White);
            title.DrawWithShadow(spriteBatch);
            usernameLabel.DrawWithShadow(spriteBatch);
            usernameAllowed.DrawWithShadow(spriteBatch);

            int crossSize = usernameBox.GetBoundingBox.Height;

            //Make sure we draw the drop shadow first, as we want it on the bottom
            (isNameAllowed ? tickD : crossD).Draw(new(usernameBox.GetBoundingBox.X + usernameBox.GetBoundingBox.Width + crossSize / 2, 476), spriteBatch, Color.White);

            (isNameAllowed ? tick : cross).DrawWithShadow(new(usernameBox.GetBoundingBox.X + usernameBox.GetBoundingBox.Width + crossSize / 2, 468), spriteBatch, 150, 3, Color.Black, Color.White);
        }

        public override void LoadContent()
        {
            Console.WriteLine("LOAD GetUsernameScene");

            tick =        AssetContainer.ReadTexture("sTick");
            cross =      AssetContainer.ReadTexture("sCross");
            crossD = AssetContainer.ReadTexture("sCrossDrop");
            tickD =   AssetContainer.ReadTexture("sTickDrop");

            Vector2 pos = new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 50);
            title = new(AssetContainer.ReadString("STR_GAME_NAME"), 2f, pos, Color.White, AssetContainer.GetFont("fMain"), Origin.MIDDLE_CENTRE, 0f);
            usernameBox = new(new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2 - 150, 468), AssetContainer.GetFont("fMain"), AssetContainer.ReadTexture("sTextboxBkg"), 20, 1f);
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
        }

        public override void UnloadContent()
        {
            Console.WriteLine("UNLOAD GetUsernameScene");
        }

        public override void Update(GameTime gameTime)
        {
            //Before we need to check the server, we should check if the name is valid
            if (usernameBox.GetText().ToString().Length < 3)
            {
                isNameAllowed = false;
            }
            else
            {
                //Ping server and check if name is allowed
                isNameAllowed = true;
            }

            string labelName = isNameAllowed ? "LBL_USERNAME_AVAIL" : "LBL_USERNAME_TAKEN";
            usernameAllowed.SetLabelText(AssetContainer.ReadString(labelName).Replace("{}", $"{usernameBox.GetText()}"));

            if ((playButton.IsClicked() || usernameBox.IsEntered) && isNameAllowed) SceneManager.Instance.LoadScene("findGame");
        }
    }
}