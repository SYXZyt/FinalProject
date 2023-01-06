namespace TowerDefence.CheatEngine
{
    internal sealed class Parser
    {
        private Token CurrentToken => tPointer < tokens.Length ? tokens[tPointer] : null;

        private readonly Token[] tokens;
        private int tPointer;

        private void Advance() => tPointer++;

        public Cheat ParseCheat()
        {
            if (CurrentToken.Type != TokenType.IDEN) return new();

            Cheat cheat;
            List<Param> cheatParams = new();

            switch (CurrentToken.Lexeme)
            {
                case "health":
                    cheat.cmd = CheatCommand.SET_HEALTH;
                    break;
                case "money":
                    cheat.cmd = CheatCommand.SET_MONEY;
                    break;
                case "exit":
                    cheat.cmd = CheatCommand.EXIT;
                    break;
                case "dispose":
                    cheat.cmd = CheatCommand.DISPOSE;
                    break;
                case "force_win":
                    cheat.cmd = CheatCommand.FORCE_WIN;
                    break;
                case "force_lose":
                    cheat.cmd = CheatCommand.FORCE_LOSE;
                    break;
                default:
                    return new();
            }

            Advance();
            while (CurrentToken is not null)
            {
                Param p;

                switch (CurrentToken.Type)
                {
                    case TokenType.STRING:
                        p.type = Param.Type.STR;
                        p.value = CurrentToken.Lexeme;
                        break;
                    case TokenType.INT:
                        p.type = Param.Type.INT;
                        p.value = int.Parse(CurrentToken.Lexeme);
                        break;
                    case TokenType.IDEN:
                        {
                            if (CurrentToken.Lexeme == "max")
                            {
                                tokens[tPointer] = new(TokenType.INT, int.MaxValue.ToString());
                            }
                            else return new();
                        }
                        continue;
                    default: return new();
                }

                Advance();
                cheatParams.Add(p);
            }

            cheat.@params = cheatParams.ToArray();
            return cheat;
        }

        public Parser(Token[] tokens)
        {
            this.tokens = tokens;
            tPointer = 0;
        }
    }
}