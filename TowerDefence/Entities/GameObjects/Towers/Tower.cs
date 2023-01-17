using UILibrary;
using TowerDefence.Visuals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Entities.GameObjects.Towers
{
    internal sealed class Tower : Entity
    {
        private int rotation;
        private TowerData data;
        private readonly AnimationCollection anim;
        private readonly Vector2 drawOffset;

        public static Dictionary<int, TowerData> towerDatas;

        public int Rotation => rotation;
        public TowerData Data => data;

        public override Entity Deserialise(string serialised)
        {
            throw new NotImplementedException();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Texture2D texture = anim.GetCurrentTexture;
            texture.Draw(position + drawOffset, spriteBatch, Color.White);
        }

        public override byte GetID() => data.id;

        public override string Serialise()
        {
            return $"|0,{position.X},{position.Y},{rotation},{data.id}";
        }

        public override void Update(GameTime gameTime)
        {
            //Loop over every enemy and check if any are within range
        }

        public Tower(int id, Vector2 position, Animation idleAnim, Vector2 drawOffset)
        {
            rotation = 0;
            this.position = position;

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