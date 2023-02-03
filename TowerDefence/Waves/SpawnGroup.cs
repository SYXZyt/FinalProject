namespace TowerDefence.Waves
{
    internal struct SpawnGroup
    {
        public string id;
        public int count;
        public float delay;
        public float cooldown;

        public SpawnGroup(SpawnGroup spawnGroup)
        {
            this.id = spawnGroup.id;
            this.count = spawnGroup.count;
            this.delay = spawnGroup.delay;
            this.cooldown = spawnGroup.cooldown;
        }
    }
}