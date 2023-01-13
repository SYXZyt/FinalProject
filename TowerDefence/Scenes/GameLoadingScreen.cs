using UILibrary;
using System.Xml;
using UILibrary.Scenes;
using System.Diagnostics;
using AssetStreamer.Assets;
using TowerDefence.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TowerDefence.Entities.GameObjects;
using TowerDefence.Entities.GameObjects.Towers;

using Color = Microsoft.Xna.Framework.Color;

namespace TowerDefence.Scenes
{
    internal sealed class GameLoadingScreen : Scene
    {
        private int i = 0;

        private sealed class LoadFile
        {
            public enum TypeToLoad
            {
                TEXT,
                TEXTURE,
                SOUND,
                FONT,
            }

            private readonly TypeToLoad typeToLoad;
            private readonly string path;
            private readonly string name;

            public TypeToLoad Type => typeToLoad;
            public string Path => path;
            public string Name => name;

            public void Load()
            {
                Console.WriteLine($"LOADING_ASSET '{path}'");

                switch (typeToLoad)
                {
                    case TypeToLoad.TEXT:
                        StringLoader.LoadString(path);
                        break;
                    case TypeToLoad.TEXTURE:
                        TextureLoader.LoadTexture(path, name, SceneManager.Instance.GraphicsDevice);
                        break;
                    case TypeToLoad.FONT:
                        FontLoader.LoadFont(path, name, SceneManager.Instance.GraphicsDevice);
                        break;
                    case TypeToLoad.SOUND:
                        SoundLoader.LoadSound(path, name);
                        break;
                }
            }

            public LoadFile(TypeToLoad typeToLoad, string path, string name = "")
            {
                this.typeToLoad = typeToLoad;
                this.path = path;
                this.name = name;
            }
        }

        private static class ConfigLoader
        {
            private static void LoadBulletCfg()
            {
                if (!File.Exists(@"cfg\entity_bullet.xml")) throw new FileNotFoundException($"Could not find 'cfg\\entity_bullet.xml'", "entity_bullet.xml");

                XmlDocument xmlDoc = new();
                xmlDoc.Load(@"cfg\entity_bullet.xml");

                XmlNode entityID = xmlDoc.SelectSingleNode("/entity/entity_id");
                Bullet.ID = Convert.ToByte(entityID.InnerText);

                XmlNode entityTexture = xmlDoc.SelectSingleNode("/entity/cfg_texture");
                Bullet.TextureName = entityTexture.InnerText;

                XmlNode entitySpeed = xmlDoc.SelectSingleNode("/entity/cfg_speed");
                Bullet.BulletSpeed = (float)Convert.ToDouble(entitySpeed.InnerText);
            }

            private static TowerData LoadTower(string path)
            {
                if (!File.Exists(path)) throw new FileNotFoundException($"Could not find '{path}'", path);

                XmlDocument xml = new();
                xml.Load(path);

                TowerData towerData = new();

                XmlNode towerId = xml.SelectSingleNode("/tower/tower_id");
                towerData.id = Convert.ToByte(towerId.InnerText);

                XmlNode towerName = xml.SelectSingleNode("/tower/tower_name");
                towerData.name = towerName.InnerText;

                XmlNode towerRange = xml.SelectSingleNode("/tower/range");
                towerData.range = Convert.ToUInt16(towerRange.InnerText);

                XmlNode towerCost = xml.SelectSingleNode("/tower/cost");
                towerData.cost = Convert.ToUInt16(towerCost.InnerText);

                XmlNode textureIdle = xml.SelectSingleNode("/tower/texture_idle");
                towerData.texIdle = textureIdle.InnerText;

                XmlNode textureButton = xml.SelectSingleNode("/tower/texture_button");
                towerData.texButton = textureButton.InnerText;

                XmlNode towerRate = xml.SelectSingleNode("/tower/rate");
                towerData.rate = Convert.ToUInt16(towerRate.InnerText);

                return towerData;
            }

            private static void LoadColours()
            {
                if (!File.Exists(@"cfg\rgb.xml")) return;

                XmlDocument xml = new();
                xml.Load(@"cfg\rgb.xml");

                XmlNode mainR = xml.SelectSingleNode("/color/text_main_r");
                XmlNode mainB = xml.SelectSingleNode("/color/text_main_g");
                XmlNode mainG = xml.SelectSingleNode("/color/text_main_b");
                Color main = new(int.Parse(mainR.InnerText), int.Parse(mainG.InnerText), int.Parse(mainB.InnerText));

                XmlNode errorR = xml.SelectSingleNode("/color/text_error_r");
                XmlNode errorB = xml.SelectSingleNode("/color/text_error_g");
                XmlNode errorG = xml.SelectSingleNode("/color/text_error_b");
                Color error = new(int.Parse(errorR.InnerText), int.Parse(errorG.InnerText), int.Parse(errorB.InnerText));

                XmlNode warningR = xml.SelectSingleNode("/color/text_warning_r");
                XmlNode warningB = xml.SelectSingleNode("/color/text_warning_g");
                XmlNode warningG = xml.SelectSingleNode("/color/text_warning_b");
                Color warning = new(int.Parse(warningR.InnerText), int.Parse(warningG.InnerText), int.Parse(warningB.InnerText));

                GlobalSettings.TextMain = main;
                GlobalSettings.TextError = error;
                GlobalSettings.TextWarning = warning;
            }

            public static void Load()
            {
                LoadBulletCfg();
                LoadColours();
                TowerData debugTower = LoadTower(@"cfg\tower_dev.xml");
                Tower.towerDatas.Add(debugTower.name, debugTower);
            }
        }

        private ProgressBar progressBar;

        private readonly Stopwatch timer;

        private int loadTaskCount = 0;
        private readonly Queue<LoadFile> filesToLoad;

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            progressBar.Draw(spriteBatch);
        }

        public override void DrawGUI(SpriteBatch spriteBatch, GameTime gameTime)
        {

        }

        public override void LoadContent()
        {
            static Texture2D CreateTexture(GraphicsDevice device, int width, int height, Func<int, Color> paint)
            {
                //initialize a texture
                Texture2D texture = new(device, width, height);

                //the array holds the color for each pixel in the texture
                Color[] data = new Color[width * height];
                for (int pixel = 0; pixel < data.Length; pixel++)
                {
                    //the function applies the color according to the specified pixel
                    data[pixel] = paint(pixel);
                }

                //set the color
                texture.SetData(data);

                return texture;
            }

            Console.WriteLine($"LOAD GameLoadingScreen");

            SceneManager.Instance.IsMouseVisible = true;
            SceneManager.Instance.graphics.ApplyChanges();
            LoadSettings();

            byte[] texture = Properties.Resources.unknown;
            using (var stream = new MemoryStream(texture))
            {
                TextureLoader.LoadTexture(Texture2D.FromStream(GraphicsDevice, stream), string.Empty);
            }


            //Get all of the asset files to load
            GetAssetsToLoad();

            Color f = new(52, 116, 235);
            Color u = new(0);
            AABB progressBarBox = new(5, (short)(SceneManager.Instance.graphics.PreferredBackBufferHeight - 18), (short)(SceneManager.Instance.graphics.PreferredBackBufferWidth - 10), 6);

            loadTaskCount = filesToLoad.Count;
            progressBar = new(CreateTexture(GraphicsDevice, 1, 4, c => f), CreateTexture(GraphicsDevice, 1, 4, c => u), progressBarBox, loadTaskCount);

            ConfigLoader.Load();
        }

        public override void UnloadContent()
        {
            Console.WriteLine($"UNLOAD GameLoadingScreen");
        }

        public override void Update(GameTime gameTime)
        {
            i++;

            if (!timer.IsRunning)
            {
                timer.Start();
            }

            //If there is anything to load, load it
            //For some absolutely unknown reason to me, I have to wait 30 frames
            //before I can load any assets
            //30 is the lowest consistent number it takes in order to run correctly
            //If I wait any less than 30, the draw function may not call
            //Monogame, pls fix
            //So for some reason, draw still isn't being called correctly.
            //I cannot figure it out
            if (i > 100)
            {
                progressBar.UpdateProgress(loadTaskCount - filesToLoad.Count);
                if (filesToLoad.Count > 0)
                {
                    LoadFile task = filesToLoad.Dequeue();
                    task.Load();
                }
                else
                {
                    timer.Stop();
                    Console.WriteLine($"Loaded assets in {timer.Elapsed.TotalMilliseconds}ms");
                    SceneManager.Instance.LoadScene("mainMenu");
                }
            }
        }

        private void GetAssetsToLoad()
        {
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXT, @"Assets\Strings\text.txt"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.FONT, @"Assets\Fonts\MilkyCoffee.ttf", "fMain"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\menu.png", "sMenu"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\menuButtonClicked.png", "sMenuButtonClicked"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\menuButtonUnclicked.png", "sMenuButtonUnclicked"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\textbackground.png", "sTextboxBkg"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\cross.png", "sCross"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\tick.png", "sTick"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\tickDrop.png", "sTickDrop"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\crossDrop.png", "sCrossDrop"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\border.png", "sBorder"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towerSelectionClicked.png", "sTowerSelClick"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towerSelectionUnclicked.png", "sTowerSelUnclick"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\filter.png", "sMenuFilter"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\stats.png", "sStat"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\settingWindowed.png", "sSettingWindowed"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\settingFullscreen.png", "sSettingFullscreen"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\bullet.png", "sBullet"));

            //Load the map textures
            for (int i = 0; i < 29; i++)
            {
                LoadFile file = new(LoadFile.TypeToLoad.TEXTURE, $@"Assets\Textures\maps\{i}.png", $"map_{i}");
                filesToLoad.Enqueue(file);

                if (i == 0)
                {
                    for (int j = 0; j < 2; j++) filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, $@"Assets\Textures\maps\{i}_{j + 1}.png", $"map_{i}_{j}"));
                }
                else if (i == 16)
                {
                    for (int j = 0; j < 3; j++) filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, $@"Assets\Textures\maps\{i}_{j + 1}.png", $"map_{i}_{j}"));
                }
            }
        }

        private static void LoadSettings() => SettingFileHandler.LoadSettingsFile();

        public GameLoadingScreen()
        {
            filesToLoad = new();
            timer = new();
        }
    }
}