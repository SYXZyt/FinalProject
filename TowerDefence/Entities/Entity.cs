using Microsoft.Xna.Framework;

namespace TowerDefence.Entities
{
    internal abstract class Entity
    {
        private Vector2 position;

        public Vector2 GetPosition() => position;

        public abstract string Serialise();

        public Entity(Vector2 position)
        {
            this.position = position;
        }
    }
}