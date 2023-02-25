using UILibrary;
using TowerDefence.Visuals;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace TowerDefence.Entities.GameObjects
{
    internal class RocketExplosion : Entity
    {
        private readonly Animation frames;

        public override Entity Deserialise(string serialised)
        {
            throw new NotImplementedException();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            frames.GetActiveFrame().Draw(position, spriteBatch, Color.White);
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
            frames.Update(gameTime);

            if (frames.HasEnded) markForDeletion = true;

            //Get all enemies and check for collision
            foreach (Enemies.Enemy e in Scenes.Game.Instance.Entities.OfType<Enemies.Enemy>())
            {
                if (aabb.CollisionCheck(e.AABB) && !e.MarkForDeletion && e.Ownership == ownership)
                {
                    e.Damage(5);
                    break;
                }
            }
        }

        public RocketExplosion(Vector2 position, bool ownership)
        {
            this.ownership = ownership;
            frames = AnimationStreamer.ReadAnimation("aExplosion").Copy();

            this.position = new(position.X - frames.GetFrame(0).Width / 2, position.Y - frames.GetFrame(0).Height / 2);
            aabb = new((short)this.position.X, (short)this.position.Y, (short)frames.GetFrame(0).Width, (short)frames.GetFrame(0).Height);
        }
    }
}