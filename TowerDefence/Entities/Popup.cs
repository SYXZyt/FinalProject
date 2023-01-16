using UILibrary;
using TowerDefence.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Game = TowerDefence.Scenes.Game;

namespace TowerDefence.Entities
{
    internal class Popup : Entity
    {
        private const float OpacitySpeed = 8f;

        private float opacity;
        private readonly Label label;
        private readonly float lifetime;
        private float time;
        private readonly Vector2 movementVector;

        public override Entity Deserialise(string serialised)
        {
            throw new NotImplementedException();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            label.DrawWithShadow(spriteBatch);
        }

        public override byte GetID()
        {
            throw new NotImplementedException();
        }

        public override string Serialise()
        {
            throw new NotImplementedException();
        }

        public override void Update(GameTime gameTime)
        {
            //Destroy if invisible
            if (opacity <= 0f)
            {
                markForDeletion = true;
                return;
            }

            //Check to begin fading
            if (time >= lifetime)
            {
                opacity = Math.Max(opacity - (float)(OpacitySpeed * gameTime.ElapsedGameTime.TotalSeconds), 0f);
                label.SetOpacity(opacity);
            }

            //Move via the vector
            Vector2 moveVec = new((float)(movementVector.X * gameTime.ElapsedGameTime.TotalSeconds), (float)(movementVector.Y * gameTime.ElapsedGameTime.TotalSeconds));
            position += moveVec;

            //Update program life
            time += (float)gameTime.ElapsedGameTime.TotalSeconds;

            label.MoveLabel(position);
        }

        public Popup(Vector2 position, string message, float size, Color textColour, SpriteFont font, float lifetime, Vector2 movementVector)
        {
            Vector2 rng = new(Game.Instance.RNG.Next(-8, 9), Game.Instance.RNG.Next(-4, 5));
            this.position = position + rng;

            this.movementVector = movementVector;
            this.lifetime = lifetime;
            opacity = 1;
            label = new(message, size, position, textColour, font, Origin.MIDDLE_CENTRE, 0f);
        }
    }
}