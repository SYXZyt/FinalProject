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

        private bool checkForPosMovement = false;
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
            //Texture2D demo = AssetStreamer.AssetContainer.ReadTexture("");

            //demo.Draw(position * Game.TileSize + drawOffset, spriteBatch, Color.White);
            frame.Draw(absolutePosition, spriteBatch, Color.White);
        }

        public override byte GetID() => data.id;

        public override string Serialise() => $"|1,{absolutePosition.X - drawOffset.X},{absolutePosition.Y - drawOffset.Y},{dir},{data.id}";

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
            if (absolutePosition.X % Game.TileSize == 0 && absolutePosition.Y % Game.TileSize == 0 && checkForPosMovement)
            {
                checkForPosMovement = false;

                MovePosition();

                List<int> moves = new();
                bool dontMove = false;

                //Check the next direction
                //Most of this code is fairly repetitive. If you understand one case, you will understand the rest
                switch (dir)
                {
                    case 0:
                        {
                            //If we are moving up, we need to check if there is a 1 tile above
                            //If we are at y 0 already, then we will delete this entity
                            if (position.Y < 1)
                            {
                                markForDeletion = true;
                                dontMove = true;
                                break;
                            }

                            //Check for HQ
                            if (mapData[(int)(position.Y - 1), (int)position.X] == 15)
                            {
                                markForDeletion = true;
                                Game.Instance.DamagePlayer(data.damage);
                                dontMove = true;
                                break;
                            }

                            //Check the up
                            if (mapData[(int)(position.Y - 1), (int)position.X] == 1)
                            {
                                moves.Add(0);
                            }
                            //Check the left
                            if (position.X > 1 && mapData[(int)position.Y, (int)(position.X - 1)] == 1)
                            {
                                moves.Add(3);
                            }
                            //Check the right
                            if (position.X < 47 && mapData[(int)position.Y, (int)(position.X + 1)] == 1)
                            {
                                moves.Add(1);
                            }
                        }
                        break;
                    case 1:
                        {
                            if (position.X > 47)
                            {
                                markForDeletion = true;
                                dontMove = true;
                                break;
                            }

                            //Check for HQ
                            if (mapData[(int)position.Y, (int)(position.X + 1)] == 15)
                            {
                                markForDeletion = true;
                                Game.Instance.DamagePlayer(data.damage);
                                dontMove = true;
                                break;
                            }

                            //Check right
                            if (mapData[(int)position.Y, (int)(position.X + 1)] == 1)
                            {
                                moves.Add(1);
                            }
                            //Check up
                            if (position.Y > 1 && mapData[(int)(position.Y - 1), (int)position.X] == 1)
                            {
                                moves.Add(0);
                            }
                            //Check down
                            if (position.Y < 41 && mapData[(int)(position.Y + 1), (int)position.X] == 1)
                            {
                                moves.Add(2);
                            }
                        }
                        break;
                    case 2:
                        {
                            if (position.Y > 41)
                            {
                                markForDeletion = true;
                                dontMove = true;
                                break;
                            }

                            //Check for HQ
                            if (mapData[(int)(position.Y + 1), (int)position.X] == 15)
                            {
                                markForDeletion = true;
                                Game.Instance.DamagePlayer(data.damage);
                                dontMove = true;
                                break;
                            }

                            //Check down
                            if (mapData[(int)(position.Y + 1), (int)position.X] == 1)
                            {
                                moves.Add(2);
                            }
                            //Check left
                            if (position.X > 1 && mapData[(int)position.Y, (int)(position.X - 1)] == 1)
                            {
                                moves.Add(3);
                            }
                            //Check right
                            if (position.X < 47 && mapData[(int)position.Y, (int)(position.X + 1)] == 1)
                            {
                                moves.Add(1);
                            }
                        }
                        break;
                    case 3:
                        {
                            if (position.X < 1)
                            {
                                markForDeletion = true;
                                dontMove = true;
                                break;
                            }

                            //Check for HQ
                            if (mapData[(int)position.Y, (int)(position.X - 1)] == 15)
                            {
                                markForDeletion = true;
                                Game.Instance.DamagePlayer(data.damage);
                                dontMove = true;
                                break;
                            }

                            //Check right
                            if (mapData[(int)position.Y, (int)(position.X - 1)] == 1)
                            {
                                moves.Add(3);
                            }
                            //Check up
                            if (position.Y > 1 && mapData[(int)(position.Y - 1), (int)position.X] == 1)
                            {
                                moves.Add(0);
                            }
                            //Check down
                            if (position.Y < 41 && mapData[(int)(position.Y + 1), (int)position.X] == 1)
                            {
                                moves.Add(2);
                            }
                        }
                        break;
                }

                if (!dontMove)
                {
                    if (moves.Count == 0) throw new("Entity stuck");
                    dir = moves[Game.Instance.RNG.Next(0, moves.Count)];
                }
            }

            elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (elapsedTime * data.speed >= 1)
            {
                Move();
                elapsedTime = 0;
                checkForPosMovement = true;
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
            checkForPosMovement = false;
        }
    }
}