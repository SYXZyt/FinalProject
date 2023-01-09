namespace MapPacker
{
    internal class Program
    {
        private static byte[] ReadCSV(StreamReader reader)
        {
            List<byte> bytes = new();

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                string[] values = line.Split(",")[0..^1];
                byte[] row = new byte[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    row[i] = byte.Parse(values[i].ToString());
                }

                foreach (byte b in row) bytes.Add(b);
            }

            return bytes.ToArray();
        }

        private static void WritePackedFile(byte[] data, string nameWithoutExtension)
        {
            using BinaryWriter writer = new(File.Open($"{nameWithoutExtension}.map", FileMode.Create));

            foreach (byte b in data) writer.Write(b);
            writer.Close();
        }

        private static void Main(string[] args)
        {
            if (args.Length == 0) return;

            //Load each CSV file from disk, and then convert it to a binary file
            foreach (string csvFile in args)
            {
                if (!File.Exists(csvFile)) continue;

                using StreamReader reader = new(File.OpenRead(csvFile));
                byte[] bytes = ReadCSV(reader);
                reader.Close();
                WritePackedFile(bytes, Path.GetFileNameWithoutExtension(csvFile));
            }
        }
    }
}