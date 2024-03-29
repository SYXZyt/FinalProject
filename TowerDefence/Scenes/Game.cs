﻿using UILibrary;
using System.Text;
using UILibrary.IO;
using AssetStreamer;
using UILibrary.Scenes;
using UILibrary.Buttons;
using TowerDefence.Waves;
using TowerDefencePackets;
using TowerDefence.Visuals;
using TowerDefence.Entities;
using TowerDefence.Settings;
using Microsoft.Xna.Framework;
using TowerDefence.CheatEngine;
using Microsoft.Xna.Framework.Input;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework.Graphics;
using TowerDefence.Entities.GameObjects.Towers;
using TowerDefence.Entities.GameObjects.Enemies;

using TextureCollection = TowerDefence.Visuals.TextureCollection;

namespace TowerDefence.Scenes
{
    internal sealed class Game : Scene
    {
        #region Members
        private readonly Random rng;

        #region Nuke Data
        private float nukeFlashOpacity;
        private Texture2D nukeFlash;
        #endregion

        private Label[] towerCostLabels;

        private Texture2D rangeTexture;
        private int rangeTextureRad;
        private const int RangeTextureLimit = 2500;

        private readonly Stack<ushort> moneyMadeThisFrame = new();
        private readonly List<Vector2> enemySpawnPositions = new();

        private ulong tick = 0;
        private bool isWinner = false;
        private float gameOverOpacity;
        private const float GameOverOpacitySpeed = 0.01f;

        public const int TileSize = 16;
        private readonly Vector2 GameSize = new(48, 42);

        private UIManager pausedMenuUILayer;

        private Texture2D towerSelUnclick;
        private Texture2D towerSelClick;

        private const int TowerCount = 10;
        private SwitchArray towers;

        private ImageTextButton resumeButton;
        private ImageTextButton disconnectButton;

        private ImageTextButton readyButton;

        private Texture2D platformTexture;
        private Texture2D divider;
        private Texture2D menuFilter;
        private Texture2D statPanel;

        private Label username;
        private Label otherUsername;

        private Label selectedTower;

        private bool showDebugStats;

        private Wave currentWave;

        private int tmx;
        private int tmy;

        private Label money;
        private Label health;

        private byte vHealth;
        private ushort vMoney;

        private bool isDead;

        private bool isWaveActive;
        private bool ready;

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

        private Animation waterAnimation;
        private TextureCollection[] playfieldTextures;

        private Texture2D vignette;
        private float vignetteOpac;
        private const float vignetteSpeed = 1.0f;

        private Texture2D bkg;
        private bool placementIsOverplayfield;

        private List<Entity> entities;

        private List<Entity> enemyEntities;

        private readonly Stack<Entity> entityBuffer = new();

        public static Game Instance { get; private set; }

        #endregion

        #region Properties

        public float NukeFlashOpacity
        {
            get => nukeFlashOpacity;
            set => nukeFlashOpacity = value;
        }

        /// <summary>
        /// Get all active entities in the game
        /// </summary>
        public List<Entity> Entities => entities;

        public List<Entity> EnemyEntities => enemyEntities;

        /// <summary>
        /// How many pixels wide the play field is
        /// </summary>
        private int PlayfieldWidth => (int)(playfieldTextures[0][0].Width * GameSize.X);

        /// <summary>
        /// How many pixels tall the play field is
        /// </summary>
        private int PlayfieldHeight => (int)(playfieldTextures[0][0].Height * GameSize.Y);

        public Vector2 PlayerGameOffset => playFieldOffset;
        public Vector2 OpponentGameOffset => enemyPlayFieldOffset;

        public Random RNG => rng;

        /// <summary>
        /// Is the game running in debug mode
        /// </summary>
        public static bool IsDebugPlay { get; set; } = false;

        #endregion

        public void AddEntity(Entity entity) => entityBuffer.Push(entity);

        public void AddMoneyThisFrame(ushort amount) => moneyMadeThisFrame.Push(amount);

        public void DamagePlayer(int amount, bool isPlayer)
        {
            if (gameState == GameState.END) return;

            if (isPlayer)
            {
                vHealth = (byte)Math.Max(0, vHealth - amount);
                Client.Instance?.SendMessage($"{Header.TAKE_HEALTH}{Client.Instance.PlayerID},{amount}");
                vignetteOpac = 1.0f;
            }
            else
            {
                ovHealth = (byte)Math.Max(0, ovHealth - amount);
            }
        }

        private Texture2D CreateCircleTexture(int radius, Color color)
        {
            if (radius > RangeTextureLimit) radius = RangeTextureLimit;

            //Create a new texture with the same width and height as the radius
            Texture2D circleTexture = new(SceneManager.Instance.GraphicsDevice, radius, radius);

            rangeTexture?.Dispose();
            rangeTexture = circleTexture;

            //Create an array to hold the texture color data
            Color[] colorData = new Color[radius * radius];

            //Set the center of the circle
            int center = radius / 2;
            float squareRadius = (radius / 2f) * (radius / 2f);

            //Fill the color data array with the circle color
            for (int x = 0; x < radius; x++)
            {
                int xDist = x - center;
                for (int y = 0; y < radius; y++)
                {
                    int yDist = y - center;

                    //Calculate the distance between the current pixel and the center of the circle
                    if (xDist * xDist + yDist * yDist <= squareRadius)
                    {
                        colorData[x + y * radius] = color;
                    }
                    else
                    {
                        colorData[x + y * radius] = Color.Transparent;
                    }
                }
            }

            //Set the color data on the texture
            circleTexture.SetData(colorData);

            //Return the generated texture
            return circleTexture;
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

            foreach (Entity e in entities) if (e is Tower or Enemy) builder.Append(e.Serialise());

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
            if (message == string.Empty) return;

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

                    enemyEntities.Clear();

                    string[] temp = Regex.Split(message, "(?<=[|])");
                    StringBuilder sb = new();
                    for (int i = 1; i < temp.Length; i++) sb.Append(temp[i]);
                    message = sb.ToString();

                    if (message == string.Empty) break;

                    //Get the data to parse
                    string[] entities = message.Split('|');
                    foreach (string data in entities)
                    {
                        string[] csvData = data.Split(',');

                        if (csvData[0] == "0")
                        {
                            Tower tower = new(data);
                            enemyEntities.Add(tower);
                        }
                        else if (csvData[0] == "1")
                        {
                            Enemy enemy = new(data);
                            enemyEntities.Add(enemy);
                        }
                    }
                }
                break;
                case (byte)Header.SYNC:
                {
                    string[] split = message.Split(",");
                    vMoney = ushort.Parse(split[0]);
                    vHealth = byte.Parse(split[1]);
                }
                break;
                case (byte)Header.ROUND_BEGIN:
                {
                    int number = int.Parse(message);
                    currentWave = Wave.waves[number].DeepCopy();
                    isWaveActive = true;
                    ready = false;

                    Console.WriteLine($"Starting round {number}");
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

                    try
                    {
                        string texName = p.value as string;
                        AssetContainer.ReadTexture(texName).Dispose();
                    }
                    catch { }
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
                        Popup popup = new(new(MouseController.GetMousePosition().x, MouseController.GetMousePosition().y), AssetContainer.ReadString("POPUP_INV_LOC"), 1f, GlobalSettings.TextError, AssetContainer.GetFont("fMain"), 1.75f, new(0, -9f));
                        entities.Add(popup);
                        return;
                    }
                }

                if (vMoney < Tower.towerDatas[towers.GetActiveIndex()].cost)
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
                    vMoney -= Tower.towerDatas[towers.GetActiveIndex()].cost;

                    //Create the tower object and add it to the entities
                    TextureCollection textures = new();
                    textures.AddTexture(AssetContainer.ReadTexture(Tower.towerDatas[towers.GetActiveIndex()].texIdle));
                    Animation anim = new(textures, 0);
                    Tower tower = new(towers.GetActiveIndex(), new(tmx - playFieldOffset.X, tmy - playFieldOffset.Y), anim, playFieldOffset, true);
                    entities.Add(tower);

                    Popup popup = new(new(MouseController.GetMousePosition().x, MouseController.GetMousePosition().y), $"-${tower.Data.cost}", 1f, GlobalSettings.TextError, AssetContainer.GetFont("fMain"), 1.75f, new(0, -9f));
                    entities.Add(popup);

                    Client.Instance.SendMessage($"{Header.SPEND_MONEY}{Client.Instance.PlayerID},{Tower.towerDatas[towers.GetActiveIndex()].cost}");

                    towers.Clear();
                    towers.Update();
                    gameState = GameState.PLAY;
                }
            }
        }

        /// <summary>
        /// Draw the enemy entities that this client has created
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch to draw with</param>
        private void DrawEnemyEntities(SpriteBatch spriteBatch)
        {
            foreach (Entity e in enemyEntities)
            {
                if (e is Tower) platformTexture.Draw(e.GetPosition() + enemyPlayFieldOffset, spriteBatch, Color.White);

                e?.Draw(spriteBatch);
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
                if (!Tower.towerDatas.ContainsKey(i)) continue;

                TowerData towerData = Tower.towerDatas[i];
                Texture2D texture = AssetContainer.ReadTexture(towerData.texButton);

                Switch swtch = towers[i];
                texture.Draw(new(swtch.AABB.X + ((swtch.AABB.Width - texture.Width) / 2), swtch.AABB.Y + (swtch.AABB.Height - texture.Height) / 2), spriteBatch, Color.White);

                foreach (Label l in towerCostLabels) l.DrawWithShadow(spriteBatch);
            }
        }

        private void DrawTowers(SpriteBatch spriteBatch)
        {
            foreach (Tower t in entities.OfType<Tower>())
            {
                platformTexture.Draw(t.GetPosition() + playFieldOffset, spriteBatch, Color.White);
                t.Draw(spriteBatch);
            }
        }

        private void AddMoneyToServer()
        {
            if (moneyMadeThisFrame.Count == 0) return;

            ushort total = 0;
            while (moneyMadeThisFrame.Count > 0) total += moneyMadeThisFrame.Pop();
            Client.Instance.SendMessage($"{Header.ADD_MONEY}{Client.Instance.PlayerID},{total}");
            vMoney = (ushort)Math.Min(vMoney + total, ushort.MaxValue);
        }

        public void SpawnEnemyFromWave(string name)
        {
            Vector2 pos = enemySpawnPositions[0];
            Vector2 absPos = playFieldOffset + (pos * TileSize);
            entities.Add(new Enemy(name, absPos, pos, playFieldOffset, true));
            enemyEntities.Add(new Enemy(name, absPos, pos, playFieldOffset, false));

            //Force a snapshot send
            tick = 0;
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
            foreach (Entity e in entities) if (e is not Popup and not Tower) e?.Draw(spriteBatch);
            DrawTowers(spriteBatch);
            DrawEnemyEntities(spriteBatch);

            spriteBatch.Draw(nukeFlash, new Rectangle(0, 0, SceneManager.Instance.graphics.PreferredBackBufferWidth, SceneManager.Instance.graphics.PreferredBackBufferHeight), Color.White * nukeFlashOpacity);
        }

        public override void DrawGUI(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.Draw(vignette, Vector2.Zero, Color.White * vignetteOpac);

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
                        TowerData data = Tower.towerDatas[towers.GetActiveIndex()];

                        //Check if we should recreate the texture
                        if (rangeTexture is null || rangeTextureRad != data.range)
                        {
                            CreateCircleTexture(data.range, Color.White);
                        }
                        rangeTexture.Draw(new Vector2(tmx - rangeTexture.Width / 2 + TileSize / 2, tmy - rangeTexture.Height / 2 + TileSize / 2), spriteBatch, (canPlace ? Color.White : Color.Red) * 0.2f);

                        //Update our buffers
                        rangeTextureRad = data.range;
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
                string message = $"PING: {(Client.Instance.Peer.Ping)}ms";
                spriteBatch.DrawString(AssetContainer.GetFont("fMain"), message, new Vector2(100, 100), Color.White);
            }

            foreach (Popup popup in entities.OfType<Popup>()) popup.Draw(spriteBatch);
            if (!ready && !isWaveActive) readyButton.Draw(spriteBatch);
        }

        public override void LoadContent()
        {
            Console.WriteLine("LOAD Game");

            void LoadTextures()
            {
                nukeFlash = new Texture2D(GraphicsDevice, 1, 1);
                nukeFlash.SetData(new Color[] { Color.White });

                bkg = AssetContainer.ReadTexture("sMenu");
                vignette = AssetContainer.ReadTexture("sVignette");
                divider = AssetContainer.ReadTexture("sBorder");
                towerSelUnclick = AssetContainer.ReadTexture("sTowerSelUnclick");
                towerSelClick = AssetContainer.ReadTexture("sTowerSelClick");
                menuFilter = AssetContainer.ReadTexture("sMenuFilter");
                statPanel = AssetContainer.ReadTexture("sStat");
                platformTexture = AssetContainer.ReadTexture("sPlatform");

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
                short buttonsX = GlobalSettings.PlayerOnRight ? (short)(SceneManager.Instance.graphics.PreferredBackBufferWidth - towerSelClick.Width) : (short)0;

                towerCostLabels = new Label[TowerCount];

                for (int i = 0; i < TowerCount; i++)
                {
                    AABB aabb = new(buttonsX, (short)((towerSelClick.Height * 3) + towerSelClick.Height * i - towerSelClick.Height * 1.5), (short)(towerSelClick.Width), (short)(towerSelClick.Height));
                    Switch s = new(aabb, towerSelUnclick, towerSelClick, false);
                    towers.AddSwitch(s);

                    Label costLabel = new($"${Tower.towerDatas[i].cost}", 1f, new(buttonsX, towerSelClick.Height * 3 + towerSelClick.Height * i - towerSelClick.Height * 1.5f + towerSelClick.Height), GlobalSettings.TextMain, AssetContainer.GetFont("fMain"), Origin.BOTTOM_LEFT);
                    towerCostLabels[i] = costLabel;
                }

                Vector2 topRight = new(SceneManager.Instance.graphics.PreferredBackBufferWidth, 0);

                money = new("NULL", 1.3f, topRight, Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_RIGHT, 0f);
                health = new("NULL", 1.3f, topRight + new Vector2(0, 48), Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_RIGHT, 0f);
                gameOverLabel = new("", 4f, new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 100), Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_CENTRE, 0f);

                oMoney = new("NULL", 1.3f, Vector2.Zero, Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_LEFT, 0f);
                oHealth = new("NULL", 1.3f, new Vector2(0, 48), Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_LEFT, 0f);

                selectedTower = new("NULL", 1.6f, new(SceneManager.Instance.graphics.PreferredBackBufferWidth / 2, 0), Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_CENTRE, 0f);

                sellButton = new(new(buttonsX, (short)(towerSelClick.Height * 3 + towerSelClick.Height * TowerCount - towerSelClick.Height * 1.5), 64, 32), AssetContainer.ReadTexture("sSellUnlick"), AssetContainer.ReadTexture("sSellClick"));
                uIManager.Add(sellButton);


                AABB readyButtonBB = new((short)((SceneManager.Instance.graphics.PreferredBackBufferWidth / 2) - (165 / 2)), 950, 165, 48);
                readyButton = new(readyButtonBB, AssetContainer.ReadTexture("sMenuButtonUnclicked"), AssetContainer.ReadTexture("sMenuButtonClicked"), AssetContainer.ReadString("GM_READY"), 1f, AssetContainer.GetFont("fMain"));

                //Swap stats if needed
                if (!GlobalSettings.PlayerOnRight)
                {
                    money.MoveLabel(Vector2.Zero);
                    health.MoveLabel(new(0, 48));
                    money.ChangeOrigin(Origin.TOP_LEFT);
                    health.ChangeOrigin(Origin.TOP_LEFT);

                    oMoney.MoveLabel(topRight);
                    oHealth.MoveLabel(topRight + new Vector2(0, 48));
                    oMoney.ChangeOrigin(Origin.TOP_RIGHT);
                    oHealth.ChangeOrigin(Origin.TOP_RIGHT);
                }
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
#if DEBUG
                else
                {
                    playfield = new byte[,]
                    {
                        {0,0,0,2,15,6,0,0,0,0,0,0,0,0,0,2,15,6,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                        {0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                        {0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,0,0,0,0,18,19,19,19,20,0,0,0,0,0,0,0,0,0,0,0,0},
                        {0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,2,1,6,0,0,0,0,0,0,0,0,18,19,19,19,19,26,16,16,16,25,19,19,20,0,0,0,0,0,0,0,0,0},
                        {0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,2,1,6,0,0,0,0,0,0,0,18,26,16,16,16,16,16,16,16,16,16,16,16,25,20,0,0,0,0,0,0,0,0},
                        {0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,2,1,6,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,16,16,16,16,16,25,20,0,0,0,0,0,0,0},
                        {0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,2,1,6,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,16,16,16,16,16,16,25,20,0,0,0,0,0,0},
                        {0,0,0,2,1,10,5,0,0,0,0,0,0,0,3,11,1,6,0,0,0,0,0,0,0,24,28,16,16,16,16,16,16,16,16,16,16,16,16,16,16,25,20,0,0,0,0,0},
                        {0,0,0,2,1,1,10,4,5,0,0,0,3,4,11,1,1,6,0,0,0,0,0,0,0,0,24,28,16,16,16,16,16,16,16,16,16,16,16,16,16,16,21,0,0,0,0,0},
                        {0,0,0,9,13,1,1,1,10,5,0,3,11,1,1,1,12,7,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,16,16,16,16,16,16,21,0,0,0,0,0},
                        {0,0,0,0,9,8,13,1,1,10,4,11,1,1,12,8,7,0,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,16,16,16,16,16,16,21,0,0,0,0,0},
                        {0,0,0,0,0,0,9,13,1,1,1,1,1,12,7,0,0,0,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,16,16,16,16,16,16,25,20,0,0,0,0},
                        {0,0,0,0,0,0,0,9,8,13,1,12,8,7,0,0,0,0,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,21,0,0,0,0},
                        {0,0,0,0,0,0,0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,21,0,0,0,0},
                        {0,0,0,0,0,0,0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,16,16,16,16,16,16,16,21,0,0,0,0},
                        {0,0,0,0,0,0,0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,24,28,16,16,16,16,16,16,16,16,16,16,16,16,16,16,21,0,0,0,0},
                        {0,0,0,0,0,0,0,0,0,2,1,10,4,4,5,0,0,0,0,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,16,16,16,16,16,16,21,0,0,0,0},
                        {0,0,0,0,0,0,0,0,0,2,1,1,1,1,10,5,0,0,0,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,16,16,16,16,16,27,22,0,0,0,0},
                        {0,0,0,0,0,0,0,0,0,9,8,8,13,1,1,10,5,0,0,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,16,16,16,16,27,22,0,0,0,0,0},
                        {0,0,0,0,0,0,0,0,0,0,0,0,9,13,1,1,6,0,0,0,0,0,0,0,0,0,0,0,24,28,16,16,16,16,16,16,16,16,16,16,27,22,0,0,0,0,0,0},
                        {0,0,0,0,0,0,0,0,0,0,0,0,0,9,13,1,6,0,0,0,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,16,27,22,0,0,0,0,0,0,0},
                        {0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,16,21,0,0,0,0,0,0,0,0},
                        {0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,27,22,0,0,0,0,0,0,0,0},
                        {0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,21,0,0,0,0,0,0,0,0,0},
                        {0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,21,0,0,0,0,0,0,0,0,0},
                        {0,0,0,0,0,0,0,0,0,0,0,0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,25,20,0,0,0,0,0,0,0,0},
                        {0,3,4,4,4,4,4,5,0,0,0,0,0,3,11,1,6,0,0,0,0,0,0,0,0,0,0,0,0,24,28,16,16,16,16,16,16,16,16,21,0,0,0,0,0,0,0,0},
                        {0,2,1,1,1,1,1,6,0,0,0,0,3,11,1,1,6,0,0,0,0,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,25,20,0,0,0,0,0,0,0},
                        {0,2,1,12,8,13,1,6,0,0,0,3,11,1,1,12,7,0,0,0,0,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,16,21,0,0,0,0,0,0,0},
                        {0,2,1,6,0,2,1,10,4,4,4,11,1,1,12,7,0,0,0,0,0,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,16,21,0,0,0,0,0,0,0},
                        {0,2,1,6,0,2,1,1,1,1,1,1,1,12,7,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,24,28,16,16,16,16,16,16,16,16,21,0,0,0,0,0,0,0},
                        {0,2,1,10,5,9,8,8,8,8,8,8,8,7,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,16,21,0,0,0,0,0,0,0},
                        {0,2,1,1,10,5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,17,16,16,16,16,16,16,16,27,22,0,0,0,0,0,0,0},
                        {0,9,13,1,1,6,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,24,23,28,16,16,16,16,27,22,0,0,0,0,0,0,0,0},
                        {0,0,9,13,1,6,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,24,28,16,16,27,22,0,0,0,0,0,0,0,0,0},
                        {0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,24,23,23,22,0,0,0,0,0,0,0,0,0,0},
                        {0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                        {0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                        {0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                        {0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                        {0,0,0,2,1,6,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                        {0,0,0,2,14,6,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                    };
                }
#endif

                Enemy.mapData = playfield;

                //Now we can load the random offsets
                //But while we are here, we can save another loop and load tower locations into the enemies
                textureOffsets = new byte[(int)GameSize.Y, (int)GameSize.X];
                for (int y = 0; y < GameSize.Y; y++)
                {
                    for (int x = 0; x < GameSize.X; x++)
                    {
                        //Load HQ position
                        if (playfield[y, x] == 15)
                        {
                            Enemy.HQLocations.Add(new(x, y));
                        }

                        //Load spawn positions
                        else if (playfield[y, x] == 14)
                        {
                            enemySpawnPositions.Add(new(x, y));
                        }

                        //Load random grass offset
                        else if (playfield[y, x] == 0)
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
                username = new(Client.Instance.PlayerName, 1.1f, rghtQ, Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_CENTRE, 0f);

                if (!IsDebugPlay)
                {
                    Client.Instance.SendMessage($"{Header.REQUEST_USERNAME_FROM_ID}{Client.Instance.EnemyID}");
                    Client.Instance.WaitForNewMessage();
                    string enemyUsername = Client.Instance.ReadLatestMessage();
                    otherUsername = new(enemyUsername, 1.1f, leftQ, Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_CENTRE, 0f);
                }
                else
                {
                    string enemyUsername = "null";
                    otherUsername = new(enemyUsername, 1.1f, leftQ, Color.White, AssetContainer.GetFont("fMain"), Origin.TOP_CENTRE, 0f);
                }

                //Swap the locations if needed
                if (!GlobalSettings.PlayerOnRight)
                {
                    username.MoveLabel(leftQ);
                    otherUsername.MoveLabel(rghtQ);
                }

            }

            nukeFlashOpacity = 0f;

            vignetteOpac = 0.0f;
            gameOverOpacity = 0f;
            entities = new();
            enemyEntities = new();
            showDebugStats = false;
            pausedMenuUILayer = new();
            towers = new();
            showCheatPanel = false;
            cheatPanel = null;
            vHealth = 100;
            vMoney = 800;
            gameState = GameState.PLAY;

            ready = false;
            isWaveActive = false;
            currentWave = null;

            SceneManager.Instance.ManagedUIManager = false; //We need to draw a layer over the UI when paused, so we need to take full control

            LoadTextures();
            InitUI();
            InitPlayerField();
            LoadMap();
            SetUpUsernames();

            uIManager.Add(towers);

            playFieldOffset = new(1920 / 2 + divider.Width, 96);
            enemyPlayFieldOffset = new(1920 / 2 - PlayfieldWidth - divider.Width, 96);

            //If the player is on the left, swap the offsets
            if (!GlobalSettings.PlayerOnRight)
            {
                //This will swap the variables. The compiler told me to do it this way. Less readable IMO but oh well, it shuts the compiler up
                (enemyPlayFieldOffset, playFieldOffset) = (playFieldOffset, enemyPlayFieldOffset);
            }

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
            nukeFlash.Dispose();
            SceneManager.Instance.ManagedUIManager = true; //Pass control back to the SceneManager
            UIManager.Clear();
            rangeTexture = null;
        }

        public override void Update(GameTime gameTime)
        {
            if (currentWave is not null)
            {
                currentWave.Update(gameTime);
                if (currentWave.IsOver && !entities.Where(e => e is Enemy).Any())
                {
                    currentWave = null;
                    Client.Instance?.SendMessage($"{Header.ROUND_END}{Client.Instance?.PlayerID}");
                    isWaveActive = false;
                }
            }

            if (!ready && !isWaveActive) readyButton.Update();
            if (readyButton.IsClicked())
            {
                Client.Instance?.SendMessage($"{Header.READY_FOR_WAVE}{Client.Instance?.PlayerID}");
                ready = true;
            }

            while (entityBuffer.Count > 0) entities.Add(entityBuffer.Pop());

            HandleServer();
            if (tick++ % 150 == 0) SendSnapShot();
            CheckForTowerPlacement();
            CheckForBuildMode();

            if (vHealth == 0 && !isDead)
            {
                //Tell the server the game is over and the player lost
                isDead = true;
                Client.Instance?.SendMessage($"{Header.GAME_OVER}{Client.Instance?.EnemyID}");
            }

            //Adjust the vignette opacity
            vignetteOpac = (float)Math.Max(0, vignetteOpac - vignetteSpeed * gameTime.ElapsedGameTime.TotalSeconds);

            //Adjust the nuke flash
            nukeFlashOpacity = (float)Math.Max(0, nukeFlashOpacity - 1 * gameTime.ElapsedGameTime.TotalSeconds);

#if DEBUG

            //Debug stuff
            if (KeyboardController.IsPressed(Keys.NumPad0))
            {
                string name = "Basic Unit";
                Vector2 pos = enemySpawnPositions[0];
                Vector2 absPos = playFieldOffset + (pos * TileSize);
                Enemy enemy = new(name, absPos, pos, playFieldOffset, true);
                entities.Add(enemy);

                //Force a snapshot send
                tick = 0;
            }
            else if (KeyboardController.IsPressed(Keys.NumPad1))
            {
                string name = "Advanced Unit";
                Vector2 pos = enemySpawnPositions[0];
                Vector2 absPos = playFieldOffset + (pos * TileSize);
                Enemy enemy = new(name, absPos, pos, playFieldOffset, true);
                entities.Add(enemy);

                tick = 0;
            }

#endif

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

            if (Tower.towerDatas.ContainsKey(towers.GetActiveIndex()) && gameState == GameState.PLACEMENT && towers.GetActiveIndex() != -1)
            {
                int selectedTower = towers.GetActiveIndex();
                this.selectedTower.SetLabelText(Tower.towerDatas[selectedTower].name);
            }

            foreach (Entity e in entities) e?.Update(gameTime);
            entities.RemoveAll(e => e.MarkForDeletion);
            foreach (Entity e in enemyEntities) e?.Update(gameTime);
            enemyEntities.RemoveAll(e => e.MarkForDeletion);

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
            AddMoneyToServer();
        }

        public Game() : base()
        {
            rng = new();
            Instance = this;
        }
    }
}