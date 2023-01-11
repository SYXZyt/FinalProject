namespace MapEditor
{
    internal static class Packer
    {
        public static void PackData(string fname, int[,] data)
        {
            using BinaryWriter writer = new(File.Open(fname, FileMode.Create));

            for (int y = 0; y < 42; y++)
            {
                for (int x = 0; x < 48; x++)
                {
                    byte b = (byte)data[x, y];
                    writer.Write(b);
                }
            }

            writer.Flush();
            writer.Close();
        }

        public static int[,] UnpackData(string fname)
        {
            using BinaryReader reader = new(File.OpenRead(fname));
            int[,] data = new int[48, 42];

            for (int y = 0; y < 42; y++)
            {
                for (int x = 0; x < 48; x++)
                {
                    byte b = reader.ReadByte();
                    data[x, y] = b;
                }
            }

            return data;
        }
    }
}