﻿using UILibrary;
using TowerDefence.Scenes;
using TowerDefence.Visuals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TowerDefence.Entities.GameObjects.Enemies;

using Game = TowerDefence.Scenes.Game;

namespace TowerDefence.Entities.GameObjects.Towers
{
    internal sealed class Tower : Entity
    {
        private double rotation;
        private TowerData data;
        private readonly AnimationCollection anim;
        private readonly Vector2 drawOffset;

        private float elapsedTime;

        private readonly List<Enemy> enemiesInRange;

        public static Dictionary<int, TowerData> towerDatas;

        public double Rotation => rotation;
        public TowerData Data => data;

        public override Entity Deserialise(string serialised)
        {
            throw new NotImplementedException();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Texture2D texture = anim.GetCurrentTexture;
            Vector2 originOffset = new(8);
            spriteBatch.Draw(texture, position + drawOffset + originOffset, null, Color.White, (float)rotation, originOffset, Vector2.One, SpriteEffects.None, 0f);
        }

        public override byte GetID() => data.id;

        public override string Serialise()
        {
            return $"|0,{position.X},{position.Y},{rotation},{data.id}";
        }

        private void SpawnBullet()
        {
            #region Spawn Methods
            void SpawnSingleBullet()
            {
                Vector2 originOffset = new(8);
                Vector2 bulletOriginOffset = new(-2);
                Bullet bullet = new(position + drawOffset + originOffset + bulletOriginOffset, rotation, ownership);
                Game.Instance.AddEntity(bullet); //Hand off ownership of the bullet
                elapsedTime = 0;
            }

            void SpawnDoubleBullet()
            {
                //For the double bullet we need to do a little bit of maths
                Vector2 originOffset = new(8);
                Vector2 centre = position + drawOffset + originOffset;

                (int width, int height) = (anim.GetCurrentTexture.Width, anim.GetCurrentTexture.Height);

                Vector2 halfSize = new(width / 2, height / 2);

                Matrix rotationMatrix = Matrix.CreateRotationZ((float)rotation);

                Vector2 bulletA = Vector2.Transform(new Vector2(-halfSize.X, 0), rotationMatrix) + centre;
                Vector2 bulletB = Vector2.Transform(new Vector2(halfSize.X, 0), rotationMatrix) + centre;

                Bullet bA = new(bulletA, rotation, ownership);
                Bullet bB = new(bulletB, rotation, ownership);

                Game.Instance.AddEntity(bA);
                Game.Instance.AddEntity(bB);
                elapsedTime = 0;
            }

            void SpawnInstaBullet()
            {
                Enemy[] enimies = enemiesInRange.OrderBy(x => x.TotalDistance).ToArray();
                for (int i = enimies.Length - 1; i >= 0; i--)
                {
                    if (!enimies[i].DamagedThisFrame)
                    {
                        enimies[i].Damage(1);
                        elapsedTime = 0;
                        break;
                    }
                }
            }

            void SpawnFireRound()
            {
                Enemy closest = enemiesInRange.OrderBy(x => x.TotalDistance).ToArray()[^1];
                Vector2 position = closest.GetPosition() * Game.TileSize;
                position += ownership ? Game.Instance.PlayerGameOffset : Game.Instance.OpponentGameOffset;

                Vector2 rngOff = new(Game.Instance.RNG.Next(-6, 7), Game.Instance.RNG.Next(-6, 7));
                position += rngOff;
                Flame flame = new(position, ownership);
                Game.Instance.AddEntity(flame);
                elapsedTime = 0;
            }

            void SpawnNuke()
            {
                Enemy closest = enemiesInRange.OrderBy(x => x.TotalDistance).ToArray()[^1];
                Vector2 position = closest.GetPosition() * Game.TileSize;
                position += ownership ? Game.Instance.PlayerGameOffset : Game.Instance.OpponentGameOffset;

                Nuke nuke = new(position, this.position + drawOffset, ownership);
                Game.Instance.AddEntity(nuke);
                elapsedTime = 0;
            }
            #endregion

            switch (data.projectile)
            {
                case "bullet":
                {
                    SpawnSingleBullet();
                }
                break;
                case "bullet_double":
                {
                    SpawnDoubleBullet();
                }
                break;
                case "bullet_triple":
                {
                    SpawnSingleBullet();
                    SpawnDoubleBullet();
                }
                break;
                case "fire":
                {
                    SpawnFireRound();
                }
                break;
                case "bullet_instant":
                {
                    SpawnInstaBullet();
                }
                break;
                case "nuke":
                {
                    SpawnNuke();
                }
                break;
                default:
                {
                    Console.WriteLine($"[WARNING] Tower attack '{data.projectile}' has no implementation");
                }
                break;
            }
        }

        public override void Update(GameTime gameTime)
        {
            //Loop over every enemy and check if any are within range
            Enemy[] allEnemies = Game.Instance.Entities.OfType<Enemy>().ToArray();
            enemiesInRange.Clear();

            foreach (Enemy e in allEnemies)
            {
                //Check if this object is within range
                //Here we want screen coords so we need to use some offsets
                Vector2 originOffset = new(8);
                Vector2 towerPos = position + drawOffset + originOffset;
                Vector2 enemyPos = e.GetScreenPosition() + originOffset;

                float dist = Vector2.Distance(towerPos, enemyPos);

                //If the object is within range, add it to the list and if it is our players enemy
                if (dist < data.range && e.Ownership == ownership)
                {
                    enemiesInRange.Add(e);
                }
            }

            //Now if we are have an enemy in range, we then need to get the closest and perform some trigonometry to get the angle to the object
            if (enemiesInRange.Count > 0)
            {
                elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                Enemy closest = enemiesInRange.OrderBy(x => x.TotalDistance).ToArray()[^1];

                //Calculate positions and stuff
                Vector2 originOffset = new(8);
                Vector2 towerPos = position + drawOffset + originOffset;
                Vector2 enemyPos = closest.GetScreenPosition() + originOffset;

                Vector2 v = Vector2.Normalize(enemyPos - towerPos);
                double angle = Math.Acos(Vector2.Dot(v, Vector2.UnitY));
                if (towerPos.X < enemyPos.X) angle = -angle;
                angle += MathHelper.Pi; //The tower has a 180 degree offset so we need to offset it by PI (PI = 180 degrees in radians)

                rotation = angle;

                //Fire at enemy
                if (elapsedTime * data.rate >= 1)
                {
                    SpawnBullet();
                }
            }

            if (!data.rotate) rotation = 0;
        }


        public Tower(int id, Vector2 position, Animation idleAnim, Vector2 drawOffset, bool ownership)
        {
            rotation = 0;
            this.position = position;
            enemiesInRange = new();

            anim = new();
            anim.AddAnimation("state_idle", idleAnim);
            anim.SetCurrentAnimation("state_idle");

            if (!towerDatas.ContainsKey(id)) throw new($"No tower called with id {id} found");
            data = towerDatas[id];
            this.drawOffset = drawOffset;
            elapsedTime = 0;
            base.ownership = ownership;
        }

        static Tower()
        {
            towerDatas = new();
        }
    }
}