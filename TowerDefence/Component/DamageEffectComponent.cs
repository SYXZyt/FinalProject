using Microsoft.Xna.Framework;
using TowerDefence.Entities.GameObjects.Enemies;

namespace TowerDefence.Component
{
    internal class DamageEffectComponent : BaseComponent
    {
        public Enemy Enemy { get; set; }

        public static Dictionary<string, DamageEffectStats> damageEffectStats = new();

        protected float damageTime;
        protected float elapsedTime;
        protected int damage;

        //damageTime=elapsedTime
        public string Serialise() => $"{damageTime}:{elapsedTime}:{damage}";

        public static DamageEffectComponent Deserialise(string serialised)
        {
            string[] parts = serialised.Split(":");

            DamageEffectComponent comp = new()
            {
                damageTime = float.Parse(parts[0]),
                elapsedTime = float.Parse(parts[1]),
                damage = int.Parse(parts[2]),
            };

            return comp;
        }

        public override void Update(GameTime gameTime)
        {
            elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (elapsedTime >= damageTime)
            {
                Enemy.Damage(damage);
                MarkForRemoval = true;
            }
        }

        private DamageEffectComponent() { }

        public DamageEffectComponent(float damageTime, int damage)
        {
            this.damageTime = damageTime;
            this.damage = damage;
        }

        public DamageEffectComponent(string effectName)
        {
            DamageEffectStats stats = damageEffectStats[effectName];
            damageTime = stats.time;
            damage = stats.damage;
        }
    }
}