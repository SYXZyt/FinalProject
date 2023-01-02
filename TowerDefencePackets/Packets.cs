using LiteNetLib.Utils;

namespace TowerDefencePackets
{
    public sealed class ServerSnapshotPacket
    {
        public short Players;
        public ushort[] Money;
        public byte[] Health;
    }

    public sealed class UpdatePacket
    {
        public int PlayerAction;
    }

    public sealed class WelcomePacket
    {
        public string PlayerID;
        public int PlayerNumber;
        public ushort Money;
        public byte Health;
    }

    public sealed class EchoPacket
    {
        public int Direction { get; set; }

        /*public void Serialize(NetDataWriter writer) => writer.Put(Direction);
        public void Deserialize(NetDataReader reader) => Direction = reader.GetInt();*/
    }

    public sealed class RequestName
    {
        public string Name { get; set; }

        /*public void Serialize(NetDataWriter writer) => writer.Put(Name);
        public void Deserialize(NetDataReader reader) => Name = reader.GetString();*/
    }
}