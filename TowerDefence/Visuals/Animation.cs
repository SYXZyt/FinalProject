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
        private readonly int animationSpeed; /*(in frames)*/
        private readonly int globalFrameBegin;
        private int tick;
        private readonly AnimationPlayType playType;
        private bool freeze;

        public bool HasEnded => frame == -1;

        public Texture2D GetActiveFrame() => frames[frame == -1 ? frames.Count - 1 : frame];

        public void SetFreeze(bool freeze) => this.freeze = freeze;

        public void Update()
        {
            //Check if we should update the animation
            if (frame == -1 || freeze) return; //If the animation has already ended, we don't need to do anything

            //Check if we need to update the frame
            if (globalFrameBegin + tick % animationSpeed == 0) frame++;

            //Check for end of animation
            if (frame >= frames.Count)
            {
                if (playType == AnimationPlayType.PAUSE_AT_END) frame = -1;
                else if (playType == AnimationPlayType.LOOP) frame = 0;
            }

            tick++;
        }

        public void Reset()
        {
            frame = 0;
            tick = 0;
        }

        public Animation(TextureCollection frames, int animationSpeed,  int currentGlobalFrame = 0, AnimationPlayType playType = AnimationPlayType.PAUSE_AT_END)
        {
            tick = 0;
            this.frames = frames;
            frame = 0;
            this.animationSpeed = animationSpeed;
            globalFrameBegin = currentGlobalFrame;
            this.playType = playType;
        }
    }
}