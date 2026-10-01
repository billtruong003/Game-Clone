using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CasualGame.Core
{
    public enum Ease { Linear, InQuad, OutQuad, InCubic, OutCubic, OutBack, InBack, InOutSine, OutElastic, OutBounce }

    /// <summary>
    /// Tiny allocation-light tween engine (no third-party dependency). One runner updates every job;
    /// jobs whose owner was destroyed stop silently, so scene changes never leave dangling tweens.
    /// </summary>
    public sealed class Tween : MonoBehaviour
    {
        private class Job
        {
            public UnityEngine.Object owner;
            public float delay, duration, elapsed;
            public Ease ease;
            public bool unscaled;
            public Action<float> apply;
            public Action done;
        }

        private static Tween runner;
        private readonly List<Job> jobs = new(64);
        private readonly Stack<Job> spare = new();

        private static Tween Runner
        {
            get
            {
                if (runner != null) return runner;
                var go = new GameObject("TweenRunner");
                DontDestroyOnLoad(go);
                return runner = go.AddComponent<Tween>();
            }
        }

        public static void Run(UnityEngine.Object owner, float duration, Action<float> apply, Ease ease = Ease.OutQuad,
            float delay = 0f, Action done = null, bool unscaled = false)
        {
            var r = Runner;
            var job = r.spare.Count > 0 ? r.spare.Pop() : new Job();
            job.owner = owner;
            job.delay = delay;
            job.duration = Mathf.Max(0.0001f, duration);
            job.elapsed = 0f;
            job.ease = ease;
            job.unscaled = unscaled;
            job.apply = apply;
            job.done = done;
            r.jobs.Add(job);
        }

        /// <summary>Endless animation (tutorial hands, hint pulses): <paramref name="apply"/> gets the seconds elapsed.
        /// Runs until the owner is destroyed or killed; a fixed-length tween would freeze mid-move on a slow player.</summary>
        public static void Loop(UnityEngine.Object owner, Action<float> apply, bool unscaled = false) =>
            Run(owner, 86400f, k => apply(k * 86400f), Ease.Linear, 0f, null, unscaled);

        public static void Delay(UnityEngine.Object owner, float seconds, Action done, bool unscaled = false) =>
            Run(owner, seconds, null, Ease.Linear, 0f, done, unscaled);

        public static void Kill(UnityEngine.Object owner)
        {
            if (runner == null) return;
            foreach (var j in runner.jobs)
                if (j.owner == owner) j.owner = null; // marks as dead; removed next frame without firing done
        }

        // ---- common shortcuts ----
        public static void Scale(Transform t, Vector3 to, float duration, Ease ease = Ease.OutBack, float delay = 0f, Action done = null)
        {
            var from = t.localScale;
            Run(t, duration, k => t.localScale = Vector3.LerpUnclamped(from, to, k), ease, delay, done);
        }

        public static void Move(Transform t, Vector3 to, float duration, Ease ease = Ease.OutCubic, float delay = 0f, Action done = null)
        {
            var from = t.localPosition;
            Run(t, duration, k => t.localPosition = Vector3.LerpUnclamped(from, to, k), ease, delay, done);
        }

        public static void Anchored(RectTransform t, Vector2 to, float duration, Ease ease = Ease.OutCubic, float delay = 0f, Action done = null)
        {
            var from = t.anchoredPosition;
            Run(t, duration, k => t.anchoredPosition = Vector2.LerpUnclamped(from, to, k), ease, delay, done);
        }

        public static void Fade(CanvasGroup g, float to, float duration, float delay = 0f, Action done = null, bool unscaled = true)
        {
            var from = g.alpha;
            Run(g, duration, k => g.alpha = Mathf.Lerp(from, to, k), Ease.OutQuad, delay, done, unscaled);
        }

        public static void Color(Graphic g, Color to, float duration, float delay = 0f)
        {
            var from = g.color;
            Run(g, duration, k => g.color = UnityEngine.Color.LerpUnclamped(from, to, k), Ease.OutQuad, delay);
        }

        // Rest scale of every transform being punched: a punch that starts while another is still running must
        // bounce around the same rest size, not the enlarged one (otherwise fast combos make the label grow for good).
        private static readonly Dictionary<Transform, (Vector3 rest, int running)> punchRest = new();

        public static void Punch(Transform t, float amount = 0.18f, float duration = 0.25f)
        {
            if (!punchRest.TryGetValue(t, out var p))
            {
                if (punchRest.Count > 32) foreach (var dead in new List<Transform>(punchRest.Keys)) if (dead == null) punchRest.Remove(dead);
                p = (t.localScale, 0);
            }
            var rest = p.rest;
            punchRest[t] = (rest, p.running + 1);
            Run(t, duration, k => t.localScale = rest * (1f + amount * Mathf.Sin(k * Mathf.PI)), Ease.Linear, 0f, () =>
            {
                t.localScale = rest;
                if (punchRest.TryGetValue(t, out var q) && q.running > 1) punchRest[t] = (q.rest, q.running - 1);
                else punchRest.Remove(t);
            });
        }

        private void Update()
        {
            float dt = Time.deltaTime, udt = Time.unscaledDeltaTime;
            for (int i = jobs.Count - 1; i >= 0; i--)
            {
                var j = jobs[i];
                if (j.owner == null) { Recycle(i); continue; }
                var step = j.unscaled ? udt : dt;
                if (j.delay > 0f) { j.delay -= step; continue; }
                j.elapsed += step;
                var k = Mathf.Clamp01(j.elapsed / j.duration);
                // One broken tween (e.g. it writes to an object destroyed mid-flight) must not stop every other tween:
                // an exception here would abort the loop and hit again every frame, freezing popups for good.
                try { j.apply?.Invoke(Evaluate(j.ease, k)); }
                catch (Exception e) { Debug.LogException(e); Recycle(i); continue; }
                if (k < 1f) continue;
                var done = j.done;
                Recycle(i);
                try { done?.Invoke(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        private void Recycle(int i)
        {
            var j = jobs[i];
            jobs.RemoveAt(i);
            j.owner = null;
            j.apply = null;
            j.done = null;
            spare.Push(j);
        }

        public static float Evaluate(Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.InQuad: return t * t;
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.InCubic: return t * t * t;
                case Ease.OutCubic: { var u = 1f - t; return 1f - u * u * u; }
                case Ease.OutBack: { const float c = 1.70158f; var u = t - 1f; return 1f + (c + 1f) * u * u * u + c * u * u; }
                case Ease.InBack: { const float c = 1.70158f; return (c + 1f) * t * t * t - c * t * t; }
                case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                case Ease.OutElastic:
                    if (t <= 0f || t >= 1f) return t;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
                case Ease.OutBounce:
                {
                    const float n = 7.5625f, d = 2.75f;
                    if (t < 1f / d) return n * t * t;
                    if (t < 2f / d) { t -= 1.5f / d; return n * t * t + 0.75f; }
                    if (t < 2.5f / d) { t -= 2.25f / d; return n * t * t + 0.9375f; }
                    t -= 2.625f / d;
                    return n * t * t + 0.984375f;
                }
                default: return t;
            }
        }
    }
}
