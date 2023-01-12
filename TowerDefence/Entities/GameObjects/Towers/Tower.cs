using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Entities.GameObjects.Towers
{
    internal sealed class Tower : Entity
    {
        private int rotation;

        public static Dictionary<string, TowerData> towerDatas;

        public int Rotation => rotation;

        public override Entity Deserialise(string serialised)
        {
            throw new NotImplementedException();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            throw new NotImplementedException();
        }

        public override byte GetID()
        {
            throw new NotImplementedException();
        }

        public override string Serialise()
        {
            throw new NotImplementedException();
        }

        public override void Update(GameTime gameTime)
        {

        }

        static Tower()
        {
            towerDatas = new();
        }
    }
}