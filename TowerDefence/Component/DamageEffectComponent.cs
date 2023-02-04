using Microsoft.Xna.Framework;
using TowerDefence.Entities.GameObjects.Enemies;

namespace TowerDefence.Component
{
    internal class DamageEffectComponent : BaseComponent
    {
        public Enemy Enemy { get; set; }

        public static Dictionary<string, DamageEffectStats> damageEffectStats = new();

        protected readonly float damageTime;
        protected float elapsedTime;
        protected readonly int damage;

        public override void Update(GameTime gameTime)
        {
            elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (elapsedTime >= damageTime)
            {
                Enemy.Damage(damage);
                MarkForRemoval = true;
            }
        }

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