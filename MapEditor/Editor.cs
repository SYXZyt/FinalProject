namespace MapEditor
{
    internal struct Vector2
    {
        public int x;
        public int y;

        public Vector2() => x = y = 0;
        public Vector2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    internal sealed class Editor
    {
        private readonly string fname;
        private int tileIndex;
        private readonly int[,] map;

        private Vector2 cursorLast;
        private Vector2 cursor;

        private readonly char[] tiles = new char[] { '▓', '▒', '╠', '╔', '╦', '╗', '╣', '╝', '╩', '╚', '└', '┘', '┌', '┐' };

        private void InitDraw()
        {
            Console.ResetColor();
            Console.SetCursorPosition(0, 0);

            char[] buffer = new char[(42 * 48) + 42 /* <-- remember to allocate space for the \n*/];
            int index = 0;
            for (int y = 0; y < 42; y++)
            {
                for (int x = 0; x < 48; x++)
                {
                    buffer[index] = tiles[map[x, y]];
                    index++;
                }
                buffer[index] = '\n';
                index++;
            }
            Console.Write(buffer);

            Console.SetCursorPosition(cursor.x, cursor.y);
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;
            Console.Write(tiles[map[cursor.x, cursor.y]]);

            DrawTiles();
        }

        private void CleanCursor()
        {
            Console.ResetColor();
            Console.SetCursorPosition(cursorLast.x, cursorLast.y);
            Console.Write(tiles[map[cursorLast.x, cursorLast.y]]);

            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;
            Console.SetCursorPosition(cursor.x, cursor.y);
            Console.Write(tiles[map[cursor.x, cursor.y]]);
        }

        private void DrawTiles()
        {
            Console.SetCursorPosition(0, 44);
            Console.ResetColor();

            foreach (char c in tiles)
            {
                if (c == tiles[tileIndex])
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.White;
                }

                Console.Write(c);
                Console.ResetColor();
                Console.Write(' ');
            }
        }

        private void UpdateInput()
        {
            Console.SetCursorPosition(0, 45);
            Console.ForegroundColor = Console.BackgroundColor = ConsoleColor.Black;

            Console.CursorVisible = false;
            ConsoleKey key = Console.ReadKey().Key;

            switch (key)
            {
                case ConsoleKey.LeftArrow:
                    if (cursor.x == 0) break;
                    cursorLast = cursor;
                    cursor.x--;
                    CleanCursor();
                    break;
                case ConsoleKey.RightArrow:
                    if (cursor.x == 47) break;
                    cursorLast = cursor;
                    cursor.x++;
                    CleanCursor();
                    break;
                case ConsoleKey.UpArrow:
                    if (cursor.y == 0) break;
                    cursorLast = cursor;
                    cursor.y--;
                    CleanCursor();
                    break;
                case ConsoleKey.DownArrow:
                    if (cursor.y == 41) break;
                    cursorLast = cursor;
                    cursor.y++;
                    CleanCursor();
                    break;
                case ConsoleKey.Enter:
                    map[cursor.x, cursor.y] = tileIndex;
                    CleanCursor();
                    break;
                case ConsoleKey.OemPlus:
                    if (tileIndex < tiles.Length - 1) tileIndex++;
                    DrawTiles();
                    break;
                case ConsoleKey.OemMinus:
                    if (tileIndex > 0) tileIndex--;
                    DrawTiles();
                    break;
                case ConsoleKey.S:
                    Save();
                    break;
                case ConsoleKey.Escape:
                    Environment.Exit(0);
                    break;
                default:
                    break;
            }
        }

        private void Save()
        {
            using StreamWriter writer = new(fname);
            for (int y = 0; y < 42; y++)
            {
                for (int x = 0; x < 48; x++)
                {
                    writer.Write($"{map[x, y]},");
                } writer.Write("\r\n");
            }

            writer.Close();
        }

        public void Run()
        {
            InitDraw();

            while (true)
            {
                UpdateInput();
            }
        }

        public Editor(string fname, bool isNewFile)
        {
            this.fname = fname;
            cursor = new();

            Console.BufferWidth = Console.WindowWidth;
            Console.BufferHeight = Console.WindowHeight;

            if (isNewFile)
            {
                map = new int[48, 42];
            }
            else
            {
                using StreamReader reader = new(fname);
                map = new int[48, 42];

                for (int i = 0; i < 42; i++)
                {
                    string line = reader.ReadLine();
                    string[] cs = line.Split(",");
                    for (int j = 0; j < 48; j++) map[j, i] = int.Parse(cs[j].ToString());
                }

                reader.Close();
            }
        }
    }
}