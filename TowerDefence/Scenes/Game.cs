using UILibrary;
using UILibrary.IO;
using AssetStreamer;
using UILibrary.Scenes;
using UILibrary.Buttons;
using TowerDefencePackets;
using Microsoft.Xna.Framework;
using TowerDefence.CheatEngine;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Scenes
{
    internal sealed class Game : Scene
    {
        private readonly Random rng;

        private bool isWinner = false;
        private float gameOverOpacity = 0f;
        private const float GameOverOpacitySpeed = 0.01f;

        private const int TileSize = 16;
        private readonly Vector2 GameSize = new(48, 42);

        private UIManager pausedMenuUILayer;

        private Texture2D towerSelUnclick;
        private Texture2D towerSelClick;

        private SwitchArray towers;

        private ImageTextButton resumeButton;
        private ImageTextButton disconnectButton;

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
        private byte[,] textureOffsets;

        private Vector2 playFieldOffset;
        private Vector2 enemyPlayFieldOffset;

        private Label gameOverLabel;
        private ImageTextButton gameOverExit;

        private int PlayfieldWidth => (int)(playfieldTextures[0][0].Width * GameSize.X);
        private int PlayfieldHeight => (int)(playfieldTextures[0][0].Height * GameSize.Y);

        private TextureCollection[] playfieldTextures;

        private Texture2D bkg;
        private bool placementIsOverplayfield;

        private void ProcessStateMachine()
        {
            switch (gameState)
            {
                case GameState.PLACEMENT:
                    placementIsOverplayfield = IsCursorOnPlayField();
                    break;
                case GameState.MENU:
                    pausedMenuUILayer.Update();

                    //Check if buttons pressed
                    if (resumeButton.IsClicked()) gameState = GameState.PLAY;
                    if (disconnectButton.IsClicked()) Disconnect();
                    break;
                case GameState.END:
                    gameOverOpacity += GameOverOpacitySpeed;
                    gameOverOpacity = Math.Clamp(gameOverOpacity, 0f, 1f);
                    gameOverLabel.SetOpacity(gameOverOpacity);
                    gameOverExit.Update();

                    if (gameOverExit.IsClicked())
                    {
                        Client.Instance.Disconnect();
                        SceneManager.Instance.LoadScene("mainMenu");
                    }
                    break;
            }
        }

        private void HandleServer()
        {
            Client.Instance.PollEvents();

            //If the server has sent nothing, there is no point in doing anything
            if (Client.Instance.MessageCount == 0) return;

            while (Client.Instance.MessageCount > 0)
            {
                ProcessServerMessage(Client.Instance.ReadLatestMessage());
                Client.Instance.PollEvents();
            }
        }

        private void ProcessServerMessage(string message)
        {
            //Read the header of the server and deal with it
            byte header = (byte)message[0];
            message = message[1..];

            switch (header)
            {
                case (byte)Header.GAME_OVER:
                    gameState = GameState.END;
                    if ((byte)message[0] > 0) isWinner = true;
                    gameOverLabel.SetLabelText(AssetContainer.ReadString(isWinner ? "GM_END_WIN" : "GM_END_LOSE"));
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Disconnect this client from the game
        /// </summary>
        private static void Disconnect()
        {
            //Tell the server that the other player wins
            Client.Instance.SendMessage($"{Header.GAME_OVER}{Client.Instance.EnemyID}");
            Thread.Sleep(30);
            Client.Instance.Disconnect();
            SceneManager.Instance.LoadScene("mainMenu");
        }

        /// <summary>
        /// Check if the mouse cursor is over the left side play area
        /// </summary>
        /// <returns>True if it is</returns>
        private bool IsCursorOnPlayField()
        {
            //Convert the play field to an AABB
            AABB playfield;

            short x = (short)(playFieldOffset.X);
            short y = (short)(playFieldOffset.Y);
            short width = (short)PlayfieldWidth;
            short height = (short)PlayfieldHeight;

            playfield = new(x, y, width, height);

            AABB mouse = (AABB)MouseController.GetMousePosition();

            return playfield.CollisionCheck(mouse);
        }

        /// <summary>
        /// Check whether a tower button has been clicked
        /// </summary>
        private void CheckForBuildMode()
        {
            if (towers.GetActiveIndex() != -1)
            {
                gameState = GameState.PLACEMENT;
            }
            else if (gameState == GameState.PLACEMENT) gameState = GameState.PLAY; //If we are in the placement state and we deselect a tower, we need to go back into the game mode
        }

        /// <summary>
        /// Draw all of the play areas onto the screen
        /// </summary>
        /// <param name="spriteBatch">The sprite batch to draw with</param>
        private void DrawPlayField(SpriteBatch spriteBatch)
        {
            void DrawNonSpecificField(byte[,] field, Vector2 thisFieldOffset)
            {
                Rectangle border = new((int)(thisFieldOffset.X - 5), (int)(thisFieldOffset.Y - 5), PlayfieldWidth + 10, PlayfieldHeight + 10);
                spriteBatch.Draw(playfieldTextures[0][0], border, Color.Black);

                for (int y = 0; y < GameSize.Y; y++)
                {
                    for (int x = 0; x < GameSize.X; x++)
                    {
                        Texture2D cell = playfieldTextures[field[y, x]][textureOffsets[y, x]];
                        Vector2 v = new(thisFieldOffset.X + x * cell.Width, thisFieldOffset.Y + y * cell.Height);
                        cell.Draw(v, spriteBatch, Color.White);
                    }
                }
            }

            DrawNonSpecificField(playfield, playFieldOffset);
            DrawNonSpecificField(oppPlayfield, enemyPlayFieldOffset);
        }

        /// <summary>
        /// Make the cheat panel visible again
        /// </summary>
        private void ResetCheatPanel()
        {
            cheatPanel = new(Vector2.Zero, AssetContainer.GetFont("fMain"), AssetContainer.ReadTexture("sTextboxBkg"), 40, 0.7f, Origin.TOP_LEFT);
            cheatPanel.SetActive(true);
            cheatPanel.SetFocus(true);
        }

        /// <summary>
        /// Process a <see cref="Cheat"/> object
        /// </summary>
        /// <param name="cheat">The cheat struct to execute</param>
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
            else if (cheat.cmd == CheatCommand.EXIT)
            {
                Disconnect();
                SceneManager.Instance.Exit();
            }
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
            else if (cheat.cmd == CheatCommand.FORCE_WIN)
            {
                Client.Instance.SendMessage($"{Header.GAME_OVER}{Client.Instance.PlayerID}");
            }
            else if (cheat.cmd == CheatCommand.FORCE_LOSE)
            {
                Client.Instance.SendMessage($"{Header.GAME_OVER}{Client.Instance.EnemyID}");
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            bkg.Draw(Vector2.Zero, spriteBatch, Color.White);

            DrawPlayField(spriteBatch);
        }

        public override void DrawGUI(SpriteBatch spriteBatch, GameTime gameTime)
        {
            //Draw the divider over the centre of the screen
            int cX = SceneManager.Instance.graphics.PreferredBackBufferWidth / 2;
            for (int y = 0; y < SceneManager.Instance.graphics.PreferredBackBufferHeight; y += divider.Height)
            {
                spriteBatch.Draw(divider, new Vector2(cX - divider.Width / 2, y), Color.White);
            }

            //Draw the stat panel on the top right of the screen
            spriteBatch.Draw(statPanel, new Vector2(SceneManager.Instance.graphics.PreferredBackBufferWidth - statPanel.Width, 0), Color.White);
            money.DrawWithShadow(spriteBatch);
            health.DrawWithShadow(spriteBatch);

            //Draw addition UI elements
            if (gameState == GameState.PLACEMENT && placementIsOverplayfield)
            {
                //Check if the position that the mouse is over, is a ground tile
                int pX = (int)((tmx - playFieldOffset.X) / TileSize);
                int pY = (int)((tmy - playFieldOffset.Y) / TileSize);

                if (playfield[pY, pX] == 0)
                {
                    spriteBatch.Draw(statPanel, new Rectangle(tmx, tmy, TileSize, TileSize), Color.White * 0.6f);
                }
                else
                {
                    spriteBatch.Draw(statPanel, new Rectangle(tmx, tmy, TileSize, TileSize), Color.Red * 0.6f);
                }
            }
            if (showCheatPanel) cheatPanel.Draw(spriteBatch);

            //Draw the username of both players
            username.DrawWithShadow(spriteBatch);
            otherUsername.DrawWithShadow(spriteBatch);

            UIManager.Draw(spriteBatch);

            //If the game is in the menu state, we need to draw an overlay over everything else
            if (gameState == GameState.MENU)
            {
                spriteBatch.Draw(menuFilter, new Rectangle(0, 0, SceneManager.Instance.graphics.PreferredBackBufferWidth, SceneManager.Instance.graphics.PreferredBackBufferHeight), Color.White);

                //If we are in the paused menu, we want to draw the paused buttons
                pausedMenuUILayer.Draw(spriteBatch);
            }

            if (gameState == GameState.END)
            {
                gameOverLabel.DrawWithShadow(spriteBatch);
                gameOverExit.Draw(spriteBatch);
            }
        }

        public override void LoadContent()
        {
            void LoadTextures()
            {
                bkg = AssetContainer.ReadTexture("sMenu");
                divider = AssetContainer.ReadTexture("sBorder");
                towerSelUnclick = AssetContainer.ReadTexture("sTowerSelUnclick");
                towerSelClick = AssetContainer.ReadTexture("sTowerSelClick");
                menuFilter = AssetContainer.ReadTexture("sMenuFilter");
                statPanel = AssetContainer.ReadTexture("sStat");

                playfieldTextures = new TextureCollection[2];
                for (int i = 0; i < playfieldTextures.Length; i++)
                {
                    playfieldTextures[i] = new();
                    playfieldTextures[i].AddTexture(AssetContainer.ReadTexture($"map_{i}"));

                    //Add the alt textures
                    //There is probably a better way than hard-coding but fuck it
                    if (i == 0)
                    {
                        for (int j = 0; j < 2; j++)
                        {
                            playfieldTextures[i].AddTexture(AssetContainer.ReadTexture($"map_{i}_{j}"));
                        }
                    }
                }
            }

            void InitUI()
            {
                for (int i = 0; i < 10; i++)
                {
                    AABB aabb = new((short)(SceneManager.Instance.graphics.PreferredBackBufferWidth - towerSelClick.Width), (short)((towerSelClick.Height * 3) + towerSelClick.Height * i), (short)(towerSelClick.Width), (short)(towerSelClick.Height));
                    Switch s = new(aabb, towerSelUnclick, towerSelClick, false);
                    towers.AddSwitch(s);

                    Vector2 topRight = new(SceneManager.Instance.graphics.PreferredBackBufferWidth, 0);

                    money = new("NULL", 1.3f, topRight, Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_RIGHT, 0f);
                    health = new("NULL", 1.3f, topRight + new Vector2(0, 48), Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_RIGHT, 0f);
                }

                gameOverLabel = new("", 4f, new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 100), Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_CENTRE, 0f);
            }

            void InitPlayerField()
            {
                playfield = new byte[(int)GameSize.Y, (int)GameSize.X];
                for (int y = 0; y < GameSize.Y; y++) for (int x = 0; x < GameSize.X; x++) playfield[y, x] = 0;

                oppPlayfield = new byte[(int)GameSize.Y, (int)GameSize.X];
                for (int y = 0; y < GameSize.Y; y++) for (int x = 0; x < GameSize.X; x++) oppPlayfield[y, x] = 0;
            }

            void LoadMap()
            {
                //Wait til we get the correct message
                while (Client.Instance.PeekLatest is null || Client.Instance.PeekLatest[0] != Header.RECEIVE_MAP_DATA) Client.Instance.PollEvents();

                string mapData = Client.Instance.ReadLatestMessage()[1..];

                if (mapData.Length != GameSize.X * GameSize.Y) throw new("Invalid map data read from server");

                int i = 0;
                for (int y = 0; y < GameSize.Y; y++)
                {
                    for (int x = 0; x < GameSize.X; x++)
                    {
                        playfield[y, x] = byte.Parse(mapData[i++].ToString());
                        oppPlayfield[y, x] = playfield[y, x];
                    }
                }

                //Now we can load the random offsets
                textureOffsets = new byte[(int)GameSize.Y, (int)GameSize.X];
                for (int y = 0; y < GameSize.Y; y++)
                {
                    for (int x = 0; x < GameSize.X; x++)
                    {
                        if (playfield[y, x] == 0)
                        {
                            textureOffsets[y, x] = (byte)rng.Next(playfieldTextures[0].Count);
                        }
                    }
                }
            }

            void SetUpUsernames()
            {
                Vector2 centre = new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 0);
                Vector2 leftQ = new(centre.X / 2, 0);
                Vector2 rghtQ = new(centre.X + (centre.X / 2), 0);
                username = new(Client.Instance.PlayerName, 1.1f, rghtQ, Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_LEFT, 0f);

                Client.Instance.SendMessage($"{Header.REQUEST_USERNAME_FROM_ID}{Client.Instance.EnemyID}");
                Client.Instance.WaitForNewMessage();
                string enemyUsername = Client.Instance.ReadLatestMessage();
                otherUsername = new(enemyUsername, 1.1f, leftQ, Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_LEFT, 0f);
            }

            Console.WriteLine("LOAD Game");
            pausedMenuUILayer = new();
            towers = new();
            showCheatPanel = false;
            cheatPanel = null;
            vHealth = 100;
            vMoney = 1000;
            gameState = GameState.PLAY;
            SceneManager.Instance.ManagedUIManager = false; //We need to draw a layer over the UI when paused, so we need to take full control

            LoadTextures();
            InitUI();
            InitPlayerField();
            LoadMap();
            SetUpUsernames();

            uIManager.Add(towers);

            playFieldOffset = new(1920 / 2 + divider.Width, 96);
            enemyPlayFieldOffset = new(1920 / 2 - PlayfieldWidth - divider.Width, 96);

            short bWidth = 310;
            short bHeight = 72;
            short bX = (short)(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2 - bWidth / 2);
            short bY = (short)(SceneManager.Instance.graphics.PreferredBackBufferHeight * 0.82);
            resumeButton = new(new(bX, (short)(bY - bHeight), bWidth, bHeight), AssetContainer.ReadTexture("sMenuButtonUnclicked"), AssetContainer.ReadTexture("sMenuButtonClicked"), AssetContainer.ReadString("GM_MENU_PLAY"), 1.4f, AssetContainer.GetFont("fMain"));
            disconnectButton = new(new(bX, bY, bWidth, bHeight), AssetContainer.ReadTexture("sMenuButtonUnclicked"), AssetContainer.ReadTexture("sMenuButtonClicked"), AssetContainer.ReadString("GM_MENU_EXIT"), 1.4f, AssetContainer.GetFont("fMain"));
            gameOverExit = new(new(bX, (short)(bY - bHeight), bWidth, bHeight), AssetContainer.ReadTexture("sMenuButtonUnclicked"), AssetContainer.ReadTexture("sMenuButtonClicked"), AssetContainer.ReadString("GM_MENU_EXIT"), 1.4f, AssetContainer.GetFont("fMain"));

            pausedMenuUILayer.Add(resumeButton);
            pausedMenuUILayer.Add(disconnectButton);
        }

        public override void UnloadContent()
        {
            Console.WriteLine($"UNLOAD Game");
            SceneManager.Instance.ManagedUIManager = true; //Pass control back to the SceneManager
        }

        public override void Update(GameTime gameTime)
        {
            HandleServer();

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

            ProcessStateMachine();

            if (gameState is not GameState.MENU and not GameState.END) UIManager.Update();
            CheckForBuildMode();
        }

        public Game() : base()
        {
            rng = new();
        }
    }
}