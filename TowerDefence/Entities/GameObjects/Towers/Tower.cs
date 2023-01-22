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
                    Console.WriteLine($"{dist}/{data.range}");
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
                    //Bullets use degrees where as the tower uses radians, so we need to convert
                    Bullet bullet = new(position + drawOffset, rotation);
                    Game.Instance.AddEntity(bullet); //Hand off ownership of the bullet
                    elapsedTime = 0;
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