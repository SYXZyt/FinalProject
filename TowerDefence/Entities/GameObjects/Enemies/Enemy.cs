﻿using UILibrary;
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
        private readonly Vector2 drawOffset;
        private Vector2 absolutePosition;
        private int dir; //0 - up, 1 - right, 2 - down, 3 - left,

        private float elapsedTime;

        public override Entity Deserialise(string serialised)
        {
            throw new NotImplementedException();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Texture2D frame = frames.GetFrame(dir);
            Texture2D demo = AssetStreamer.AssetContainer.ReadTexture("");

            demo.Draw(position * Game.TileSize + drawOffset, spriteBatch, Color.White);
            frame.Draw(absolutePosition + drawOffset, spriteBatch, Color.White);
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

            absolutePosition += vec;
        }

        private void MovePosition()
        {
            Vector2 vec = dir switch
            {
                0 => new(0, -1),
                1 => new(1, 0),
                2 => new(0, 1),
                3 => new(-1, 0),
                _ => Vector2.Zero,
            };

            position += vec;
        }

        public override void Update(GameTime gameTime)
        {
            if (absolutePosition.X % Game.TileSize == 0 && absolutePosition.Y % Game.TileSize == 0)
            {
                MovePosition();

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

            elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (elapsedTime * data.speed >= 1)
            {
                Move();
                elapsedTime = 0;
            }
        }

        public Enemy(string name, Vector2 screenPosition, Vector2 gridPosition, Vector2 drawOffset, Animation textures)
        {
            absolutePosition = screenPosition;
            position = gridPosition;
            frames = textures;
            this.drawOffset = drawOffset;
            if (!enemyDatas.ContainsKey(name)) throw new($"No unit called '{name}' found");
            data = enemyDatas[name];
            frames.SetFreeze(true);
            elapsedTime = 0;
        }
    }
}