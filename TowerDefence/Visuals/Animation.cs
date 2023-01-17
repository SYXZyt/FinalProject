using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Visuals
{
    internal enum AnimationPlayType
    {
        PAUSE_AT_END,
        LOOP,
    }

    internal sealed class Animation
    {
        private readonly TextureCollection frames;
        private int frame;
        private readonly float animationSpeed; /*(in seconds)*/
        private float elapsedTime;
        private readonly AnimationPlayType playType;
        private bool freeze;

        public bool HasEnded => frame == -1;

        public Texture2D GetFrame(int frame) => frames[frame];

        public Texture2D GetActiveFrame() => frames[frame == -1 ? frames.Count - 1 : frame];

        public void SetFreeze(bool freeze) => this.freeze = freeze;

        public void Update(GameTime gameTime)
        {
            //Check if we should update the animation
            if (frame == -1 || freeze) return; //If the animation has already ended, we don't need to do anything

            //Check if we need to update the frame
            elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (elapsedTime * animationSpeed >= 1)
            {
                elapsedTime = 0;
                frame++;
            }

            //Check for end of animation
            if (frame >= frames.Count)
            {
                if (playType == AnimationPlayType.PAUSE_AT_END) frame = -1;
                else if (playType == AnimationPlayType.LOOP) frame = 0;
            }
        }

        public void Reset()
        {
            frame = 0;
            elapsedTime = 0;
        }

        public Animation(TextureCollection frames, float animationSpeed, AnimationPlayType playType = AnimationPlayType.PAUSE_AT_END)
        {
            elapsedTime = 0;
            this.frames = frames;
            frame = 0;
            this.animationSpeed = animationSpeed;
            this.playType = playType;
        }
    }
}
