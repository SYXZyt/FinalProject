using UILibrary;
using TowerDefence.Visuals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Game = TowerDefence.Scenes.Game;

namespace TowerDefence.Entities.GameObjects.Enemies
{
    internal sealed class Enemy : Entity
    {
        public static List<Vector2> HQLocations = new();

        public static Dictionary<string, EnemyData> enemyDatas = new();
        public static byte[,] mapData;

        private readonly Animation frames;
        private readonly EnemyData data;
        private Vector2 offset;
        private readonly Vector2 drawOffset;
        private int dir; //0 - up, 1 - right, 2 - down, 3 - left,

        public override Entity Deserialise(string serialised)
        {
            throw new NotImplementedException();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Texture2D frame = frames.GetFrame(dir);

            frame.Draw(position + drawOffset + offset, spriteBatch, Color.White);
        }

        public override byte GetID() => data.id;

        public override string Serialise()
        {
            throw new NotImplementedException();
        }

        private void Move()
        {
            Vector2 vec = dir switch
            {
                0 => new(0, -1),
                1 => new(1, 0),
                2 => new(0, 1),
                3 => new(-1, 0),
                _ => Vector2.Zero,
            };

            offset += vec;
        }

        public override void Update(GameTime gameTime)
        {
            //Check whether to move the object
            Vector2 actualLoc = position + offset;
            if (actualLoc.X % Game.TileSize == 0 && actualLoc.Y % Game.TileSize == 0)
            {
                position = actualLoc;

                //Check the next direction
                switch (dir)
                {
                    case 0:
                        {
                            //If we are moving up, we need to check if there is a 1 tile above
                            //If we are at y 0 already, then we will delete this entity
                            if (position.Y == 0)
                            {
                                markForDeletion = true;
                                break;
                            }

                            //Check for blockage
                            if (mapData[(int)(position.Y - 1), (int)position.X] != 1)
                            {
                                //Check the left
                                if (position.X >= 1 && mapData[(int)position.Y, (int)(position.X - 1)] == 1)
                                {
                                    dir = 3;
                                }
                                else if (position.X <= 47 && mapData[(int)position.Y, (int)(position.X + 1)] == 1)
                                {
                                    dir = 1;
                                }
                                else
                                {
                                    throw new Exception("entity got stuck");
                                }
                            }
                        }
                        break;
                }
            }
            else Move();

            textures.Update(gameTime);
        }

        public Enemy(string name, Vector2 position, Vector2 drawOffset, Animation textures)
        {
            this.position = position;
            frames = textures;
            this.drawOffset = drawOffset;
            if (!enemyDatas.ContainsKey(name)) throw new($"No unit called '{name}' found");
            data = enemyDatas[name];
            frames.SetFreeze(true);
        }
    }
}