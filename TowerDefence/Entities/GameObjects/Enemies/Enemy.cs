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

        public static Dictionary<string, Animation> enemyAnims = new();
        public static Dictionary<string, EnemyData> enemyDatas = new();
        public static byte[,] mapData;

        private Queue<int> directionChanges;

        private ulong distanceTravelled;
        private int health;
        private bool checkForPosMovement = false;
        private Animation frames;
        private EnemyData data;
        private Vector2 drawOffset;
        private Vector2 absolutePosition;
        private int dir; //0 - up, 1 - right, 2 - down, 3 - left,
        private bool damagedThisFrame = false;

        private float elapsedTime;

        public ulong TotalDistance => distanceTravelled;

        //|1,X,Y,DIR,DIST,HEALTH,ELAPSED,NAME,DIRECTIONCHANGES (CSV using ;)
        public override string Serialise()
        {
            StringBuilder sb = new();

            sb.Append($"|1,{position.X},{position.Y},{dir},{distanceTravelled},{health},{elapsedTime},{data.name},");

            StringBuilder directionChanges = new();
            List<int> directions = this.directionChanges.ToList();
            if (directions.Count > 0)
            {
                for (int i = 0; i < directions.Count; i++)
                {
                    directionChanges.Append(directions[i]);
                    directionChanges.Append(';');
                }
                sb.Append(directionChanges);
                sb.Length--;
            }
            else
            {
                sb.Append('0');
            }

            return sb.ToString();
        }

        public override Entity Deserialise(string serialised)
        {
            try
            {
                string[] csv = serialised.Split(',');

                //Acquire all of the data we need
                float absX = (float.Parse(csv[1]) * Game.TileSize) + Game.Instance.OpponentGameOffset.X;
                float absY = (float.Parse(csv[2]) * Game.TileSize) + Game.Instance.OpponentGameOffset.Y;

                float x = float.Parse(csv[1]);
                float y = float.Parse(csv[2]);

                int dir = int.Parse(csv[3]);

                ulong distanceTravelled = ulong.Parse(csv[4]);

                int health = int.Parse(csv[5]);

                float elapsedTime = float.Parse(csv[6]);

                string name = csv[7];

                string[] dirChangesCsv = csv[8].Split(';');

                //Write the data we have now
                drawOffset = Game.Instance.PlayerGameOffset;
                absolutePosition = new(absX, absY);
                ownership = false;

                position = new(x, y);

                this.dir = dir;
                this.distanceTravelled = distanceTravelled;
                this.health = health;
                this.elapsedTime = elapsedTime;
                data = enemyDatas[name];
                frames = enemyAnims[name];

                directionChanges = new();
                foreach (string s in dirChangesCsv)
                {
                    if (s == string.Empty) continue;
                    directionChanges.Enqueue(int.Parse(s));
                }

                aabb = new((short)absX, (short)absY, 16, 16);

                return this;
            }
            catch { return null; }
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
            if (health <= 0 && ownership == true)
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

        public Enemy(string serialised) => Deserialise(serialised);

        public Enemy(string name, Vector2 screenPosition, Vector2 gridPosition, Vector2 drawOffset, bool ownership)
        {
            absolutePosition = screenPosition;
            position = gridPosition;
            frames = enemyAnims[name];
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