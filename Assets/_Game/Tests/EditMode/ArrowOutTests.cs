using System.Collections.Generic;
using System.Linq;
using CasualGame.ArrowOut;
using NUnit.Framework;
using UnityEngine;

namespace CasualGame.Tests
{
    public class ArrowOutTests
    {
        [Test]
        public void Endless_NeverDeadlocks()
        {
            var free = new List<Arrow>();
            int turns = 0;
            for (int seed = 1; turns < 4000; seed++)
            {
                var run = new EndlessRun(Rng.Seeded(seed));
                while (!run.Over && turns < 4000)
                {
                    run.Board.FreeArrows(free);
                    Assert.IsTrue(run.Board.Count == 0 || free.Count > 0, $"deadlock at turn {turns} (seed {seed})");
                    Assert.IsFalse(run.Board.HasCycle(), "cycle in board");
                    run.Exit(free[(int)(run.Rand() * free.Count)].Id);
                    turns++;
                }
            }
        }

        [Test]
        public void Endless_SkillMatters()
        {
            float Play(bool smart, int seed)
            {
                var run = new EndlessRun(Rng.Seeded(seed));
                var free = new List<Arrow>();
                var tmp = new List<Arrow>();
                while (!run.Over && run.Cleared < 600)
                {
                    run.Board.FreeArrows(free);
                    Arrow pick;
                    if (!smart) pick = free[(int)(run.Rand() * free.Count)];
                    else
                    {
                        pick = free.OrderByDescending(a =>
                        {
                            var same = a.Color == run.ComboColor ? 100 : 0;
                            var supply = free.Count(f => f.Color == a.Color) * 10;
                            run.Board.Remove(a.Id);
                            run.Board.FreeArrows(tmp);
                            var opened = tmp.Count;
                            run.Board.Place(a);
                            return same + supply + opened;
                        }).First();
                    }
                    run.Exit(pick.Id);
                }
                return run.Score;
            }

            float random = 0, smartScore = 0;
            for (int s = 0; s < 40; s++) { random += Play(false, 500 + s); smartScore += Play(true, 500 + s); }
            Debug.Log($"skill gap x{smartScore / random:0.00}");
            Assert.Greater(smartScore, random * 1.5f, "a planning player must clearly out-score a random one");
        }

        [Test]
        public void Levels_AllSolvableAndRampUp()
        {
            var json = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Game/Data/ArrowOut/levels.json");
            Assert.IsNotNull(json, "bake levels first: Tools/Casual Game/Arrow Out/Bake 100 Levels");
            var pack = JsonUtility.FromJson<LevelPack>(json.text);
            Assert.AreEqual(100, pack.levels.Length);

            var free = new List<Arrow>();
            foreach (var level in pack.levels)
            {
                var b = level.ToBoard();
                Assert.IsFalse(b.HasCycle(), $"level {level.n} can deadlock");
                while (b.Count > 0)
                {
                    b.FreeArrows(free);
                    Assert.Greater(free.Count, 0, $"level {level.n} is stuck");
                    b.Remove(free[0].Id);
                }
            }

            // Only boss swaps may break the order, and each boss must be harder than the level before it.
            for (int i = 4; i < 100; i++)
            {
                var n = i + 1;
                var cur = pack.levels[i].difficulty;
                var prev = pack.levels[i - 1].difficulty;
                bool nearBoss = n % 10 == 0 || (n - 3) % 10 == 0 || (n - 1) % 10 == 0 || (n - 4) % 10 == 0;
                if (n % 10 == 0) Assert.Greater(cur, prev, $"boss {n} is not a spike");
                else if (!nearBoss) Assert.GreaterOrEqual(cur, prev - 0.001f, $"level {n} easier than {n - 1}");
            }
            Assert.Greater(pack.levels[99].difficulty, pack.levels[3].difficulty * 20f);
        }
    }
}
