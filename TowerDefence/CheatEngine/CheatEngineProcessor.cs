namespace TowerDefence.CheatEngine
{
    internal enum CheatCommand
    {
        INVALID,
        SET_MONEY,
        SET_HEALTH,
        DISPOSE,
        EXIT,
        FORCE_WIN,
        FORCE_LOSE,
    }

    internal struct Param
    {
        public enum Type
        {
            INT,
            STR,
            NUL,
        }

        public Type type;
        public object value;

        public Param()
        {
            type = Type.NUL;
            value = null;
        }
    }

    internal struct Cheat
    {
        public CheatCommand cmd;
        public Param[] @params;

        public Cheat()
        {
            cmd = CheatCommand.INVALID;
            @params = Array.Empty<Param>();
        }
    }

    internal static class CheatEngineProcessor
    {
        public static Cheat ParseCheat(string input)
        {
            Lexer lexer = new(input);
            Token[] tokens = lexer.Tokenise();

            //A new blank cheat is has its command set to do nothing so we can return with no worries about errors
            if (tokens is null || tokens.Length == 0) return new Cheat();

            Parser parser = new(tokens);
            return parser.ParseCheat();
        }
    }
}