namespace TowerDefence.Logging
{
    internal static class Logger
    {
        private static StreamWriter writer;

        public static void CloseEvent(object sender, EventArgs e) => Close();

        public static void Close()
        {
            writer?.Close();
            writer?.Dispose();
            writer = null;
        }

        public static void Write(System.Text.StringBuilder sb) => Write(sb.ToString());
        public static void Write(string str) => writer.Write($"[{DateTime.Now:T}] - '{str}'\r\n");

        static Logger()
        {
            string fname = $"Log-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt";

            int i = 1;
            string temp = fname;
            while (File.Exists(temp)) temp = Path.GetFileNameWithoutExtension(fname) + $"_{i++}.txt";
            fname = temp;

            writer = new(fname);
        }
    }
}