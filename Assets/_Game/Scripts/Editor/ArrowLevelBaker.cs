using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CasualGame.ArrowOut;
using UnityEditor;
using UnityEngine;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// Bakes the 100 Arrow Out levels into Resources/ArrowOut/levels.json. Seeded, so the result is reproducible.
    /// Per level: 60 candidates (board grows, arrows lengthen, coverage 55% → 100%), pick the one nearest an
    /// exponential difficulty curve, sort everything after the tutorial by difficulty, then give every 10th level
    /// ("boss") the level three steps harder.
    /// </summary>
    public static class ArrowLevelBaker
    {
        public const string OutputPath = "Assets/_Game/Data/ArrowOut/levels.json";
        private const int Count = 100;
        private const int Candidates = 60;
        private const int Tutorial = 3;

        [MenuItem("Tools/Casual Game/Arrow Out/Bake 100 Levels")]
        public static void Bake()
        {
            var pool = new List<LevelData>[Count];
            try
            {
                for (int i = 0; i < Count; i++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Arrow Out", $"Generating level {i + 1}/{Count}", i / (float)Count)) return;
                    pool[i] = Enumerable.Range(0, Candidates).Select(k => Build(i + 1, (i + 1) * 1000 + k, k)).ToList();
                }
            }
            finally { EditorUtility.ClearProgressBar(); }

            const float dMin = 0.5f;
            var dMax = pool[Count - 1].Max(c => c.difficulty) * 0.8f;
            var picked = new List<LevelData>();
            for (int i = 0; i < Count; i++)
            {
                var n = i + 1;
                var cands = pool[i];
                if (n <= Tutorial) { picked.Add(cands.OrderBy(c => c.difficulty).First()); continue; }
                var target = dMin * Mathf.Pow(dMax / dMin, (n - Tutorial - 1f) / (Count - Tutorial - 1f));
                var nonTrivial = cands.Where(c => c.difficulty > 0f).ToList();
                picked.Add((nonTrivial.Count > 0 ? nonTrivial : cands).OrderBy(c => Mathf.Abs(c.difficulty - target)).First());
            }

            var levels = picked.Take(Tutorial).Concat(picked.Skip(Tutorial).OrderBy(l => l.difficulty)).ToList();
            for (int boss = 9; boss + 3 < Count; boss += 10) (levels[boss], levels[boss + 3]) = (levels[boss + 3], levels[boss]);
            for (int i = 0; i < levels.Count; i++) levels[i].n = i + 1;

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath)!);
            File.WriteAllText(OutputPath, JsonUtility.ToJson(new LevelPack { levels = levels.ToArray() }));
            AssetDatabase.ImportAsset(OutputPath);
            Debug.Log("Arrow Out: baked 100 levels. " + string.Join("  ", new[] { 1, 10, 30, 50, 70, 90, 100 }
                .Select(n => $"#{n} {levels[n - 1].w}x{levels[n - 1].h} {levels[n - 1].arrows.Length}a d={levels[n - 1].difficulty:0.0}")));
        }

        private static LevelData Build(int n, int seed, int k)
        {
            var t = (n - 1f) / (Count - 1f);
            int w = 3 + Mathf.RoundToInt(4 * Mathf.Pow(t, 0.45f));
            int h = 3 + Mathf.RoundToInt(6 * Mathf.Pow(t, 0.45f));
            int minLen = 1 + Mathf.FloorToInt(3 * t);
            int maxLen = Math.Max(minLen + 1, 2 + Mathf.RoundToInt(6 * t) + k % 3 - 1);
            float coverage = Mathf.Min(1f, 0.55f + 0.45f * Mathf.Pow(t, 0.5f) + (k % 5 - 2) * 0.04f);

            var rand = Rng.Seeded(seed);
            var board = new Board(Board.CenteredMask(w, h));
            int covered = 0, area = w * h;
            while (covered < coverage * area)
            {
                var a = board.Spawn(rand, minLen, maxLen, 4);
                if (a == null) break;
                covered += a.Cells.Length;
            }
            var level = LevelData.FromBoard(n, w, h, board, covered / (float)area);
            level.difficulty = level.GuessMistakes(Rng.Seeded(seed ^ 0x5bd1e995));
            return level;
        }
    }
}
