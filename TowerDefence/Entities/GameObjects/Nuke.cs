using UILibrary;
using TowerDefence.Visuals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Entities.GameObjects
{
    internal class Nuke : Entity
    {
        public static byte ID = 0;
        private readonly Animation frames;
        private float darkness;
        private float opacity;

        private const int FadeFrame = 115;

        public override Entity Deserialise(string serialised) => null;

        public override void Draw(SpriteBatch spriteBatch)
        {
            byte v = (byte)(255 - this.darkness);
            Color darkness = new(v, v, v);
            frames.GetActiveFrame().Draw(position, spriteBatch, darkness * opacity);
        }

        public override byte GetID() => ID;

        public override string Serialise() => string.Empty;

        public override void Update(GameTime gameTime)
        {
            frames.Update(gameTime);

            if (frames.FrameIndex >= FadeFrame)
            {
                opacity = (float)Math.Max(0, opacity - 1f * gameTime.ElapsedGameTime.TotalSeconds);
                darkness -= 0.01f;
            }

            if (opacity == 0) markForDeletion = true;
        }

        public Nuke(Vector2 position, bool ownership)
        {
            this.ownership = ownership;
            frames = AnimationStreamer.ReadAnimation("aNuke").Copy();

            Vector2 v = new(frames.GetFrame(0).Width / 2, frames.GetFrame(0).Height / 2);
            this.position = position - v;
            opacity = 1;
            darkness = 0;

            Scenes.Game.Instance.NukeFlashOpacity = 1f;
        }
    }
}