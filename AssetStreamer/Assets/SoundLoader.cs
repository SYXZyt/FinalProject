using Microsoft.Xna.Framework.Audio;
using System.Runtime.InteropServices;

namespace AssetStreamer.Assets
{
    public static class SoundLoader
    {
        [DllImport("MessageBox.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        static extern void DisplayError(string message);

        public static void LoadSound(string path, string name)
        {
            if (!File.Exists(path))
            {
                DisplayError($"The file '{Path.GetFullPath(path)}' can not be found");
                Environment.Exit(0);
            }

            SoundEffect sound = SoundEffect.FromFile(path);
            AssetContainer.AddSound(name, sound);
        }
    }
}