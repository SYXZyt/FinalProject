using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Visuals
{
    internal sealed class Animation
    {
        private readonly TextureCollection frames;
        private int frame;
        private readonly int animationSpeed; /*(in frames)*/
        private readonly int globalFrameBegin;
        private int tick;

        public bool HasEnded => frame == -1;

        public Texture2D GetActiveFrame() => frames[frame == -1 ? frames.Count - 1 : frame];

        public void Update()
        {
            //Check if we should update the animation
            if (frame == -1) return; //If the animation has already ended, we don't need to do anything

            //Check if we need to update the frame
            if (globalFrameBegin + tick % animationSpeed == 0) frame++;

            tick++;
        }

        public Animation(TextureCollection frames, int animationSpeed, int currentGlobalFrame = 0)
        {
            tick = 0;
            this.frames = frames;
            frame = 0;
            this.animationSpeed = animationSpeed;
            globalFrameBegin = currentGlobalFrame;
        }
    }
}