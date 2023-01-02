namespace TowerDefence.CheatEngine
{
    internal sealed class Token
    {
        private readonly TokenType type;
        private readonly string lexeme;

        public TokenType Type => type;
        public string Lexeme => lexeme;

        public Token(TokenType type, string lexeme)
        {
            this.type = type;
            this.lexeme = lexeme;
        }
    }
}