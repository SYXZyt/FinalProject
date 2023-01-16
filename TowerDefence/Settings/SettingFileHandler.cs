using System.Net;
using TowerDefence.Settings.ConfigParsing;

namespace TowerDefence.Settings
{
    internal static class SettingFileHandler
    {
        private const string filename = "settings.cfg";
        private static readonly string path = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\SYXZSoft\MultiplayerTowerDefence";

        public static bool DoesSettingsFileExist() => File.Exists($@"{path}\{filename}");

        public static void SaveSettingsFile()
        {
            if (!DoesSettingsFileExist()) File.Create($@"{path}\{filename}");

            using CfgWriter writer = new($@"{path}\{filename}");

            writer.Write("fullscreen", GlobalSettings.Fullscreen ? "true" : "false");
            writer.Write("ip", GlobalSettings.ServerIP.ToString());
            writer.Write("port", GlobalSettings.Port);
            writer.Write("playerLeft", GlobalSettings.PlayerOnRight ? "false" : "true");
            writer.Close();
        }

        public static void LoadDefaultSettings()
        {
            //Delete the current settings file and then load the default one onto disk
            if (DoesSettingsFileExist())
            {
                File.Delete($@"{path}\{filename}");
                LoadSettingsFile();
            }
        }

        public static void LoadSettingsFile()
        {
            //Check if there is a settings file that exists, if not load hard coded settings, then create a setting file
            if (DoesSettingsFileExist())
            {
                //Begin reading the file
                using CfgParser configParser = new($@"{path}\{filename}");

                Console.WriteLine($@"Loading settings from {path}\{filename}");

                CfgResult cfrFullscreen = configParser.Read("fullscreen");
                CfgResult cfrServerIp = configParser.Read("ip");
                CfgResult cfrServerPort = configParser.Read("port");
                CfgResult cfrPlayerLeft = configParser.Read("playerLeft");

                //Make sure that all of the data read is correct
                if (cfrFullscreen.type != CfgType.STR) throw new CfgIncorrectType("fullscreen");
                if (cfrServerIp.type != CfgType.STR) throw new CfgIncorrectType("ip");
                if (cfrServerPort.type != CfgType.I16) throw new CfgIncorrectType("port");
                if (cfrPlayerLeft.type != CfgType.STR) throw new CfgIncorrectType("cfrPlayerLeft");

                GlobalSettings.Fullscreen = (cfrFullscreen.result as string) == "true";
                GlobalSettings.Port = (int)cfrServerPort.result;
                GlobalSettings.ServerIP = IPAddress.Parse((string)cfrServerIp.result);
                GlobalSettings.PlayerOnRight = (cfrPlayerLeft.result as string) == "false";
                GlobalSettings.ApplySettings();
                configParser.Close();
            }
            else
            {
                //Hard code settings, save to the settings file, then finally recurse to load the settings
                using CfgWriter writer = new($@"{path}\{filename}");

                writer.Write("fullscreen", "true");
                writer.Write("ip", "86.141.88.49");
                writer.Write("port", 9050);
                writer.Write("playerLeft", "false");

                writer.Close();
                LoadSettingsFile();
            }
        }
    }
}