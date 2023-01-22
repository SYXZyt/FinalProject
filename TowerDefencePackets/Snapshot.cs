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
            sb.Append($"{ID},{Health},{Money}");
            return sb.ToString();
        }

        public void Deserialize(string data)
        {
            string[] allData = data.Split("|");
            string[] csv = allData[0].Split(",");
            ID = long.Parse(csv[0]);
            Health = byte.Parse(csv[1]);
            Money = ushort.Parse(csv[2]);
        }
    }
}