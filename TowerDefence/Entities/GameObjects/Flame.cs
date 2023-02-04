using UILibrary;
using TowerDefence.Visuals;
using TowerDefence.Component;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Entities.GameObjects
{
    internal class Flame : Entity
    {
        private new readonly Animation textures;
        private float totalTime = 0f;
        private readonly float life = 2f;

        (int width, int height) tsize;

        public static byte ID = 1;

        public override Entity Deserialise(string serialised)
        {
            throw new NotImplementedException();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            textures.GetActiveFrame().Draw(position, spriteBatch, Color.White);
        }

        public override byte GetID() => ID;

        public override string Serialise()
        {
            throw new NotImplementedException();
        }

        public override void Update(GameTime gameTime)
        {
            textures.Update(gameTime);
            totalTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (totalTime * life > 1f) MarkForDeletion = true;

            foreach (Enemies.Enemy e in Scenes.Game.Instance.Entities.OfType<Enemies.Enemy>())
            {
                if (aabb.CollisionCheck(e.AABB) && !e.MarkForDeletion)
                {
                    if (!e.HasComponent<FireDamage>()) e.AddDamageComponent(new FireDamage());
                }
            }
        }

        public Flame(Vector2 position, bool ownership)
        {
            this.position = position;
            base.ownership = ownership;

            Visuals.TextureCollection textureCollection = new();
            for (int i = 0; i < 6; i++) textureCollection.AddTexture(AssetStreamer.AssetContainer.ReadTexture($"sFire_{i}"));
            textures = new(textureCollection, 15f, AnimationPlayType.LOOP);

            tsize = (textures.GetFrame(0).Width, textures.GetFrame(0).Height);
            aabb = new((short)position.X, (short)position.Y, (short)(tsize.width * 1.2), (short)(tsize.height * 1.2));
        }
    }
}