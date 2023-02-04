using UILibrary;
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

#if DEBUG
        private readonly List<Vector2> spawnPos = new();
#endif

        public double Rotation => rotation;
        public TowerData Data => data;

#if DEBUG
        //This method is just for debugging purposes
        private static void DrawLine(SpriteBatch spriteBatch, Vector2 begin, Vector2 end, Color color, int width = 1)
        {
            Rectangle r = new((int)begin.X, (int)begin.Y, (int)(end - begin).Length() + width, width);
            Vector2 v = Vector2.Normalize(begin - end);
            float angle = (float)Math.Acos(Vector2.Dot(v, -Vector2.UnitX));
            if (begin.Y > end.Y) angle = MathHelper.TwoPi - angle;
            Texture2D x = Extension.CreateTexture(UILibrary.Scenes.SceneManager.Instance.GraphicsDevice, 1, 1, c => Color.Magenta);
            spriteBatch.Draw(x, r, null, color, angle, Vector2.Zero, SpriteEffects.None, 0);
        }

        private static void DrawLineByAngle(SpriteBatch spriteBatch, Vector2 begin, float angle, float length, Color color, int width = 1)
        {
            Vector2 end = begin + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * length;
            Rectangle r = new((int)begin.X, (int)begin.Y, (int)length + width, width);
            Texture2D x = Extension.CreateTexture(UILibrary.Scenes.SceneManager.Instance.GraphicsDevice, 1, 1, c => Color.Magenta);
            spriteBatch.Draw(x, r, null, color, angle, Vector2.Zero, SpriteEffects.None, 0);
        }

#endif

        public override Entity Deserialise(string serialised)
        {
            throw new NotImplementedException();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Texture2D texture = anim.GetCurrentTexture;
            Vector2 originOffset = new(8);
            spriteBatch.Draw(texture, position + drawOffset + originOffset, null, Color.White, (float)rotation, originOffset, Vector2.One, SpriteEffects.None, 0f);

#if DEBUG
            if (enemiesInRange.Any())
            {
                Enemy closest = enemiesInRange.OrderBy(x => x.TotalDistance).ToArray()[^1];

                //foreach (Vector2 pos in spawnPos) DrawLineByAngle(spriteBatch, pos, (float)(rotation - (Math.PI / 2)), 100, Color.White);
            }
#endif
        }

        public override byte GetID() => data.id;

        public override string Serialise()
        {
            return $"|0,{position.X},{position.Y},{rotation},{data.id}";
        }

        private void SpawnBullet()
        {
#if DEBUG
            spawnPos.Clear();
#endif

            #region Spawn Methods
            void SpawnSingleBullet()
            {
                Vector2 originOffset = new(8);
                Vector2 bulletOriginOffset = new(-2);
                Bullet bullet = new(position + drawOffset + originOffset + bulletOriginOffset, rotation);
                Game.Instance.AddEntity(bullet); //Hand off ownership of the bullet
                elapsedTime = 0;

#if DEBUG
                spawnPos.Add(position + drawOffset + originOffset + bulletOriginOffset);
#endif
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

                Bullet bA = new(bulletA, rotation);
                Bullet bB = new(bulletB, rotation);

                Game.Instance.AddEntity(bA);
                Game.Instance.AddEntity(bB);
                elapsedTime = 0;

#if DEBUG
                spawnPos.Add(bulletA);
                spawnPos.Add(bulletB);
#endif
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
                default: break;
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

            elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
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