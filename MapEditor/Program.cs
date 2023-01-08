namespace MapEditor
{
    internal static class Program
    {
        private static int menuOption = 0;

        private delegate void MenuOption();
        private static MenuOption[] menuOptions = new MenuOption[3] { New, Load, Exit };

        private static void New()
        {
            Console.CursorVisible = true;
            Console.Clear();
            Console.WriteLine("Enter map name");
            string map = Console.ReadLine() + ".map";

            Editor editor = new(map, true);
            Console.WriteLine("Loading!");
            editor.Run();

            Environment.Exit(0);
        }

        private static void Load()
        {
            Console.CursorVisible = true;
            Console.Clear();
            Console.WriteLine("Enter map to load");
            string map = Console.ReadLine() + ".map";

            bool isNew = !File.Exists(map);
            Editor editor = new(map, isNew);
            Console.WriteLine("Loading!");
            editor.Run();

            Environment.Exit(0);
        }

        private static void Exit()
        {
            Environment.Exit(0);
        }

        private static void UpdateMenu()
        {
            ConsoleKey key = Console.ReadKey().Key;

            if (key == ConsoleKey.UpArrow)
            {
                menuOption--;

                if (menuOption == -1) menuOption = 2;
            }
            else if (key == ConsoleKey.DownArrow)
            {
                menuOption++;

                if (menuOption > 2) menuOption = 0;
            }
            else if (key == ConsoleKey.Enter)
            {
                Console.ResetColor();
                menuOptions[menuOption].Invoke();
            }
        }

        private static void DrawMenu()
        {
            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.White;

            string[] names = new string[3] { "NEW", "LOAD", "EXIT" };

            for (int i = 0; i < 3; i++)
            {
                if (menuOption == i)
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.White;
                }

                Console.WriteLine(names[i]);
                Console.ResetColor();
            }

            Console.ForegroundColor = ConsoleColor.Black;
        }

        private static void Main()
        {
            AppDomain domain = AppDomain.CurrentDomain;
            domain.ProcessExit += Domain_ProcessExit;

            Console.Clear();

            /* Menu Options are
             * NEW
             * LOAD
             * EXIT
             */

            while (true)
            {
                Console.CursorVisible = false;
                DrawMenu();
                UpdateMenu();
            }
        }

        private static void Domain_ProcessExit(object sender, EventArgs e)
        {
            Console.ResetColor();
        }
    }
}