using UILibrary;
using System.Text;
using UILibrary.IO;
using AssetStreamer;
using UILibrary.Scenes;
using UILibrary.Buttons;
using TowerDefencePackets;
using TowerDefence.Visuals;
using TowerDefence.Entities;
using TowerDefence.Settings;
using Microsoft.Xna.Framework;
using TowerDefence.CheatEngine;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TowerDefence.Entities.GameObjects;
using TowerDefence.Entities.GameObjects.Towers;

using TextureCollection = TowerDefence.Visuals.TextureCollection;

namespace TowerDefence.Scenes
{
    internal sealed class Game : Scene
    {
        /// <summary>
        /// Is the game running in debug mode
        /// </summary>
        public static bool IsDebugPlay { get; set; } = false;

        private readonly Random rng;

        private bool isWinner = false;
        private float gameOverOpacity;
        private const float GameOverOpacitySpeed = 0.01f;

        private const int TileSize = 16;
        private readonly Vector2 GameSize = new(48, 42);

        private UIManager pausedMenuUILayer;

        private Texture2D towerSelUnclick;
        private Texture2D towerSelClick;

        private const int TowerCount = 10;
        private SwitchArray towers;
        private readonly string[] towerNames = new string[TowerCount] { "Debug Tower", "", "", "", "", "", "", "", "", "" };

        private ImageTextButton resumeButton;
        private ImageTextButton disconnectButton;

        private Texture2D divider;
        private Texture2D menuFilter;
        private Texture2D statPanel;

        private Label username;
        private Label otherUsername;

        private Label selectedTower;

        private bool showDebugStats;

        private int tmx;
        private int tmy;

        private Label money;
        private Label health;

        private byte vHealth;
        private ushort vMoney;

        private byte ovHealth;
        private ushort ovMoney;
        private Label oMoney;
        private Label oHealth;

        private Textbox cheatPanel;
        private bool showCheatPanel;

        private Switch sellButton;

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

        private Animation waterAnimation;
        private TextureCollection[] playfieldTextures;

        private Texture2D bkg;
        private bool placementIsOverplayfield;

        private List<Entity> entities;

        private List<ServerTowerData> enemyTowers;

        private Texture2D CreateCircleText(int diameter, Color colour)
        {
            Texture2D texture = new(GraphicsDevice, diameter, diameter);
            Color[] colorData = new Color[diameter * diameter];

            float radius = diameter / 2f;
            float radiusSq = radius * radius;

            for (int x = 0; x < diameter; x++)
            {
                for (int y = 0; y < diameter; y++)
                {
                    int index = x * diameter + y;
                    Vector2 pos = new(x - radius, y - radius);
                    if (pos.LengthSquared() <= radiusSq)
                    {
                        colorData[index] = colour;
                    }
                    else
                    {
                        colorData[index] = Color.Transparent;
                    }
                }
            }

            texture.SetData(colorData);
            return texture;
        }

        /// <summary>
        /// Update the game based on the current state
        /// </summary>
        private void ProcessStateMachine()
        {
            if (gameState is GameState.PLAY or GameState.PLACEMENT or GameState.SELL && !showCheatPanel)
            {
                //Use keyboard to select tower
                const int MONOGAME_KEY_OFFSET = 48;
                for (int i = 0; i < 10; i++)
                {
                    Keys keyToCheck = (Keys)MONOGAME_KEY_OFFSET + i;

                    //If this key is down, we need to toggle the state the of the switch
                    if (KeyboardController.IsPressed(keyToCheck))
                    {
                        if (gameState == GameState.SELL)
                        {
                            gameState = GameState.PLACEMENT;
                            sellButton.SetState(false);
                        }

                        //MonoGame key-codes encodes 0 as first, so we need to do a quick check if we are on index 0
                        int offset = i == 0 ? 9 : i - 1;

                        //If the tower is selected already, then just deselect it
                        towers.Clear();

                        if (!towers[offset].State)
                        {
                            towers.SetActiveIndex(offset);
                        }
                        else
                        {
                            gameState = GameState.PLAY;
                        }
                    }
                }
            }

            switch (gameState)
            {
                case GameState.PLACEMENT:
                case GameState.SELL:
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

        /// <summary>
        /// Send a snapshot of the game to the other player
        /// </summary>
        private void SendSnapShot()
        {
            //If we are in the end state, we don't need to send anything
            if (gameState == GameState.END) return;

            Snapshot ss = new()
            {
                ID = Client.Instance.PlayerID,
                Health = vHealth,
                Money = vMoney,
            };

            StringBuilder builder = new();
            builder.Append($"{Header.SNAPSHOT}{ss.Serialize()}");

            foreach (Entity e in entities)
            {
                if (e is Tower)
                {
                    Tower t = e as Tower;
                    builder.Append(t.Serialise());
                }
            }

            Client.Instance.SendMessage(builder.ToString());
        }

        /// <summary>
        /// Handle all messages from the server
        /// </summary>
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

        /// <summary>
        /// Process a specific message from the server
        /// </summary>
        /// <param name="message">The message to process</param>
        private void ProcessServerMessage(string message)
        {
            //Read the header of the server and deal with it
            byte header = (byte)message[0];
            message = message[1..];

            switch (header)
            {
                case (byte)Header.GAME_OVER:
                    gameState = GameState.END;
                    if (byte.Parse(message[0].ToString()) > 0) isWinner = true;
                    gameOverLabel.SetLabelText(AssetContainer.ReadString(isWinner ? "GM_END_WIN" : "GM_END_LOSE"));
                    break;
                case (byte)Header.SNAPSHOT:
                    {
                        Snapshot ss = new();
                        ss.Deserialize(message);
                        ovHealth = ss.Health;
                        ovMoney = ss.Money;

                        int offset = 27;
                        if (message.Length <= offset) break;
                        message = message[27..^1];

                        enemyTowers.Clear();

                        //Get the data to parse
                        string[] entities = message.Split('|');
                        foreach (string data in entities)
                        {
                            string[] csvData = data.Split(',');

                            if (csvData[0] == "0")
                            {
                                if (!int.TryParse(csvData[1], out int x)) x = 0;
                                if (!int.TryParse(csvData[2], out int y)) y = 0;
                                if (!int.TryParse(csvData[3], out int rot)) rot = 0;
                                if (!int.TryParse(csvData[4], out int id)) id = 0;
                                enemyTowers.Add(new() { x = x, y = y, rot = rot, id = id });
                            }
                        }
                    }
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
                sellButton.SetState(false);
            }
            else if (gameState == GameState.PLACEMENT)
            {
                gameState = GameState.PLAY; //If we are in the placement state and we deselect a tower, we need to go back into the game mode
                sellButton.SetState(false);
            }
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

                        if (field[y, x] == 16)
                        {
                            cell = waterAnimation.GetActiveFrame();
                        }

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
            else if (cheat.cmd == CheatCommand.DEBUG)
            {
                showDebugStats = !showDebugStats;
            }
        }

        /// <summary>
        /// Build a new tower if the conditions are met
        /// </summary>
        private void CheckForTowerPlacement()
        {
            if (gameState != GameState.PLACEMENT) return;

            //Check if any tower is held down, and if it is, place it and change state back to play
            if (towers.GetActiveIndex() == -1) return;

            //If the mouse is over the play-field and the user clicks, place the tower
            if (MouseController.IsPressed(MouseController.MouseButton.LEFT) && IsCursorOnPlayField())
            {
                int pX = (int)((tmx - playFieldOffset.X) / TileSize);
                int pY = (int)((tmy - playFieldOffset.Y) / TileSize);

                //Get all towers as we need to check if the current spot is free from them
                List<Tower> activeTowers = entities.OfType<Tower>().ToList();

                //Loop over each tower and if the current tile is occupied, do not place
                foreach (Tower tower in activeTowers)
                {
                    if (tower.GetPosition().X + playFieldOffset.X == tmx && tower.GetPosition().Y + playFieldOffset.Y == tmy)
                    {
                        Popup popup = new(new(MouseController.GetMousePosition().x, MouseController.GetMousePosition().y), AssetContainer.ReadString("POPUP_INV_LOC"), 1f, GlobalSettings.TextWarning, AssetContainer.GetFont("fMain"), 1.75f, new(0, -9f));
                        entities.Add(popup);
                        return;
                    }
                }

                if (vMoney < Tower.towerDatas[towerNames[towers.GetActiveIndex()]].cost)
                {
                    Popup popup = new(new(MouseController.GetMousePosition().x, MouseController.GetMousePosition().y), AssetContainer.ReadString("POPUP_POOR"), 1f, GlobalSettings.TextWarning, AssetContainer.GetFont("fMain"), 1.75f, new(0, -9f));
                    entities.Add(popup);
                }
                //Check that the cursor is at a valid position
                else if (playfield[pY, pX] != 0)
                {
                    Popup popup = new(new(MouseController.GetMousePosition().x, MouseController.GetMousePosition().y), AssetContainer.ReadString("POPUP_INV_LOC"), 1f, GlobalSettings.TextError, AssetContainer.GetFont("fMain"), 1.75f, new(0, -9f));
                    entities.Add(popup);
                }
                else //Tower placement is okay
                {
                    //Remove the money from the user
                    vMoney -= Tower.towerDatas[towerNames[towers.GetActiveIndex()]].cost;

                    //Create the tower object and add it to the entities
                    TextureCollection textures = new();
                    textures.AddTexture(AssetContainer.ReadTexture(Tower.towerDatas[towerNames[towers.GetActiveIndex()]].texIdle));
                    Animation anim = new(textures, 0);
                    Tower tower = new(towerNames[towers.GetActiveIndex()], new(tmx - playFieldOffset.X, tmy - playFieldOffset.Y), anim, playFieldOffset);
                    entities.Add(tower);

                    towers.Clear();
                    towers.Update();
                    gameState = GameState.PLAY;

                    Popup popup = new(new(MouseController.GetMousePosition().x, MouseController.GetMousePosition().y), $"+${tower.Data.cost}", 1f, GlobalSettings.TextError, AssetContainer.GetFont("fMain"), 1.75f, new(0, -9f));
                    entities.Add(popup);
                }
            }
        }

        /// <summary>
        /// Draw the enemy entities that this client has created
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch to draw with</param>
        private void DrawEnemyEntities(SpriteBatch spriteBatch)
        {
            foreach (ServerTowerData towerData in enemyTowers)
            {
                string towerName = towerNames[towerData.id];
                Texture2D texture = AssetContainer.ReadTexture(Tower.towerDatas[towerName].texIdle);

                Vector2 pos = new(towerData.x, towerData.y);
                texture.Draw(pos + enemyPlayFieldOffset, spriteBatch, Color.White);
            }
        }

        /// <summary>
        /// Draw tower icons onto the tower buttons
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch to draw with</param>
        private void DrawTowerPictures(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < TowerCount; i++)
            {
                string towerName = towerNames[i];
                if (towerName == string.Empty) continue;

                TowerData towerData = Tower.towerDatas[towerName];
                Texture2D texture = AssetContainer.ReadTexture(towerData.texButton);

                Switch swtch = towers[0];
                texture.Draw(new(swtch.AABB.X + ((swtch.AABB.Width - texture.Width) / 2), swtch.AABB.Y + (swtch.AABB.Height - texture.Height) / 2), spriteBatch, Color.White);
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            bkg.Draw(Vector2.Zero, spriteBatch, Color.White);

            DrawPlayField(spriteBatch);

            //Draw the divider over the centre of the screen
            int cX = SceneManager.Instance.graphics.PreferredBackBufferWidth / 2;
            for (int y = 0; y < SceneManager.Instance.graphics.PreferredBackBufferHeight; y += divider.Height)
            {
                spriteBatch.Draw(divider, new Vector2(cX - divider.Width / 2, y), Color.White);
            }

            UIManager.Draw(spriteBatch);
            foreach (Entity e in entities) if (e is not Popup) e.Draw(spriteBatch);
            DrawEnemyEntities(spriteBatch);
        }

        public override void DrawGUI(SpriteBatch spriteBatch, GameTime gameTime)
        {
            string x = $"State: {gameState}\nAct ID: {towers.GetActiveIndex()}";
            spriteBatch.DrawString(AssetContainer.GetFont("fMain"), x, new Vector2(0, 200), Color.White);

            money.SetColour(vMoney == 0 ? GlobalSettings.TextError : GlobalSettings.TextMain);
            oMoney.SetColour(ovMoney == 0 ? GlobalSettings.TextError : GlobalSettings.TextMain);

            //Draw the stat panel on the top right of the screen
            spriteBatch.Draw(statPanel, new Vector2(SceneManager.Instance.graphics.PreferredBackBufferWidth - statPanel.Width, 0), Color.White);
            spriteBatch.Draw(statPanel, new Rectangle(0, 0, statPanel.Width, statPanel.Height), null, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);
            money.DrawWithShadow(spriteBatch);
            health.DrawWithShadow(spriteBatch);
            oMoney.DrawWithShadow(spriteBatch);
            oHealth.DrawWithShadow(spriteBatch);

            //Draw addition UI elements
            if ((gameState is GameState.PLACEMENT or GameState.SELL) && placementIsOverplayfield)
            {
                //Check if the position that the mouse is over, is a ground tile
                int pX = (int)((tmx - playFieldOffset.X) / TileSize);
                int pY = (int)((tmy - playFieldOffset.Y) / TileSize);

                //Get all towers as we need to check if the current spot is free from them
                List<Tower> activeTowers = entities.OfType<Tower>().ToList();

                if (
                    (pX >= 0 && pX < 48) &&
                    (pY >= 0 && pY < 42)
                    )
                {
                    bool canPlace = true;

                    //Loop over each tower and if the current tile is occupied, do not place
                    if (playfield[pY, pX] == 0)
                    {
                        bool foundTower = false;

                        foreach (Tower tower in activeTowers)
                        {
                            if (tower.GetPosition().X + playFieldOffset.X == tmx && tower.GetPosition().Y + playFieldOffset.Y == tmy)
                            {
                                canPlace = false;
                                Color col = gameState == GameState.SELL ? Color.White : Color.Red;

                                spriteBatch.Draw(statPanel, new Rectangle(tmx, tmy, TileSize, TileSize), col * 0.6f);
                                foundTower = true;
                                break;
                            }
                        }

                        if (!foundTower)
                        {
                            Color col = gameState == GameState.SELL ? Color.Red : Color.White;
                            spriteBatch.Draw(statPanel, new Rectangle(tmx, tmy, TileSize, TileSize), col * 0.6f);
                        }
                    }
                    else
                    {
                        canPlace = false;
                        Color col = gameState == GameState.SELL ? Color.White : Color.Red;
                        spriteBatch.Draw(statPanel, new Rectangle(tmx, tmy, TileSize, TileSize), col * 0.6f);
                    }

                    //If we are placing a tower, draw the range
                    if (gameState == GameState.PLACEMENT)
                    {
                        TowerData data = Tower.towerDatas[towerNames[towers.GetActiveIndex()]];

                        Texture2D range = CreateCircleText(data.range, canPlace ? Color.White : Color.Red);
                        range.Draw(new Vector2(tmx - range.Width / 2 + TileSize / 2, tmy - range.Height / 2 + TileSize / 2), spriteBatch, Color.White * 0.2f);
                    }
                }
            }
            if (showCheatPanel) cheatPanel.Draw(spriteBatch);

            //Draw the username of both players
            username.DrawWithShadow(spriteBatch);
            otherUsername.DrawWithShadow(spriteBatch);

            DrawTowerPictures(spriteBatch);

            //If the game is in the menu state, we need to draw an overlay over everything else
            if (gameState == GameState.MENU)
            {
                spriteBatch.Draw(menuFilter, new Rectangle(0, 0, SceneManager.Instance.graphics.PreferredBackBufferWidth, SceneManager.Instance.graphics.PreferredBackBufferHeight), Color.White);

                //If we are in the paused menu, we want to draw the paused buttons
                pausedMenuUILayer.Draw(spriteBatch);
            }

            if (gameState == GameState.PLACEMENT)
            {
                selectedTower.DrawWithShadow(spriteBatch);
            }

            if (gameState == GameState.END)
            {
                gameOverLabel.DrawWithShadow(spriteBatch);
                gameOverExit.Draw(spriteBatch);
            }

            if (showDebugStats)
            {
                string message = $"PING: {Client.Instance.Peer.Ping}ms";
                spriteBatch.DrawString(AssetContainer.GetFont("fMain"), message, new Vector2(100, 100), Color.White);
            }

            foreach (Popup popup in entities.OfType<Popup>()) popup.Draw(spriteBatch);
        }

        public override void LoadContent()
        {
            Console.WriteLine("LOAD Game");

            void LoadTextures()
            {
                bkg = AssetContainer.ReadTexture("sMenu");
                divider = AssetContainer.ReadTexture("sBorder");
                towerSelUnclick = AssetContainer.ReadTexture("sTowerSelUnclick");
                towerSelClick = AssetContainer.ReadTexture("sTowerSelClick");
                menuFilter = AssetContainer.ReadTexture("sMenuFilter");
                statPanel = AssetContainer.ReadTexture("sStat");

                playfieldTextures = new TextureCollection[29];
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
                    else if (i == 16)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            playfieldTextures[i].AddTexture(AssetContainer.ReadTexture($"map_{i}_{j}"));
                        }
                    }
                }
                waterAnimation = new(playfieldTextures[16], 1, AnimationPlayType.LOOP);
            }

            void InitUI()
            {
                for (int i = 0; i < TowerCount; i++)
                {
                    AABB aabb = new((short)(SceneManager.Instance.graphics.PreferredBackBufferWidth - towerSelClick.Width), (short)((towerSelClick.Height * 3) + towerSelClick.Height * i), (short)(towerSelClick.Width), (short)(towerSelClick.Height));
                    Switch s = new(aabb, towerSelUnclick, towerSelClick, false);
                    towers.AddSwitch(s);
                }

                Vector2 topRight = new(SceneManager.Instance.graphics.PreferredBackBufferWidth, 0);

                money = new("NULL", 1.3f, topRight, Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_RIGHT, 0f);
                health = new("NULL", 1.3f, topRight + new Vector2(0, 48), Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_RIGHT, 0f);
                gameOverLabel = new("", 4f, new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 100), Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_CENTRE, 0f);

                oMoney = new("NULL", 1.3f, Vector2.Zero, Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_LEFT, 0f);
                oHealth = new("NULL", 1.3f, new Vector2(0, 48), Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_LEFT, 0f);

                selectedTower = new("NULL", 1.6f, new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 0), Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_CENTRE, 0f);

                sellButton = new(new((short)(SceneManager.Instance.graphics.PreferredBackBufferWidth - towerSelClick.Width), (short)(towerSelClick.Height * 3 + towerSelClick.Height * TowerCount), 64, 32), AssetContainer.ReadTexture("sSellUnlick"), AssetContainer.ReadTexture("sSellClick"));
                uIManager.Add(sellButton);
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
                if (!IsDebugPlay)
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
                            playfield[y, x] = (byte)(mapData[i++] - 1);
                            oppPlayfield[y, x] = playfield[y, x];
                        }
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

                if (!IsDebugPlay)
                {
                    Client.Instance.SendMessage($"{Header.REQUEST_USERNAME_FROM_ID}{Client.Instance.EnemyID}");
                    Client.Instance.WaitForNewMessage();
                    string enemyUsername = Client.Instance.ReadLatestMessage();
                    otherUsername = new(enemyUsername, 1.1f, leftQ, Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_LEFT, 0f);
                }
                else
                {
                    string enemyUsername = "null";
                    otherUsername = new(enemyUsername, 1.1f, leftQ, Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_LEFT, 0f);
                }
            }

            gameOverOpacity = 0f;
            entities = new();
            showDebugStats = false;
            pausedMenuUILayer = new();
            towers = new();
            showCheatPanel = false;
            cheatPanel = null;
            vHealth = 100;
            vMoney = 1000;
            gameState = GameState.PLAY;
            enemyTowers = new();

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
            SendSnapShot();
            CheckForTowerPlacement();
            CheckForBuildMode();

            if (sellButton.IsClicked())
            {
                gameState = GameState.SELL;
                towers.Clear();
            }
            else if (gameState == GameState.SELL && !sellButton.State)
            {
                gameState = GameState.PLAY;
            }
            
            if (MouseController.IsPressed(MouseController.MouseButton.LEFT) && gameState == GameState.SELL && placementIsOverplayfield)
            {
                //Check each tower and check if there is one here
                foreach (Tower t in entities.OfType<Tower>().ToList())
                {
                    if (t.GetPosition().X + playFieldOffset.X == tmx && t.GetPosition().Y + playFieldOffset.Y == tmy)
                    {
                        //Delete the tower, and refund the player
                        int refundAmount = (int)(t.Data.cost * 0.6f);
                        if (refundAmount == 0) refundAmount = 1;

                        //Add the funds, but make sure we do not overflow the total money count
                        if (vMoney + refundAmount > ushort.MaxValue) vMoney = ushort.MaxValue;
                        else vMoney += (ushort)refundAmount;

                        t.MarkForDeletion = true;

                        //Create a pop-up informing how much money the player made
                        Popup popup = new(new(tmx, tmy), $"+${refundAmount}", 1f, GlobalSettings.TextMain, AssetContainer.GetFont("fMain"), 1.75f, new(0, -9));
                        entities.Add(popup);
                    }
                }
            }

            string selectedTower = towers.GetActiveIndex() == -1 ? string.Empty : towerNames[towers.GetActiveIndex()];
            this.selectedTower.SetLabelText(selectedTower);

            foreach (Entity e in entities) e.Update(gameTime);
            entities.RemoveAll(e => e.MarkForDeletion);

            waterAnimation.Update(gameTime);

            //Sometimes integer division is good, and this is one of them cases
            //This will lock the cursor to a 32x32 grid
            //Floating point division in this case would not have the desired locking effect, and you'd probably have to do some modulus instead
            tmx = Mouse.GetState().X / TileSize * TileSize;
            tmy = Mouse.GetState().Y / TileSize * TileSize;

            money.SetLabelText($"{vMoney}$");
            health.SetLabelText($"{vHealth} HP");
            oMoney.SetLabelText($"{ovMoney}$");
            oHealth.SetLabelText($"{ovHealth} HP");

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
                Disconnect();
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
        }

        public Game() : base()
        {
            rng = new();
        }
    }
}