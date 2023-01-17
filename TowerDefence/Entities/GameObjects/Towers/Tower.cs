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
        private float rotation;
        private TowerData data;
        private readonly AnimationCollection anim;
        private readonly Vector2 drawOffset;

        private readonly List<(Enemy enemyObj, float dist)> enemiesInRange;

        public static Dictionary<int, TowerData> towerDatas;

        public int Rotation => (int)rotation;
        public TowerData Data => data;

        public override Entity Deserialise(string serialised)
        {
            throw new NotImplementedException();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Texture2D texture = anim.GetCurrentTexture;
            Vector2 originOff = new(8);
            spriteBatch.Draw(texture, position + drawOffset + originOff, null, Color.White, rotation, new(8, 8), Vector2.One, SpriteEffects.None, 0f);
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

            Console.WriteLine(enemiesInRange.Count);

            foreach (Enemy e in allEnemies)
            {
                //Check if this object is within range
                //Here we want screen coords so we need to use some offsets
                float xDiff = position.X + drawOffset.X - (e.GetScreenPosition().X + 8);
                float yDiff = position.Y + drawOffset.Y - (e.GetScreenPosition().Y + 8);

                //Perform some single Pythagoras to calculate the distance
                float dist = (float)Math.Sqrt((xDiff * xDiff) + (yDiff * yDiff));

                //If the object is within range, add it to the list
                if (dist <= data.range) enemiesInRange.Add((e, dist));
            }

            //Now if we are have an enemy in range, we then need to get the closest and perform some trigonometry to get the angle to the object
            if (enemiesInRange.Count > 0)
            {
                Enemy closest = enemiesInRange.OrderBy(x => x.dist).ToArray()[0].enemyObj;
                float x1 = position.X + drawOffset.X;
                float y1 = position.Y + drawOffset.Y;

                float x2 = closest.GetScreenPosition().X + 8;
                float y2 = closest.GetScreenPosition().Y + 8;

                float radAng = (float)Math.Atan2(y1 - y2, x1 - x2);
                rotation = radAng - 90f; //Adjust the offset since 0 degrees in radians would be east
            }
        }

        public Tower(int id, Vector2 position, Animation idleAnim, Vector2 drawOffset)
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
        }

        static Tower()
        {
            towerDatas = new();
        }
    }
}