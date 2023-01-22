using UILibrary;
using System.Text;
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

        private Queue<int> directionChanges;

        private ulong distanceTravelled;
        private int health;
        private bool checkForPosMovement = false;
        private readonly Animation frames;
        private readonly EnemyData data;
        private readonly Vector2 drawOffset;
        private Vector2 absolutePosition;
        private int dir; //0 - up, 1 - right, 2 - down, 3 - left,
        private bool damagedThisFrame = false;

        private float elapsedTime;

        public ulong TotalDistance => distanceTravelled;

        public override Entity Deserialise(string serialised)
        {
            throw new NotImplementedException();
        }

        public void Damage()
        {
            if (!damagedThisFrame)
            {
                health--;
                damagedThisFrame = true;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Texture2D frame = frames.GetFrame(dir);
            frame.Draw(absolutePosition, spriteBatch, Color.White);
        }

        public Vector2 GetScreenPosition() => absolutePosition;

        public override byte GetID() => data.id;

        //|1,SX,SY,X,Y,DIR,DIST,HEALTH,ELAPSED,DIRECTIONCHANGES (CSV using ;)
        public override string Serialise()
        {
            StringBuilder sb = new();

            sb.Append($"|1,{absolutePosition.X - drawOffset.X},{absolutePosition.Y - drawOffset.Y},{position.X},{position.Y},{dir},{distanceTravelled},{health},{elapsedTime},");

            StringBuilder directionChanges = new();
            List<int> directions = this.directionChanges.ToList();
            for (int i = 0; i < directions.Count; i++)
            {
                directionChanges.Append(directions[i]);
                if (i < directions.Count - 1) directionChanges.Append(';');
            } sb.Append(directionChanges);

            return sb.ToString();
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
            Vector2 hitBoxPos = (Vector2)aabb;

            distanceTravelled++;
            aabb.Move(hitBoxPos + vec);
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
            if (health <= 0)
            {
                Game.Instance.AddMoneyThisFrame(1);
                markForDeletion = true;
            }

            if (absolutePosition.X % Game.TileSize == 0 && absolutePosition.Y % Game.TileSize == 0 && checkForPosMovement)
            {
                checkForPosMovement = false;

                MovePosition();

                if (directionChanges.Count > 0)
                    dir = directionChanges.Dequeue();

                if (IsHeadquarters(position, dir))
                {
                    markForDeletion = true;
                    Game.Instance.DamagePlayer(data.damage, ownership);
                }
            }

            elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (elapsedTime * data.speed >= 1)
            {
                Move();
                elapsedTime = 0;
                checkForPosMovement = true;
            }

            damagedThisFrame = false;
        }

        private static List<int> CheckNextDirection(Vector2 position, int dir)
        {
            List<int> moves = new();

            //Check the up
            if (dir != 2 && IsNotBlocked(position, 0))
            {
                moves.Add(0);
            }
            //Check the right
            if (dir != 3 && IsNotBlocked(position, 1))
            {
                moves.Add(1);
            }
            //Check the down
            if (dir != 0 && IsNotBlocked(position, 2))
            {
                moves.Add(2);
            }
            //Check the left
            if (dir != 1 && IsNotBlocked(position, 3))
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

        private static bool IsHeadquarters(Vector2 position, int direction) =>
            direction switch
            {
                0 => mapData[(int)(position.Y - 1), (int)position.X] == 15,
                1 => mapData[(int)position.Y, (int)(position.X + 1)] == 15,
                2 => mapData[(int)(position.Y + 1), (int)position.X] == 15,
                3 => mapData[(int)position.Y, (int)(position.X - 1)] == 15,
                _ => false,
            };

        private static bool IsNotBlocked(Vector2 position, int direction) =>
            direction switch
            {
                0 => position.Y > 1 && mapData[(int)(position.Y - 1), (int)position.X] == 1,
                1 => position.X < 47 && mapData[(int)position.Y, (int)(position.X + 1)] == 1,
                2 => position.Y < 41 && mapData[(int)(position.Y + 1), (int)position.X] == 1,
                3 => position.X > 1 && mapData[(int)position.Y, (int)(position.X - 1)] == 1,
                _ => false,
            };

        private void GeneratePath()
        {
            Vector2 localPosition = new(position.X, position.Y - 1);
            int localDir = 0;

            //Loop until we get to the end of the path
            while (true)
            {
                //Get the directions we can travel
                List<int> directions = CheckNextDirection(localPosition, localDir);

                if (directions.Count == 0) throw new();

                //Pick one of the directions and add it to the queue
                int direction = directions[Game.Instance.RNG.Next(0, directions.Count)];

                localDir = direction;
                directionChanges.Enqueue(localDir);

                //Move in this direction
                Vector2 vec = localDir switch
                {
                    0 => new(0, -1),
                    1 => new(1, 0),
                    2 => new(0, 1),
                    3 => new(-1, 0),
                    _ => Vector2.Zero,
                };

                localPosition += vec;

                if (IsHeadquarters(localPosition, localDir)) break;
            }
        }

        public Enemy(string name, Vector2 screenPosition, Vector2 gridPosition, Vector2 drawOffset, Animation textures, bool ownership)
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
            health = data.health;
            aabb = new((short)screenPosition.X, (short)screenPosition.Y, 16, 16);
            distanceTravelled = 0;
            base.ownership = ownership;
            directionChanges = new();

            GeneratePath();
        }
    }
}