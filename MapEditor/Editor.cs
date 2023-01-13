using System.Text;

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
        
        private readonly char[] tiles = new char[] { '▓', '▒', '╠', '╔', '╦', '╗', '╣', '╝', '╩', '╚', '╘', '╛', '╒', '╕', '♠', '☺', '█', '├', '┌', '┬', '┐', '┤', '┘', '┴', '└', '╓', '╖', '╙', '╜' };
        
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

            Console.ResetColor();
            StringBuilder line = new();
            for (int i = 0; i < tiles.Length * 2 + 1; i++) line.Append('─');
            Console.SetCursorPosition(0, 45);
            line[0] = '└';
            line[^1] = '┘';
            Console.Write(line.ToString());

            line[0] = '┌';
            line[^1] = '┐';
            line[1] = 'T';
            line[2] = 'i';
            line[3] = 'l';
            line[4] = 'e';
            line[5] = 's';

            Console.SetCursorPosition(0, 43);
            Console.Write(line.ToString());

            Console.SetCursorPosition(0, 44);
            Console.Write('│');

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
            Console.SetCursorPosition(1, 44);
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

            Console.SetCursorPosition(tiles.Length * 2, 44);
            Console.Write('│');

            Console.SetCursorPosition(0, 46);
            Console.Write("Enter 'S' to save. Enter 'F' to fill");
        }

        private void FloodFill(int x, int y, int targetTile, int replacementTile)
        {
            if (targetTile == replacementTile) return; //If we are trying to replace a tile with itself, don't do anything
                
            //Check we are within bounds
            if (x < 0 || x >= 48 || y < 0 || y >= 42)
            {
                return;
            }

            //Don't do anything if this is the incorrect id
            if (map[x, y] != targetTile)
            {
                return;
            }

            map[x, y] = replacementTile;

            //Draw the tile
            Console.ResetColor();
            Console.SetCursorPosition(x, y);
            Console.Write(tiles[replacementTile]);
                
            //Affect the surrounding tiles
            FloodFill(x + 1, y, targetTile, replacementTile);
            FloodFill(x - 1, y, targetTile, replacementTile);
            FloodFill(x, y + 1, targetTile, replacementTile);
            FloodFill(x, y - 1, targetTile, replacementTile);
        }

        private void UpdateInput()
        {
            Console.SetCursorPosition(0, 47);
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
                case ConsoleKey.F:
                    {
                        int targetTile = map[cursor.x, cursor.y];
                        int replacementTile = tileIndex;
                        FloodFill(cursor.x, cursor.y, targetTile, replacementTile);

                        CleanCursor();
                    }
                    break;
                default:
                    break;
            }
        }

        private void Save()
        {
            Packer.PackData(fname, map);
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

            if (isNewFile)
            {
                map = new int[48, 42];
            }
            else
            {
                map = Packer.UnpackData(fname);
            }
        }
    }
}