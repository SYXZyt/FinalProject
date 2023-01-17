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

        public Vector2 GetScreenPosition() => absolutePosition;

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

                List<int> moves = CheckNextDirection();
                if (moves.Count > 0)
                {
                    dir = moves[Game.Instance.RNG.Next(moves.Count)];
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

        private List<int> CheckNextDirection()
        {
            List<int> moves = new List<int>();
            //Check for out of bounds
            if (IsOutOfBounds())
            {
                markForDeletion = true;
                return moves;
            }

            //Check for HQ
            if (IsHeadquarters())
            {
                markForDeletion = true;
                Game.Instance.DamagePlayer(data.damage);
                return moves;
            }

            //Check the up
            if (dir != 2 && IsNotBlocked(0))
            {
                moves.Add(0);
            }
            //Check the right
            if (dir != 3 && IsNotBlocked(1))
            {
                moves.Add(1);
            }
            //Check the down
            if (dir != 0 && IsNotBlocked(2))
            {
                moves.Add(2);
            }
            //Check the left
            if (dir != 1 && IsNotBlocked(3))
            {
                moves.Add(3);
            }
            return moves;
        }

        private bool IsOutOfBounds() =>
            dir switch
            {
                0 => position.Y < 1,
                1 => position.X > 47,
                2 => position.Y > 41,
                3 => position.X < 1,
                _ => false,
            };
        private bool IsHeadquarters() =>
            dir switch
            {
                0 => mapData[(int)(position.Y - 1), (int)position.X] == 15,
                1 => mapData[(int)position.Y, (int)(position.X + 1)] == 15,
                2 => mapData[(int)(position.Y + 1), (int)position.X] == 15,
                3 => mapData[(int)position.Y, (int)(position.X - 1)] == 15,
                _ => false,
            };

        private bool IsNotBlocked(int direction) =>
            direction switch
            {
                0 => position.Y > 1 && mapData[(int)(position.Y - 1), (int)position.X] == 1,
                1 => position.X < 47 && mapData[(int)position.Y, (int)(position.X + 1)] == 1,
                2 => position.Y < 41 && mapData[(int)(position.Y + 1), (int)position.X] == 1,
                3 => position.X > 1 && mapData[(int)position.Y, (int)(position.X - 1)] == 1,
                _ => false,
            };

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