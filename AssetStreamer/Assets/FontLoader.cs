using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace AssetStreamer.Assets
{
    public static class FontLoader
    {
        [DllImport("MessageBox.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        extern static void DisplayError(string message);

        public static void LoadFont(string path, string name, GraphicsDevice graphics)
        {
            if (!File.Exists(path))
            {
                DisplayError($"The file '{Path.GetFullPath(path)}' could not be found");
                Environment.Exit(1);
            }

            SpriteFont font = UILibrary.SpriteFontLoader.LoadFontFromFile(path, graphics);
            AssetContainer.AddFont(name, font);
        }
    }
}