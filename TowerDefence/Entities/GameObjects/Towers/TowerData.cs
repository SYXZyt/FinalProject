namespace TowerDefence.Entities.GameObjects.Towers
{
    internal struct TowerData
    {
        public byte id;
        public string name;
        public ushort range;
        public ushort cost;
        public string texIdle;
        public string texButton;
        public float rate;
        public string projectile;
        public bool rotate;
    }
}