namespace TowerDefence.Visuals
{
    internal sealed class AnimationStreamer
    {
        private static readonly Dictionary<string, Animation> animations;

        public static Animation ReadAnimation(string anim)
        {
            if (!animations.ContainsKey(anim))
            {
                MessageBox.Display($"Animation '{anim}' has not been loaded");
                return null;
            }

            return animations[anim];
        }

        public static void AddAnimation(Animation animation, string name) => animations[name] = animation;

        static AnimationStreamer()
        {
            animations = new();
        }
    }
}