namespace TowerDefence.CheatEngine
{
    internal sealed class Lexer
    {
        private char CurrentChar => pos < sstream.Length ? sstream[pos] : '\0';

        private int pos;
        private readonly string sstream;

        private void Advance() => pos++;

        public Token[] Tokenise()
        {
            List<Token> tokens = new();

            while (CurrentChar != '\0')
            {
                if (CurrentChar is ' ' or '\t' or '\n')
                {
                    Advance();
                    continue;
                }

                if (char.IsNumber(CurrentChar))
                {
                    string lexeme = "";

                    while (char.IsNumber(CurrentChar))
                    {
                        lexeme += CurrentChar;
                        Advance();
                    }

                    tokens.Add(new(TokenType.INT, lexeme));
                }
                else if (char.IsLetterOrDigit(CurrentChar))
                {
                    string lexeme = "";

                    while (char.IsLetterOrDigit(CurrentChar))
                    {
                        lexeme += CurrentChar;
                        Advance();
                    }

                    tokens.Add(new(TokenType.IDEN, lexeme.ToLower()));
                }
                else if (CurrentChar == '"')
                {
                    string value = "";
                    Advance();

                    while (CurrentChar != '"')
                    {
                        value += CurrentChar;
                        Advance();

                        if (CurrentChar == '\0') return null;
                    }
                    Advance();

                    tokens.Add(new(TokenType.STRING, value));
                }
                else return null;
            }

            return tokens.ToArray();
        }

        public Lexer(string sstream)
        {
            this.sstream = sstream;
            pos = 0;
        }
    }
}