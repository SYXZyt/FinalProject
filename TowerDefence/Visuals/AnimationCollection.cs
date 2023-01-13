using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Visuals
{
    /// <summary>
    /// Class which will handle objects which can have multiple animations
    /// </summary>
    internal sealed class AnimationCollection
    {
        private readonly Dictionary<string, (Animation anim, string animToPlayOnEnd)> loadedAnimations;

        private string currentAnim;

        /// <summary>
        /// Get the current animation being played
        /// </summary>
        public Animation CurrentAnim => currentAnim is not null ? loadedAnimations[currentAnim].anim : null;

        /// <summary>
        /// Get the current texture for the current animation
        /// </summary>
        public Texture2D GetCurrentTexture => CurrentAnim?.GetActiveFrame();

        /// <summary>
        /// Add an animation to the collection
        /// </summary>
        /// <param name="name">The name of the animation</param>
        /// <param name="animation">The animation to load</param>
        /// <param name="animToPlayOnEnd">Which animation to play when this one has finished. Pass null for no animation to play</param>
        public void AddAnimation(string name, Animation animation, string animToPlayOnEnd = null)
        {
            //Throw an error if the animation has already been loaded
            if (loadedAnimations.ContainsKey(name)) throw new($"Animation {name} has already been loaded");

            loadedAnimations[name] = (animation, animToPlayOnEnd);
        }

        /// <summary>
        /// Set a new current animation
        /// </summary>
        /// <param name="name">The name of the next animation</param>
        public void SetCurrentAnimation(string name)
        {
            if (!loadedAnimations.ContainsKey(name)) throw new($"Animation {name} could not be found");

            CurrentAnim?.Reset();
            currentAnim = name;
        }

        /// <summary>
        /// Update the animation currently playing
        /// </summary>
        public void Update(GameTime gameTime)
        {
            //If we do not have an anim, we don't have anything to update
            if (currentAnim is null) return;

            CurrentAnim.Update(gameTime);

            //Check if the animation has ended
            if (CurrentAnim.HasEnded && loadedAnimations[currentAnim].animToPlayOnEnd is not null)
            {
                currentAnim = loadedAnimations[currentAnim].animToPlayOnEnd;
            }
        }

        public AnimationCollection()
        {
            currentAnim = "";
            loadedAnimations = new();
        }
    }
}