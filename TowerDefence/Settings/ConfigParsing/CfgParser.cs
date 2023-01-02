namespace TowerDefence.Settings.ConfigParsing
{
    internal sealed class CfgParser : IDisposable
    {
        private StreamReader reader;
        private string[] lines;
        private string filename;

        public void Dispose()
        {
            Close();
            reader?.Dispose();
        }

        public void Close()
        {
            reader?.Close();
            reader = null;
            lines = null;
            filename = null;
        }

        public CfgResult Read(string key)
        {
            if (reader is null) throw new Exception("File is closed when trying to read");

            for (int i = 0; i < lines.Length; i++)
            {
                string malformedError = $"Malformed line {i} in {filename}";

                string line = lines[i];

                //Check if this line is a comment
                if (line.StartsWith(';')) continue;

                //Now we have a valid line, we need to remove any whitespace before or after the data
                line = line.Trim();

                string[] split = line.Split(':');

                //Make sure that all data was provided
                if (split.Length != 3) throw new Exception(malformedError);

                //Index 1 will store the key, so we need to check that
                if (key != split[1]) continue;

                //Now we know we have the line wanted, we can parse the data
                CfgResult cfgResult = new();

                //Check that the type is valid
                switch (split[0])
                {
                    case "i":
                        {
                            cfgResult.type = CfgType.I16;
                            if (!int.TryParse(split[2], out int j)) throw new Exception(malformedError);
                            cfgResult.result = j;
                        }
                        break;
                    case "f":
                        {
                            cfgResult.type = CfgType.F32;
                            if (!float.TryParse(split[2], out float f)) throw new Exception(malformedError);
                            cfgResult.result = f;
                        }
                        break;
                    case "s":
                        {
                            //Make sure that the string is surrounded by quotes
                            string str = split[2];

                            if (str[0] != '"' && str[^1] != '"') throw new Exception("Malformed string");

                            //Now we know that the string is valid, we can remove the first and last character
                            if (str.Length == 2) str = ""; //If the read string is empty, we can skip the trim
                            else str = str[1..^1];
                            cfgResult.result = str;
                        }
                        break;
                    default: throw new Exception(malformedError);
                }

                return cfgResult;
            }

            throw new Exception($"No key found with name '{key}'");
        }

        public void Open(string file)
        {
            filename = file;

            if (reader is not null) throw new Exception("File was not closed when trying to reopen");
            if (!File.Exists(file)) throw new FileNotFoundException(file);

            reader = new(File.OpenRead(file));
            List<string> lines = new();

            while (!reader.EndOfStream)
            {
                lines.Add(reader.ReadLine());
            }

            this.lines = lines.ToArray();
        }

        public CfgParser(string file)
        {
            Open(file);
        }
    }
}