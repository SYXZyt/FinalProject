using System.Text;

namespace TowerDefencePackets
{
    public class Snapshot
    {
        public long ID { get; set; }
        public byte Health { get; set; }
        public ushort Money { get; set; }

        public string Serialize()
        {
            StringBuilder sb = new();
            sb.Append($"{ID:D19}{Health:D3}{Money:D5}");
            return sb.ToString();
        }

        public void Deserialize(string data)
        {
            const int ID_START = 0;
            const int ID_LENGTH = 19;

            const int HEALTH_START = ID_START + ID_LENGTH;
            const int HEALTH_LENGTH = 3;

            const int MONEY_START = HEALTH_START + HEALTH_LENGTH;
            const int MONEY_LENGTH = 5;

            ID = long.Parse(data[..ID_LENGTH]);
            Health = byte.Parse(data.Substring(HEALTH_START, HEALTH_LENGTH));
            Money = ushort.Parse(data.Substring(MONEY_START, MONEY_LENGTH));
        }
    }
}