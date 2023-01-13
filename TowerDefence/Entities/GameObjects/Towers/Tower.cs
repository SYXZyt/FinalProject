using TowerDefence.Visuals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Entities.GameObjects.Towers
{
    internal sealed class Tower : Entity
    {
        private int rotation;
        private TowerData data;
        private AnimationCollection anim;

        public static Dictionary<string, TowerData> towerDatas;

        public int Rotation => rotation;
        public TowerData Data => data;

        public override Entity Deserialise(string serialised)
        {
            throw new NotImplementedException();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            throw new NotImplementedException();
        }

        public override byte GetID() => data.id;

        public override string Serialise()
        {
            throw new NotImplementedException();
        }

        public override void Update(GameTime gameTime)
        {
            //Loop over every enemy and check if any are within range
        }

        public Tower(string name)
        {
            rotation = 0;

            if (!towerDatas.ContainsKey(name)) throw new($"No tower called '{name}' found");
            data = towerDatas[name];
        }

        static Tower()
        {
            towerDatas = new();
        }
    }
}