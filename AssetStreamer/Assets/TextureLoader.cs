using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace AssetStreamer.Assets
{
    public static class TextureLoader
    {
        [DllImport("MessageBox.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        extern static void DisplayError(string message);

        public static void LoadTexture(string path, string textureName, GraphicsDevice graphics)
        {
            if (!File.Exists(path))
            {
                DisplayError($"The file '{Path.GetFullPath(path)}' could not be found");
                Environment.Exit(1);
            }

            LoadTexture(Texture2D.FromFile(graphics, path), textureName);
        }

        public static void LoadTexture(Texture2D texture, string textureName) => AssetContainer.AddTexture(textureName, texture);
    }
}