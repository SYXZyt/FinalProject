﻿using System.Xml;
using Microsoft.Xna.Framework;
using TowerDefence.Entities.GameObjects.Enemies;

namespace TowerDefence.Waves
{
    internal sealed class Wave
    {
        public static List<Wave> waves;

        public enum WaveState
        {
            SPAWNING,
            COOLDOWN,
        }

        private List<SpawnGroup> groups;
        private SpawnGroup? active;
        private Queue<Enemy> enemiesBuffer;
        private WaveState state;

        private double elapsedTime = 0;
        private int spawned = 0;

        public bool IsOver => !groups.Any() && active is null && enemiesBuffer.Count == 0;

        public Wave DeepCopy()
        {
            Wave copy = new()
            {
                groups = new(),
                enemiesBuffer = new(),
                state = state,
                elapsedTime = elapsedTime,
                spawned = spawned
            };

            if (active is null) copy.active = null;
            else copy.active = new((SpawnGroup)active);

            //Copy over the enemyBuffer
            if (enemiesBuffer.Any())
            {
                List<Enemy> buffer = enemiesBuffer.ToList();
                foreach (Enemy e in buffer) copy.enemiesBuffer.Enqueue(e);
            }

            //Finally, copy the groups
            foreach (SpawnGroup group in groups) copy.groups.Add(group);

            return copy;
        }

        public void Update(GameTime gameTime)
        {
            elapsedTime += (gameTime.ElapsedGameTime.TotalSeconds * 20);

            if (active is null)
            {
                if (groups.Count == 0) return;

                active = groups[0];
                groups.RemoveAt(0);
            }

            System.Diagnostics.Debug.WriteLine($"WAVE: {state} - ({spawned}/{((SpawnGroup)active).count}) - ({elapsedTime}/{((SpawnGroup)active).delay})");

            if (state == WaveState.SPAWNING)
            {
                //If our elapsed time is over the set time, then we need to spawn and reset
                if (elapsedTime > ((SpawnGroup)active).delay)
                {
                    elapsedTime = 0;
                    Scenes.Game.Instance.SpawnEnemyFromWave(((SpawnGroup)active).id);
                    spawned++;
                }

                if (spawned == ((SpawnGroup)active).count)
                {
                    state = WaveState.COOLDOWN;
                }
            }
            else
            {
                //Check if the cooldown is over
                if (elapsedTime > ((SpawnGroup)active).cooldown)
                {
                    state = WaveState.SPAWNING;

                    if (groups.Count > 0)
                    {
                        active = groups[0];
                        groups.RemoveAt(0);
                        spawned = 0;
                    }
                    else
                    {
                        active = null;
                        spawned = 0;
                    }

                    elapsedTime = 0;
                }
            }
        }

        public void AddToSpawnGroup(Enemy enemy)
        {
            enemiesBuffer.Enqueue(enemy);
        }

        public static void GenerateWave(string xmlFile)
        {
            Wave wave = new();

            XmlDocument xml = new();
            xml.Load(xmlFile);

            int groupIndex = 0;
            while (xml.SelectSingleNode($"/SpawnGroups/SpawnGroup{groupIndex}") is not null)
            {
                SpawnGroup group = new();

                XmlNode id = xml.SelectSingleNode($"/SpawnGroups/SpawnGroup{groupIndex}/id");
                XmlNode count = xml.SelectSingleNode($"/SpawnGroups/SpawnGroup{groupIndex}/count");
                XmlNode delay = xml.SelectSingleNode($"/SpawnGroups/SpawnGroup{groupIndex}/delay");
                XmlNode cooldown = xml.SelectSingleNode($"/SpawnGroups/SpawnGroup{groupIndex}/cooldown");

                group.id = id.InnerText;
                group.count = int.Parse(count.InnerText);
                group.delay = float.Parse(delay.InnerText);
                group.cooldown = float.Parse(cooldown.InnerText);

                wave.groups.Add(group);
                groupIndex++;
            }

            waves.Add(wave);
        }

        static Wave()
        {
            waves = new();
        }

        public Wave()
        {
            enemiesBuffer = new();
            groups = new();
            state = WaveState.SPAWNING;
        }
    }
}