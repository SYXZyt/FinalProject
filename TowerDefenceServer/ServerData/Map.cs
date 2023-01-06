﻿namespace TowerDefenceServer.ServerData
{
    internal class Map : ISerialisable
    {
        public const int MapWidth = 42;
        public const int MapHeight = 48;

        private readonly byte[,] mapData;

        public byte this[int x, int y] => mapData[x, y];

        public byte[] Serialise()
        {
            byte[] bytes = new byte[MapWidth * MapHeight];

            int i = 0;
            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    bytes[i++] = this[x, y];
                }
            }

            return bytes;
        }

        public static Map LoadFromDisk(string filename)
        {
            Map map = new();

            if (!File.Exists(filename)) return null;

            using BinaryReader reader = new(File.OpenRead(filename));
            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    byte b = reader.ReadByte();
                    map.mapData[x, y] = b;
                }
            }


            reader.Close();
            return map;
        }

        public Map()
        {
            mapData = new byte[MapWidth, MapHeight];
        }

        public Map(byte[,] mapData)
        {
            this.mapData = mapData;
        }
    }
}