using System;
using CasualGame.Core;

namespace CasualGame.ArrowOut
{
    /// <summary>Saved Arrow Out progress: highest cleared level, stars per level, hints, endless/daily bests.</summary>
    public static class ArrowProgress
    {
        public const int LevelCount = 100;
        private const int StartHints = 3;

        [Serializable]
        private class StarData { public int[] stars = new int[LevelCount]; }

        private static StarData cache;
        private static StarData Data => cache ??= Load();

        private static StarData Load()
        {
            var d = SaveStore.GetJson<StarData>("arrow.stars");
            if (d.stars == null || d.stars.Length != LevelCount) d.stars = new int[LevelCount];
            return d;
        }

        public static int Cleared => SaveStore.GetInt("arrow.cleared");
        public static int Unlocked => Math.Min(LevelCount, Cleared + 1);
        public static int Stars(int level) => level >= 1 && level <= LevelCount ? Data.stars[level - 1] : 0;

        public static int TotalStars
        {
            get
            {
                int sum = 0;
                foreach (var s in Data.stars) sum += s;
                return sum;
            }
        }

        public static void Complete(int level, int stars)
        {
            Data.stars[level - 1] = Math.Max(Data.stars[level - 1], stars);
            SaveStore.SetJson("arrow.stars", Data);
            if (level > Cleared) SaveStore.SetInt("arrow.cleared", level);
            SaveStore.Save();
        }

        public static int Hints
        {
            get => SaveStore.GetInt("arrow.hints", StartHints);
            set { SaveStore.SetInt("arrow.hints", Math.Max(0, value)); SaveStore.Save(); }
        }

        public static int EndlessBest => SaveStore.GetInt("arrow.endless.best");
        public static int SubmitEndless(int score) => SaveStore.SubmitBest("arrow.endless.best", score);

        public static string DailyKey => $"arrow.daily.{Rng.DailySeed(DateTime.Now)}";
        public static int DailyBest => SaveStore.GetInt(DailyKey);
        public static int SubmitDaily(int score) => SaveStore.SubmitBest(DailyKey, score);

        public static bool TutorialSeen
        {
            get => SaveStore.GetBool("arrow.tutorial", false);
            set => SaveStore.SetBool("arrow.tutorial", value);
        }
    }
}
