using UILibrary;
using AssetStreamer;
using Microsoft.Xna.Framework;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Entities.GameObjects
{
    internal class Bullet : Entity
    {
        private readonly ushort direction;
        private readonly Texture2D texture;

        public static float BulletSpeed = 3f;
        public static string TextureName = string.Empty;
        public static byte ID = 0;

        public override byte GetID() => ID;

        public override Entity Deserialise(string serialised)
        {
            throw new NotImplementedException();
        }

        public override string Serialise()
        {
            throw new NotImplementedException();
        }

        public override void Update(GameTime gameTime)
        {
            float d = direction;
            float x = position.X;
            float y = position.Y;

            y -= (float)(BulletSpeed * gameTime.ElapsedGameTime.TotalSeconds * Math.Cos(d * Math.PI / 180f));
            x += (float)(BulletSpeed * gameTime.ElapsedGameTime.TotalSeconds * Math.Sin(d * Math.PI / 180f));

            position = new(x, y);
        }

        public override void Draw(SpriteBatch spriteBatch) => texture.Draw(position, spriteBatch, Color.White);

        [SuppressMessage("Style", "IDE0002")]
        public Bullet(Vector2 position, ushort direction) : base()
        {
            base.position = position;
            base.textures = new();

            texture = AssetContainer.ReadTexture(TextureName);
            this.direction = direction;
        }
    }
}