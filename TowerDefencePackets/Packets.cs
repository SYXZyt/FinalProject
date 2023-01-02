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
        public int Direction;
    }
}