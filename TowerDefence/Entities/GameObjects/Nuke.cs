using UILibrary;
using AssetStreamer;
using TowerDefence.Visuals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TowerDefence.Entities.GameObjects.Enemies;

namespace TowerDefence.Entities.GameObjects
{
    internal class Nuke : Entity
    {
        private enum NukeState
        {
            LAUNCH,
            ATTACK,
        }

        public static byte ID = 0;
        private readonly Animation frames;
        private readonly Texture2D icbmTexture;
        private float darkness;
        private float opacity;
        private Vector2 targetPosition;
        private NukeState state;
        private ulong tick;

        private const int FadeFrame = 115;

        public override Entity Deserialise(string serialised) => null;

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (state == NukeState.ATTACK)
            {
                byte v = (byte)(255 - this.darkness);
                Color darkness = new(v, v, v);
                frames.GetActiveFrame().Draw(targetPosition, spriteBatch, darkness * opacity);
            }
            else
            {
                icbmTexture.Draw(position, spriteBatch, Color.White);
            }
        }

        public override byte GetID() => ID;

        public override string Serialise() => string.Empty;

        public override void Update(GameTime gameTime)
        {
            if (state == NukeState.ATTACK)
            {
                if (tick == 0)
                {
                    Scenes.Game.Instance.NukeFlashOpacity = 1f;

                    //Damage all enemies
                    List<Enemy> enemies = Scenes.Game.Instance.Entities.OfType<Enemy>().Where(e => e.Ownership == ownership).ToList();

                    foreach (Enemy e in enemies) e.Damage(150);
                }

                tick++;

                frames.Update(gameTime);

                if (frames.FrameIndex >= FadeFrame)
                {
                    opacity = (float)Math.Max(0, opacity - 1f * gameTime.ElapsedGameTime.TotalSeconds);
                    darkness -= 0.01f;
                }

                if (opacity == 0) markForDeletion = true;
            }
            else
            {
                position = new Vector2(position.X, (float)(position.Y - 256f * gameTime.ElapsedGameTime.TotalSeconds));

                if (position.Y < -96) state = NukeState.ATTACK;
            }
        }

        public Nuke(Vector2 targetPosition, Vector2 launchPosition, bool ownership)
        {
            this.ownership = ownership;
            frames = AnimationStreamer.ReadAnimation("aNuke").Copy();
            icbmTexture = AssetContainer.ReadTexture("sICBM");

            position = launchPosition;

            Vector2 vb = new(frames.GetFrame(0).Width / 2, frames.GetFrame(0).Height / 2);
            this.targetPosition = targetPosition - vb;
            opacity = 1;
            darkness = 0;
            state = NukeState.LAUNCH;
            tick = 0;
        }
    }
}