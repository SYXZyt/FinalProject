namespace TowerDefence.Component
{
    internal abstract class BaseComponent
    {
        public bool MarkForRemoval { get; set; } = false;

        public abstract void Update(Microsoft.Xna.Framework.GameTime gameTime);
    }
}