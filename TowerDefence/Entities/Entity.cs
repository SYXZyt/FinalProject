using UILibrary;
using TowerDefence.Visuals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefence.Entities
{
    /// <summary>
    /// Represent a base entity object
    /// </summary>
    internal abstract class Entity
    {
        protected AABB aabb;
        protected Vector2 position;
        protected AnimationCollection textures;
        protected bool markForDeletion;

        public abstract byte GetID();

        public AnimationCollection Textures { get => textures; protected set => textures = value; }
        public bool MarkForDeletion { get => markForDeletion; protected set => markForDeletion = value; }
        public AABB AABB => aabb;

        /// <summary>
        /// Get the current position of the entity
        /// </summary>
        /// <returns>A vector storing the position</returns>
        public Vector2 GetPosition() => position;

        public abstract void Update();
        public abstract void Draw(SpriteBatch spriteBatch);

        /// <summary>
        /// Convert the object into a structure the server will understand
        /// </summary>
        /// <returns>A string containing the serialised data</returns>
        public abstract string Serialise();

        /// <summary>
        /// Convert from a string, into an Entity object
        /// </summary>
        /// <param name="serialised">The serialised data</param>
        /// <returns>An reference to the generated Entity</returns>
        public abstract Entity Deserialise(string serialised);

        public Entity()
        {
            position = default;
            aabb = null;
            textures = null;
            markForDeletion = false;
        }

        public Entity(Vector2 position, AABB aabb)
        {
            this.position = position;
            textures = new();
            this.aabb = aabb;
            markForDeletion = false;
        }
    }
}