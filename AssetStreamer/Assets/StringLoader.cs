using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace AssetStreamer.Assets
{
    public static class StringLoader
    {
        [DllImport("MessageBox.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        extern static void DisplayError(string message);

        public static void LoadString(string translationFile)
        {
            if (!File.Exists(translationFile))
            {
                DisplayError($"The file '{Path.GetFullPath(translationFile)}' could not be found");
                Environment.Exit(1);
            }

            using StreamReader reader = new(translationFile);

            while (!reader.EndOfStream)
            {
                string fullLine = reader.ReadLine();
                string[] split = fullLine.Split("||");

                if (split.Length != 2)
                {
                    DisplayError($"String '{fullLine}' is malformed and not a valid format");
                    Environment.Exit(1);
                }

                AssetContainer.AddString(split[0], split[1]);
            }

            reader.Close();
        }
    }
}
