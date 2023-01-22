using UILibrary;
using AssetStreamer;
using Microsoft.Xna.Framework;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Entities.GameObjects
{
    internal class Bullet : Entity
    {
        private readonly double direction;
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
            double d = direction;
            float x = position.X + texture.Width / 2;
            float y = position.Y + texture.Width / 2;

            y -= (float)(BulletSpeed * gameTime.ElapsedGameTime.TotalSeconds * Math.Cos(d));
            x += (float)(BulletSpeed * gameTime.ElapsedGameTime.TotalSeconds * Math.Sin(d));

            position = new(x, y);

            //Update the hitbox
            aabb.Move(new(x, y));

            //Get all enemies and check for collision
            foreach (Enemies.Enemy e in Scenes.Game.Instance.Entities.OfType<Enemies.Enemy>())
            {
                if (aabb.CollisionCheck(e.AABB) && !e.MarkForDeletion)
                {
                    e.Damage();
                    markForDeletion = true;
                    break;
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Vector2 offset = new(texture.Width / 2, texture.Height / 2);
            texture.Draw(position + offset, spriteBatch, Color.White);
        }

        [SuppressMessage("Style", "IDE0002")]
        public Bullet(Vector2 position, double direction) : base()
        {
            base.position = position;
            base.textures = new();

            texture = AssetContainer.ReadTexture(TextureName);
            this.direction = direction;
            aabb = new((short)position.X, (short)position.Y, (short)(texture.Width*1.2), (short)(texture.Height*1.2));
        }
    }
}