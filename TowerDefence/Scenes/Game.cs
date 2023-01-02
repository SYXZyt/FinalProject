using UILibrary;
using UILibrary.IO;
using AssetStreamer;
using UILibrary.Scenes;
using UILibrary.Buttons;
using Microsoft.Xna.Framework;
using TowerDefence.CheatEngine;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Scenes
{
    internal sealed class Game : Scene
    {
        private const int TileSize = 32;
        private const int GameSize = 18;

        private Texture2D towerSelUnclick;
        private Texture2D towerSelClick;

        private SwitchArray towers;

        private Texture2D divider;
        private Texture2D menuFilter;
        private Texture2D statPanel;

        private Label username;
        private Label otherUsername;

        private int tmx;
        private int tmy;

        private Label money;
        private Label health;

        private byte vHealth;
        private ushort vMoney;

        private Textbox cheatPanel;
        private bool showCheatPanel;

        private GameState gameState;

        private byte[,] playfield;
        private byte[,] oppPlayfield;

        private readonly Vector2 playFieldOffset = new(1920 / 2 + 32, 96);
        private Texture2D[] playfieldTextures;

        private Texture2D bkg;
        private bool placementIsOverplayfield;

        private bool IsCursorOnPlayField()
        {
            //Convert the playfield to an AABB
            AABB playfield;

            short x = (short)(playFieldOffset.X);
            short y = (short)(playFieldOffset.Y);
            short width = (short)(playfieldTextures[0].Width * GameSize);
            short height = (short)(playfieldTextures[0].Height * GameSize);

            playfield = new(x, y, width, height);

            AABB mouse = (AABB)MouseController.GetMousePosition();

            return playfield.CollisionCheck(mouse);
        }

        private void CheckForBuildMode()
        {
            if (towers.GetActiveIndex() != -1) gameState = GameState.PLACEMENT;
        }

        private void DrawPlayField(SpriteBatch spriteBatch)
        {
            Rectangle border = new((int)(playFieldOffset.X - 5), (int)(playFieldOffset.Y - 5), (playfieldTextures[0].Width * GameSize) + 10, (playfieldTextures[0].Height * GameSize) + 10);
            spriteBatch.Draw(playfieldTextures[0], border, Color.Black);

            for (int y = 0; y < GameSize; y++)
            {
                for (int x = 0; x < GameSize; x++)
                {
                    Texture2D cell = playfieldTextures[playfield[y, x]];
                    Vector2 v = new(playFieldOffset.X + x * cell.Width, playFieldOffset.Y + y * cell.Height);
                    cell.Draw(v, spriteBatch, Color.White);
                }
            }
        }

        private void ResetCheatPanel()
        {
            cheatPanel = new(Vector2.Zero, AssetContainer.GetFont("fMain"), AssetContainer.ReadTexture("sTextboxBkg"), 40, 0.7f);
            cheatPanel.SetActive(true);
            cheatPanel.SetFocus(true);
        }

        private void ParseCheat(Cheat cheat)
        {
            //If the cheat was invalid, we can just not do anything
            if (cheat.cmd == CheatCommand.INVALID) return;

            if (cheat.cmd == CheatCommand.SET_MONEY)
            {
                if (cheat.@params.Length == 0) return;

                Param newMoney = cheat.@params[0];

                if (newMoney.type != Param.Type.INT) return;

                int iMoney = (int)newMoney.value;
                iMoney = Math.Clamp(iMoney, 0, ushort.MaxValue);

                vMoney = Convert.ToUInt16(iMoney);
            }
            else if (cheat.cmd == CheatCommand.SET_HEALTH)
            {
                if (cheat.@params.Length == 0) return;

                Param newHealth = cheat.@params[0];

                if (newHealth.type != Param.Type.INT) return;

                int iHealth = (int)newHealth.value;
                iHealth = Math.Clamp(iHealth, 0, byte.MaxValue);

                vHealth = Convert.ToByte(iHealth);
            }
            else if (cheat.cmd == CheatCommand.EXIT) SceneManager.Instance.Exit();
            else if (cheat.cmd == CheatCommand.DISPOSE)
            {
                if (cheat.@params.Length == 0) return;

                foreach (Param p in cheat.@params)
                {
                    if (p.type != Param.Type.STR)
                    {
                        Console.WriteLine("ERROR Invalid cheat parameter");
                        continue;
                    }

                    string texName = p.value as string;
                    AssetContainer.ReadTexture(texName).Dispose();
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            bkg.Draw(Vector2.Zero, spriteBatch, Color.White);

            DrawPlayField(spriteBatch);

            int cX = SceneManager.Instance.graphics.PreferredBackBufferWidth / 2;
            for (int y = 0; y < SceneManager.Instance.graphics.PreferredBackBufferHeight; y += divider.Height)
            {
                spriteBatch.Draw(divider, new Vector2(cX - divider.Width / 2, y), Color.White);
            }

            spriteBatch.Draw(statPanel, new Vector2(SceneManager.Instance.graphics.PreferredBackBufferWidth - statPanel.Width, 0), Color.White);
            money.DrawWithShadow(spriteBatch);
            health.DrawWithShadow(spriteBatch);

            if (gameState == GameState.MENU)
            {
                spriteBatch.Draw(menuFilter, new Rectangle(0, 0, SceneManager.Instance.graphics.PreferredBackBufferWidth, SceneManager.Instance.graphics.PreferredBackBufferHeight), Color.White);
            }

            if (gameState == GameState.PLACEMENT && placementIsOverplayfield) spriteBatch.Draw(statPanel, new Rectangle(tmx, tmy, TileSize, TileSize), Color.White);
            if (showCheatPanel) cheatPanel.Draw(spriteBatch);
        }

        public override void LoadContent()
        {
            Console.WriteLine("LOAD Game");
            towers = new();
            bkg = AssetContainer.ReadTexture("sMenu");

            showCheatPanel = false;
            cheatPanel = null;

            divider = AssetContainer.ReadTexture("sBorder");
            towerSelUnclick = AssetContainer.ReadTexture("sTowerSelUnclick");
            towerSelClick = AssetContainer.ReadTexture("sTowerSelClick");
            menuFilter = AssetContainer.ReadTexture("sMenuFilter");
            statPanel = AssetContainer.ReadTexture("sStat");

            playfieldTextures = new Texture2D[2];
            playfieldTextures[0] = AssetContainer.ReadTexture("sFloor");
            playfieldTextures[0] = AssetContainer.ReadTexture("sPath");

            vHealth = 100;
            vMoney = 1000;
            gameState = GameState.PLAY;

            for (int i = 0; i < 10; i++)
            {
                AABB aabb = new((short)(SceneManager.Instance.graphics.PreferredBackBufferWidth - towerSelClick.Width), (short)((towerSelClick.Height * 3) + towerSelClick.Height * i), (short)(towerSelClick.Width), (short)(towerSelClick.Height));
                Switch s = new(aabb, towerSelUnclick, towerSelClick, false);
                towers.AddSwitch(s);
            }

            Vector2 topRight = new(SceneManager.Instance.graphics.PreferredBackBufferWidth, 0);

            money = new("NULL", 1.3f, topRight, Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_RIGHT, 0f);
            health = new("NULL", 1.3f, topRight + new Vector2(0, 48), Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_RIGHT, 0f);

            uIManager.Add(towers);

            playfield = new byte[GameSize, GameSize];
            for (int y = 0; y < GameSize; y++) for (int x = 0; x < GameSize; x++) playfield[y, x] = 0;

            oppPlayfield = new byte[GameSize, GameSize];
            for (int y = 0; y < GameSize; y++) for (int x = 0; x < GameSize; x++) oppPlayfield[y, x] = 0;
        }

        public override void UnloadContent()
        {
            Console.WriteLine($"UNLOAD Game");
        }

        public override void Update(GameTime gameTime)
        {
            //Sometimes integer division is good, and this is one of them cases
            //This will lock the cursor to a 32x32 grid
            //Floating point division in this case would not have the desired locking effect, and you'd probably have to do some modulus instead
            tmx = Mouse.GetState().X / TileSize * TileSize;
            tmy = Mouse.GetState().Y / TileSize * TileSize;

            money.SetLabelText($"{vMoney}$");
            health.SetLabelText($"{vHealth} HP");

            //If the user presses escape, we need to do something different
            if (KeyboardController.IsPressed(Keys.Escape))
            {
                switch (gameState)
                {
                    case GameState.PLAY:
                        gameState = GameState.MENU;
                        showCheatPanel = false;
                        break;
                    case GameState.PLACEMENT:
                        gameState = GameState.PLAY;
                        break;
                    case GameState.MENU:
                        gameState = GameState.PLAY;
                        break;
                    default:
                        break;
                }
            }
            CheckForBuildMode();

            if (KeyboardController.IsPressed(Keys.Home))
            {
                SceneManager.Instance.LoadScene("mainMenu");
            }

            if (KeyboardController.IsPressed(Keys.F12) && !showCheatPanel)
            {
                showCheatPanel = true;
                ResetCheatPanel();
            }

            if (showCheatPanel)
            {
                cheatPanel.Update();

                if (cheatPanel.IsEntered)
                {
                    ParseCheat(CheatEngineProcessor.ParseCheat(cheatPanel.GetText().ToString()));
                    showCheatPanel = false;
                    cheatPanel = null;
                }
            }

            if (gameState == GameState.PLACEMENT)
            {
                placementIsOverplayfield = IsCursorOnPlayField();
            }
        }

        public override void DrawGUI(SpriteBatch spriteBatch, GameTime gameTime)
        {

        }
    }
}