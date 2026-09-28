using System.Collections;
using CasualGame.Core;
using UnityEngine;

namespace CasualGame.Sandbox
{
    /// <summary>
    /// Eye Merge fusion timeline from Docs/FEEL_SPEC.md §2.4: two balls suck together → white flash (+hit-stop) →
    /// burst effects at the midpoint in the new tier's color → new ball pops in with overshoot. Every number is tunable
    /// here; the final values move into the game's FeelConfig.
    /// </summary>
    public sealed class MergeChainDemo : MonoBehaviour
    {
        [SerializeField] private FxSandbox sandbox;

        [Header("Setup")]
        [SerializeField] private float ballSize = 1.4f;
        [SerializeField] private float startGap = 0.2f;
        [SerializeField] private float idleBefore = 0.35f;
        [SerializeField] private float holdAfter = 0.8f;

        [Header("1. Suck together: the two bodies melt into one goo blob (GooMerge shader)")]
        [SerializeField, Tooltip("Slide together until the two balls just touch.")] private float approachTime = 0.12f;
        [SerializeField, Tooltip("From touching to one blob: the neck forms at contact and the drops flow into each other.")]
        private float meltTime = 0.24f;
        [SerializeField, Tooltip("Neck softness at the end, as a fraction of the ball radius (bigger = gooier).")]
        private float gooBlend = 1.1f;
        [SerializeField, Tooltip("Blob radius when the centers meet, relative to one ball (≈ new tier size keeps it seamless).")]
        private float growTo = 1.29f;
        [SerializeField] private string suckFace = "face_surprised_0";

        [Header("2. White flash + hit-stop")]
        [SerializeField] private float flashTime = 0.035f;
        [SerializeField, Tooltip("Real seconds with time frozen. Spec: 0.04 (tier ≥ 6), 0.07 (tier ≥ 9).")]
        private float hitStop = 0.04f;

        [Header("3. Burst (effect prefab names, played at the midpoint; {color} = color family of the new tier)")]
        [SerializeField] private string[] burstEffects = { "Merge_Fusion_{color}" };
        [SerializeField, Tooltip("Effect scale relative to the new ball (spec: splat = 1.6× its diameter).")]
        private float burstScale = 1.6f;
        [SerializeField] private bool cameraKick;
        [SerializeField] private float kickAmount = 0.12f, kickTime = 0.15f;

        [Header("4. New ball pops in")]
        [SerializeField] private float newBallSize = 1.8f;
        [SerializeField] private float popTime = 0.1f;
        [SerializeField] private float popOvershoot = 1.15f;
        [SerializeField] private float settleTime = 0.22f;
        [SerializeField] private float settleUndershoot = 0.95f;
        [SerializeField] private string newFace = "face_laugh_0";

        private int tier;
        private bool running;

        public void Run()
        {
            if (!running) StartCoroutine(Sequence());
        }

        private IEnumerator Sequence()
        {
            running = true;
            var color = SandboxArt.Palette[tier % SandboxArt.Palette.Length];
            var next = SandboxArt.Palette[(tier + 1) % SandboxArt.Palette.Length];
            tier++;

            var root = new GameObject("MergeChain").transform;
            var x = ballSize * 0.5f + startGap;
            var a = SandboxArt.Character(root, "circle", color, ballSize, "face_happy_0", new Vector3(-x, 1f, 0f));
            var b = SandboxArt.Character(root, "circle", color, ballSize, "face_happy_0", new Vector3(x, 1f, 0f));
            var mid = new Vector3(0f, 1f, 0f);
            yield return new WaitForSeconds(idleBefore);

            // 1. suck: hide the body sprites, draw both bodies as one goo blob, faces ride on top and fade out
            SandboxArt.SetFace(a, suckFace);
            SandboxArt.SetFace(b, suckFace);
            SandboxArt.SetBodyVisible(a, false);
            SandboxArt.SetBodyVisible(b, false);
            var goo = GooMerge.Create(root, 10);
            float r = ballSize * SandboxArt.FillRadius;
            Vector3 a0 = a.position, b0 = b.position;
            var faceA = a.Find("face");
            var faceB = b.Find("face");
            var faceScale = faceA.localScale;
            var touchA = mid + Vector3.left * r * 0.98f;
            var touchB = mid + Vector3.right * r * 0.98f;
            // 1a. approach (accelerating) until they touch
            for (float t = 0f; t <= approachTime; t += Time.deltaTime)
            {
                var k = Tween.Evaluate(Ease.InQuad, Mathf.Clamp01(t / approachTime));
                a.position = Vector3.Lerp(a0, touchA, k);
                b.position = Vector3.Lerp(b0, touchB, k);
                goo.Set(a.position, r, color, b.position, r, color, 0.02f);
                yield return null;
            }
            // 1b. melt: neck snaps open at contact (fast blend), centers flow together (decelerating), volume grows to the new size
            for (float t = 0f; t <= meltTime; t += Time.deltaTime)
            {
                var u = Mathf.Clamp01(t / meltTime);
                var m = Tween.Evaluate(Ease.InOutSine, u);
                a.position = Vector3.Lerp(touchA, mid, m);
                b.position = Vector3.Lerp(touchB, mid, m);
                var rr = r * Mathf.Lerp(1f, growTo, m);
                var blend = r * gooBlend * Tween.Evaluate(Ease.OutQuad, Mathf.Min(1f, u * 2.5f));
                goo.Set(a.position, rr, color, b.position, rr, color, 0.02f + blend);
                faceA.localScale = faceB.localScale = faceScale * Mathf.Lerp(1f, 0.55f, Mathf.SmoothStep(0f, 1f, u * 1.6f));
                var fade = new Color(1f, 1f, 1f, 1f - Mathf.SmoothStep(0.05f, 0.32f, u)); // gone before the two faces overlap
                faceA.GetComponent<SpriteRenderer>().color = faceB.GetComponent<SpriteRenderer>().color = fade;
                yield return null;
            }
            var blobR = r * growTo;
            goo.Set(mid, blobR, color, mid, blobR, color, r * gooBlend);

            // 2. flash + hit-stop
            goo.Set(mid, blobR, color, mid, blobR, color, r * gooBlend, 1f);
            yield return new WaitForSeconds(flashTime);
            if (hitStop > 0f)
            {
                Time.timeScale = 0f;
                yield return new WaitForSecondsRealtime(hitStop);
                Time.timeScale = sandbox.TimeScale;
            }
            Destroy(a.gameObject);
            Destroy(b.gameObject);
            Destroy(goo.gameObject);

            // 3. burst
            foreach (var fx in burstEffects) sandbox.Play(SandboxArt.Colored(fx, next), mid, Color.white, burstScale * newBallSize / 1.6f);
            if (cameraKick) sandbox.Kick(kickAmount, kickTime);

            // 4. pop: starts at the blob's size (seamless) → overshoot → undershoot → 1
            var n = SandboxArt.Character(root, "circle", next, newBallSize, newFace, mid);
            var popFrom = blobR / (newBallSize * SandboxArt.FillRadius);
            for (float t = 0f; t < popTime; t += Time.deltaTime)
            {
                n.localScale = Vector3.one * Mathf.Lerp(popFrom, popOvershoot, Tween.Evaluate(Ease.OutQuad, t / popTime));
                yield return null;
            }
            for (float t = 0f; t < settleTime; t += Time.deltaTime)
            {
                var u = t / settleTime;
                var s = u < 0.5f ? Mathf.Lerp(popOvershoot, settleUndershoot, Tween.Evaluate(Ease.InOutSine, u * 2f))
                                 : Mathf.Lerp(settleUndershoot, 1f, Tween.Evaluate(Ease.InOutSine, u * 2f - 1f));
                n.localScale = Vector3.one * s;
                yield return null;
            }
            n.localScale = Vector3.one;
            yield return new WaitForSeconds(holdAfter);
            Destroy(root.gameObject);
            running = false;
        }
    }
}
