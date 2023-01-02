namespace TowerDefence.Settings.ConfigParsing
{
    internal struct CfgResult
    {
        public object result;
        public CfgType type;

        public CfgResult()
        {
            result = "null";
            type = CfgType.STR;
        }

        public CfgResult(object result, CfgType type)
        {
            this.result = result;
            this.type = type;
        }
    }

    internal class CfgIncorrectType : Exception { public CfgIncorrectType(string key) : base($"Invalid type in key '{key}'") { } }
}