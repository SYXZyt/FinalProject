using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Visuals
{
    internal class TextureCollection
    {
        private readonly List<Texture2D> textures;
        private readonly Random rng;

        public Texture2D this[int i] => textures[i];

        public int Count => textures.Count;

        public void AddTexture(Texture2D texture) => textures.Add(texture);

        public Texture2D GetRandom() => textures[rng.Next(textures.Count)];

        public TextureCollection()
        {
            textures = new();
            rng = new();
        }
    }
}