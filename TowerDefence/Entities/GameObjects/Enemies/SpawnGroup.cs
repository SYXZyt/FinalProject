namespace TowerDefence.Entities.GameObjects.Enemies
{
    internal class SpawnGroup
    {
        private List<Enemy> group;

        public bool IsEmpty => group.Any();

        public Enemy ReadFromGroup()
        {
            if (IsEmpty) return null;

            Enemy e = group[0];
            group.RemoveAt(0);
            return e;
        }

        public void AddToSpawnGroup(Enemy enemy)
        {
            group.Insert(0, enemy);
        }

        public SpawnGroup(List<Enemy> group)
        {
            this.group = group;
        }
    }
}