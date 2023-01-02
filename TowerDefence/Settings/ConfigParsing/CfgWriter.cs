namespace TowerDefence.Settings.ConfigParsing
{
    internal sealed class CfgWriter : IDisposable
    {
        private StreamWriter writer;
        private List<string> writtenKeys;

        public void Write(string key, string value)
        {
            if (writtenKeys.Contains(key)) throw new Exception($"Key '{key}' has already been added");
            writtenKeys.Add(key);

            writer.WriteLine($"s:{key}:\"{value}\"");
        }

        public void Write(string key, float value)
        {
            if (writtenKeys.Contains(key)) throw new Exception($"Key '{key}' has already been added");
            writtenKeys.Add(key);

            writer.WriteLine($"f:{key}:{value}");
        }

        public void Write(string key, int value)
        {
            if (writtenKeys.Contains(key)) throw new Exception($"Key '{key}' has already been added");
            writtenKeys.Add(key);

            writer.WriteLine($"i:{key}:{value}");
        }

        public void Dispose()
        {
            writer?.Dispose();
        }

        public void Close()
        {
            writer?.Close();
            writer = null;
            writtenKeys = null;
        }

        public void Open(string file)
        {
            if (writer is not null) throw new Exception("File was not closed when trying to open");

            Directory.CreateDirectory(Path.GetDirectoryName(file));

            writer = new StreamWriter(File.Open(file, FileMode.OpenOrCreate));
            writtenKeys = new();
        }

        public CfgWriter(string file)
        {
            Open(file);
        }
    }
}