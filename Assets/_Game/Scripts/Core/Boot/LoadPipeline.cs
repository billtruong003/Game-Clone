using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CasualGame.Core
{
    /// <summary>
    /// The boot's real loading: a list of weighted jobs that all start on the Boot scene's first frame and run side by
    /// side. <see cref="Progress"/> is their weighted sum (0..1), which the loading bar shows. New work (the shop's skin
    /// assets, store prices) is one more <see cref="Add"/> call; nothing else changes.
    /// </summary>
    public sealed class LoadPipeline
    {
        private sealed class Job
        {
            public string Name;
            public float Weight, Progress;
            public bool Done;
            public Func<Action<float>, IEnumerator> Run;
        }

        private readonly List<Job> jobs = new();

        /// <param name="run">The work; reports its own progress 0..1 through the callback (the pipeline marks it 1 when it ends).</param>
        public void Add(string name, float weight, Func<Action<float>, IEnumerator> run) =>
            jobs.Add(new Job { Name = name, Weight = Mathf.Max(0.01f, weight), Run = run });

        public float Progress
        {
            get
            {
                float sum = 0f, total = 0f;
                foreach (var j in jobs) { sum += j.Weight * j.Progress; total += j.Weight; }
                return total > 0f ? sum / total : 1f;
            }
        }

        public bool Done
        {
            get
            {
                foreach (var j in jobs) if (!j.Done) return false;
                return true;
            }
        }

        private static float startedAt;

        public void Start(MonoBehaviour host)
        {
            startedAt = Time.realtimeSinceStartup;
            foreach (var j in jobs) host.StartCoroutine(RunJob(j));
        }

        private static IEnumerator RunJob(Job j)
        {
            var routine = j.Run(p => j.Progress = Mathf.Max(j.Progress, Mathf.Clamp01(p)));
            // a job that throws must not hang the boot: it counts as done and the game loads without it
            while (true)
            {
                bool more;
                try { more = routine.MoveNext(); }
                catch (Exception e)
                {
                    Debug.LogWarning($"Boot job '{j.Name}' failed: {e.Message}");
                    break;
                }
                if (!more) break;
                yield return routine.Current;
            }
            j.Progress = 1f;
            j.Done = true;
            LogDone(j.Name);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private static void LogDone(string name) => Debug.Log($"[Boot] job '{name}' done at {Time.realtimeSinceStartup - startedAt:F2} s");
    }

    /// <summary>The jobs the boot runs today. Each is a coroutine reporting 0..1.</summary>
    public static class BootJobs
    {
        /// <summary>Loads the game scene in the background and holds it just before activation (Unity stops at 0.9).</summary>
        public static Func<Action<float>, IEnumerator> Scene(string sceneName, Action<AsyncOperation> started) => report => SceneRoutine(sceneName, started, report);

        private static IEnumerator SceneRoutine(string sceneName, Action<AsyncOperation> started, Action<float> report)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null) yield break;
            op.allowSceneActivation = false;
            started(op);
            while (op.progress < 0.9f)
            {
                report(op.progress / 0.9f);
                yield return null;
            }
        }

        /// <summary>Reads the save file now so the first SaveStore call in the game does not hitch.</summary>
        public static IEnumerator Save(Action<float> report)
        {
            SaveStore.GetInt("boot.warm", 0);
            report(1f);
            yield break;
        }

        /// <summary>
        /// Loads the clips that are not preloaded with the scene (the music: streamed in the background), so the intro
        /// starts on time. SFX are preloaded with the Boot scene already; loading them here would decompress them on the
        /// main thread and freeze the credits.
        /// </summary>
        public static Func<Action<float>, IEnumerator> Audio(AudioLibrary library) => report => AudioRoutine(library, report);

        private static IEnumerator AudioRoutine(AudioLibrary library, Action<float> report)
        {
            if (library == null) yield break;
            var clips = new List<AudioClip>();
            foreach (var c in library.Clips)
                if (!c.preloadAudioData && c.loadInBackground) clips.Add(c);
            for (int i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];
                if (clip != null && clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
                while (clip != null && clip.loadState == AudioDataLoadState.Loading) yield return null;
                report((i + 1f) / clips.Count);
            }
        }

        /// <summary>
        /// Compiles every shader variant once, so the first merge / clear does not stall on a shader. It blocks the main
        /// thread for a moment, so it waits for <paramref name="quiet"/>: a still frame (the Made with Unity hold) where
        /// a hitch cannot be seen.
        /// </summary>
        public static Func<Action<float>, IEnumerator> Shaders(Func<bool> quiet) => report => ShaderRoutine(quiet, report);

        private static IEnumerator ShaderRoutine(Func<bool> quiet, Action<float> report)
        {
            while (!quiet()) yield return null;
            Shader.WarmupAllShaders();
            report(1f);
        }

        /// <summary>Builds the TMP atlas glyphs the first screens use (digits, both languages' letters).</summary>
        public static IEnumerator Fonts(Action<float> report)
        {
#if !UNITY_EDITOR // in the editor TMP writes the added glyphs into the font asset on disk
            var font = TMPro.TMP_Settings.defaultFontAsset;
            if (font != null && font.atlasPopulationMode == TMPro.AtlasPopulationMode.Dynamic)
                font.TryAddCharacters("0123456789+-x%.,:!?/ ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzăâđêôơưáàảãạéèẻẽẹíìỉĩịóòỏõọúùủũụýỳỷỹỵ");
#endif
            report(1f);
            yield break;
        }
    }
}
