using Microsoft.Xna.Framework.Audio;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace AssetStreamer
{
    public static class AssetContainer
    {
        [DllImport("MessageBox.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        extern static void DisplayError(string message);

        #region Containers

        private static readonly Dictionary<string, Texture2D> textures;

        private static readonly Dictionary<string, string> strings;

        private static readonly Dictionary<string, SpriteFont> fonts;

        private static readonly Dictionary<string, SoundEffect> sounds;

        #endregion

        #region Setters

        internal static void AddTexture(string name, Texture2D texture) => textures[name] = texture;

        internal static void AddString(string name, string value) => strings[name] = value;

        internal static void AddFont(string name, SpriteFont font) => fonts[name] = font;

        internal static void AddSound(string name, SoundEffect sound) => sounds[name] = sound;

        #endregion

        #region Getters

        public static Texture2D ReadTexture(string name)
        {
            if (!textures.ContainsKey(name))
            {
                DisplayError($"No texture with the name '{name}' has been loaded.");
                Environment.Exit(1);
            }

            return textures[name];
        }

        public static string ReadString(string name)
        {
            if (!strings.ContainsKey(name))
            {
                DisplayError($"No string with the name '{name}' has been loaded");
                Environment.Exit(1);
            }

            return strings[name];
        }

        public static SpriteFont GetFont(string name)
        {
            if (!fonts.ContainsKey(name))
            {
                DisplayError($"No font with the name '{name}' has been loaded");
                Environment.Exit(1);
            }

            return fonts[name];
        }

        public static SoundEffect GetSound(string name)
        {
            if (!sounds.ContainsKey(name))
            {
                DisplayError($"No sound with the name '{name}' has been loaded");
                Environment.Exit(1);
            }

            return sounds[name];
        }

        #endregion

        static AssetContainer()
        {
            textures = new();
            strings = new();
            fonts = new();
            sounds = new();
        }
    }
}