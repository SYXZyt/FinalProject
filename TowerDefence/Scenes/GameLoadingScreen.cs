using UILibrary;
using System.Xml;
using AssetStreamer;
using UILibrary.Scenes;
using System.Diagnostics;
using AssetStreamer.Assets;
using TowerDefence.Visuals;
using TowerDefence.Settings;
using TowerDefence.Component;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TowerDefence.Entities.GameObjects;
using TowerDefence.Entities.GameObjects.Towers;
using TowerDefence.Entities.GameObjects.Enemies;

using Color = Microsoft.Xna.Framework.Color;
using TextureCollection = TowerDefence.Visuals.TextureCollection;

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
                towerData.rate = (float)Convert.ToDouble(towerRate.InnerText);

                XmlNode projectile = xml.SelectSingleNode("/tower/projectile");
                towerData.projectile = projectile.InnerText;

                return towerData;
            }

            private static EnemyData LoadEnemy(string path)
            {
                if (!File.Exists(path)) throw new FileNotFoundException($"Could not find '{path}'", path);

                XmlDocument xml = new();
                xml.Load(path);

                EnemyData enemyData = new();

                XmlNode unitId = xml.SelectSingleNode("/unit/unit_id");
                enemyData.id = byte.Parse(unitId.InnerText);

                XmlNode unitName = xml.SelectSingleNode("/unit/unit_name");
                enemyData.name = unitName.InnerText;

                XmlNode unitSpeed = xml.SelectSingleNode("/unit/unit_speed");
                enemyData.speed = int.Parse(unitSpeed.InnerText);

                XmlNode unitHealth = xml.SelectSingleNode("/unit/unit_hitpoints");
                enemyData.health = int.Parse(unitHealth.InnerText);

                XmlNode unitDmg = xml.SelectSingleNode("/unit/unit_damage");
                enemyData.damage = int.Parse(unitDmg.InnerText);

                return enemyData;
            }

            private static void LoadColours()
            {
                if (!File.Exists(@"cfg\rgb.xml")) throw new FileNotFoundException(@"cfg\rgb.xml", @"cfg\rgb.xml");

                XmlDocument xml = new();
                xml.Load(@"cfg\rgb.xml");

                XmlNode mainR = xml.SelectSingleNode("/color/text_main_r");
                XmlNode mainG = xml.SelectSingleNode("/color/text_main_g");
                XmlNode mainB = xml.SelectSingleNode("/color/text_main_b");
                Color main = new(int.Parse(mainR.InnerText), int.Parse(mainG.InnerText), int.Parse(mainB.InnerText));

                XmlNode errorR = xml.SelectSingleNode("/color/text_error_r");
                XmlNode errorG = xml.SelectSingleNode("/color/text_error_g");
                XmlNode errorB = xml.SelectSingleNode("/color/text_error_b");
                Color error = new(int.Parse(errorR.InnerText), int.Parse(errorG.InnerText), int.Parse(errorB.InnerText));

                XmlNode warningR = xml.SelectSingleNode("/color/text_warning_r");
                XmlNode warningG = xml.SelectSingleNode("/color/text_warning_g");
                XmlNode warningB = xml.SelectSingleNode("/color/text_warning_b");
                Color warning = new(int.Parse(warningR.InnerText), int.Parse(warningG.InnerText), int.Parse(warningB.InnerText));

                GlobalSettings.TextMain = main;
                GlobalSettings.TextError = error;
                GlobalSettings.TextWarning = warning;
            }

            private static void LoadEffects()
            {
                if (!File.Exists(@"Effects\Flame.xml")) throw new FileNotFoundException(@"Effects\Flame.xml", @"Effects\Flame.xml");

                XmlDocument xml = new();
                xml.Load(@"Effects\Flame.xml");

                XmlNode name = xml.SelectSingleNode("/effect/effect_name");
                XmlNode timer = xml.SelectSingleNode("/effect/effect_timer");
                XmlNode damage = xml.SelectSingleNode("effect/effect_damage");
                XmlNode damageCancel = xml.SelectSingleNode("/effect/effect_damage_cancel");

                DamageEffectStats fireDamageStats = new()
                {
                    time = float.Parse(timer.InnerText),
                    damage = int.Parse(damage.InnerText),
                    damageCancel = damageCancel.InnerText == "1",
                };

                DamageEffectComponent.damageEffectStats[name.InnerText] = fireDamageStats;

                xml.Load(@"Effects\Shock.xml");

                name = xml.SelectSingleNode("/effect/effect_name");
                timer = xml.SelectSingleNode("/effect/effect_timer");
                damage = xml.SelectSingleNode("effect/effect_damage");
                damageCancel = xml.SelectSingleNode("/effect/effect_damage_cancel");

                DamageEffectStats shockDamageStats = new()
                {
                    time = float.Parse(timer.InnerText),
                    damage = int.Parse(damage.InnerText),
                    damageCancel = damageCancel.InnerText == "1",
                };

                DamageEffectComponent.damageEffectStats[name.InnerText] = shockDamageStats;
            }

            public static void Load()
            {
                LoadBulletCfg();
                LoadColours();
                LoadEffects();

                TowerData debugTower = LoadTower(@"cfg\tower_dev.xml");
                Tower.towerDatas.Add(debugTower.id, debugTower);

                TowerData basicTower = LoadTower(@"cfg\tower_basic.xml");
                Tower.towerDatas.Add(basicTower.id, basicTower);

                TowerData advancedTower = LoadTower(@"cfg\tower_advanced.xml");
                Tower.towerDatas.Add(advancedTower.id, advancedTower);

                TowerData minigunTower = LoadTower(@"cfg\tower_minigun.xml");
                Tower.towerDatas.Add(minigunTower.id, minigunTower);

                TowerData superTower = LoadTower(@"cfg\tower_super.xml");
                Tower.towerDatas.Add(superTower.id, superTower);

                TowerData flameTower = LoadTower(@"cfg\tower_flame.xml");
                Tower.towerDatas.Add(flameTower.id, flameTower);

                TowerData rocketTower = LoadTower(@"cfg\tower_rocket.xml");
                Tower.towerDatas.Add(rocketTower.id, rocketTower);

                TowerData sniperTower = LoadTower(@"cfg\tower_sniper.xml");
                Tower.towerDatas.Add(sniperTower.id, sniperTower);

                TowerData nukeTower = LoadTower(@"cfg\tower_nuke.xml");
                Tower.towerDatas.Add(nukeTower.id, nukeTower);

                TowerData teslaTower = LoadTower(@"cfg\tower_tesla.xml");
                Tower.towerDatas.Add(teslaTower.id, teslaTower);

                TowerData batteryTower = LoadTower(@"cfg\tower_missile.xml");
                Tower.towerDatas.Add(batteryTower.id, batteryTower);

                EnemyData debugUnit = LoadEnemy(@"cfg\unit_dev.xml");
                Enemy.enemyDatas.Add(debugUnit.name, debugUnit);

                //Load wave data
                Waves.Wave.GenerateWave(@"waves\Wave0.xml");
            }
        }

        private ProgressBar progressBar;

        private readonly Stopwatch timer;

        private int loadTaskCount = 0;
        private readonly Queue<LoadFile> filesToLoad;

        private Texture2D filledTexture;
        private Texture2D unfilledTexture;

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
        }

        public override void DrawGUI(SpriteBatch spriteBatch, GameTime gameTime)
        {
            progressBar.Draw(spriteBatch);
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

            //Load the hard-coded unknown texture
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

            filledTexture = CreateTexture(GraphicsDevice, 1, 4, c => f);
            unfilledTexture = CreateTexture(GraphicsDevice, 1, 4, c => u);

            loadTaskCount = filesToLoad.Count;
            progressBar = new(filledTexture, unfilledTexture, progressBarBox, loadTaskCount);

            ConfigLoader.Load();
        }

        public override void UnloadContent()
        {
            Console.WriteLine($"UNLOAD GameLoadingScreen");
            UIManager.Clear();
            filledTexture.Dispose();
            unfilledTexture.Dispose();
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
                    string name = "Debug Unit";
                    TextureCollection textureCollection = new();
                    textureCollection.AddTexture(AssetContainer.ReadTexture("sUnit_0_0"));
                    textureCollection.AddTexture(AssetContainer.ReadTexture("sUnit_0_1"));
                    textureCollection.AddTexture(AssetContainer.ReadTexture("sUnit_0_2"));
                    textureCollection.AddTexture(AssetContainer.ReadTexture("sUnit_0_3"));
                    Animation animation = new(textureCollection, 0);
                    Enemy.enemyAnims[name] = animation;

                    TextureCollection nukeTexture = new();
                    for (int i = 0; i < 244; i++) nukeTexture.AddTexture(AssetContainer.ReadTexture($"sNuke_{i}"));
                    AnimationStreamer.AddAnimation(new(nukeTexture, 30, AnimationPlayType.PAUSE_AT_END), "aNuke");

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
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\sellChecked.png", "sSellClick"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\sellUnchecked.png", "sSellUnlick"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\units\dev_0.png", "sUnit_0_0"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\units\dev_1.png", "sUnit_0_1"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\units\dev_2.png", "sUnit_0_2"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\units\dev_3.png", "sUnit_0_3"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\vig.png", "sVignette"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\icbm.png", "sICBM"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\tower_platform.png", "sPlatform"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers\tower_0.png", "tower_basic"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers_render\tower_0.png", "tower_basic_button"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers\tower_1.png", "tower_advanced"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers_render\tower_1.png", "tower_advanced_button"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers\tower_2.png", "tower_minigun"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers_render\tower_2.png", "tower_minigun_button"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers\tower_3.png", "tower_super"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers_render\tower_3.png", "tower_super_button"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers\tower_4.png", "tower_fire"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers_render\tower_4.png", "tower_fire_button"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers\tower_5.png", "tower_rocket"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers_render\tower_5.png", "tower_rocket_button"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers\tower_6.png", "tower_sniper"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers_render\tower_6.png", "tower_sniper_button"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers\tower_7.png", "tower_nuke"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers_render\tower_7.png", "tower_nuke_button"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers\tower_8.png", "tower_tesla"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers_render\tower_8.png", "tower_tesla_button"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers\tower_9.png", "tower_missile"));
            filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @"Assets\Textures\towers_render\tower_9.png", "tower_missile_button"));

            //Load loading texture
            for (int i = 0; i < 8; i++) filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, @$"Assets\Textures\load_{i}.png", $"sLoad_{i}"));

            //Load fire texture 
            for (int i = 0; i < 6; i++) filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, $@"Assets\Textures\fire_{i}.png", $"sFire_{i}"));

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

            //Load the nuke explosion textures
            for (int i = 0; i < 244; i++)
            {
                filesToLoad.Enqueue(new(LoadFile.TypeToLoad.TEXTURE, $@"Assets\Textures\nuke\nuke_{i}.png", $"sNuke_{i}"));
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