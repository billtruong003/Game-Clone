using System.Collections;
using CasualGame.Core;
using UnityEngine;

namespace CasualGame.Sandbox
{
    /// <summary>
    /// Eye Blast row clear from Docs/FEEL_SPEC.md §3: a light sweep runs along the row, then each block flashes white and
    /// pops (effect in the block's color), in a wave spreading from the placed block. Run(3) = a multi-line clear with an
    /// impact frame, a big burst and a camera kick.
    /// </summary>
    public sealed class LineClearDemo : MonoBehaviour
    {
        [SerializeField] private FxSandbox sandbox;

        [Header("Setup")]
        [SerializeField] private int columns = 8;
        [SerializeField] private float cell = 1.15f;
        [SerializeField, Tooltip("Column of the block just placed: the wave starts here.")] private int origin = 3;
        [SerializeField] private float idleBefore = 0.35f;

        [Header("Sweep")]
        [SerializeField] private float sweepTime = 0.18f;
        [SerializeField, Range(0f, 1f)] private float sweepAlpha = 0.7f;

        [Header("Per block")]
        [SerializeField, Tooltip("Delay per column away from the origin.")] private float stagger = 0.035f;
        [SerializeField] private float flashTime = 0.05f;
        [SerializeField] private float shrinkTime = 0.1f;
        [SerializeField, Tooltip("{color} = color family of the block")] private string[] popEffects = { "Blast_BlockPop_{color}" };
        [SerializeField] private float popScale = 1f;

        [Header("Multi-line extras (Run(3))")]
        [SerializeField, Tooltip("Two-frame silhouette freeze before the blocks pop (ImpactFrame).")] private bool impactFrame = true;
        [SerializeField, Tooltip("Played once at the center of the cleared rows. Empty = none.")] private string multiLineEffect = "Blast_MultiLine";
        [SerializeField] private float multiLineEffectScale = 1.4f;
        [SerializeField] private bool cameraKick = true;
        [SerializeField] private float kickAmount = 0.14f, kickTime = 0.18f;

        private bool running;
        private ImpactFrame impact;

        public void Run() => Run(1);

        public void Run(int rows)
        {
            if (!running) StartCoroutine(Sequence(Mathf.Max(1, rows)));
        }

        private IEnumerator Sequence(int rows)
        {
            running = true;
            var root = new GameObject("LineClear").transform;
            var top = 3.5f;
            var left = -(columns - 1) * cell * 0.5f;
            var blocks = new Transform[rows, columns];
            var colors = new Color[rows, columns];
            for (int r = 0; r < rows; r++)
            for (int i = 0; i < columns; i++)
            {
                colors[r, i] = SandboxArt.Palette[Random.Range(0, SandboxArt.Palette.Length)];
                blocks[r, i] = SandboxArt.Character(root, "block", colors[r, i], cell * 0.96f, "face_happy_0", new Vector3(left + i * cell, top - r * cell, 0f));
            }
            yield return new WaitForSeconds(idleBefore);
            foreach (var bl in blocks) SandboxArt.SetFace(bl, "face_starstruck_0");

            // sweep: a white bar travelling along each row
            if (sweepTime > 0f)
            {
                var bars = new SpriteRenderer[rows];
                for (int r = 0; r < rows; r++)
                {
                    bars[r] = SandboxArt.Sprite(root, "round_rect", new Color(1f, 1f, 1f, sweepAlpha), cell * 0.5f, 40);
                    bars[r].transform.localScale = new Vector3(bars[r].transform.localScale.x, bars[r].transform.localScale.x * 2.2f, 1f);
                }
                for (float t = 0f; t < sweepTime; t += Time.deltaTime)
                {
                    var u = t / sweepTime;
                    for (int r = 0; r < rows; r++)
                    {
                        bars[r].transform.position = new Vector3(Mathf.Lerp(left - cell, -left + cell, u), top - r * cell, 0f);
                        bars[r].color = new Color(1f, 1f, 1f, sweepAlpha * Mathf.Sin(u * Mathf.PI));
                    }
                    yield return null;
                }
                foreach (var bar in bars) Destroy(bar.gameObject);
            }

            if (rows > 1)
            {
                if (impactFrame)
                {
                    if (impact == null) impact = GetComponent<ImpactFrame>();
                    if (impact == null) impact = gameObject.AddComponent<ImpactFrame>(); // (no ?? on Unity objects: fake null)
                    yield return impact.Run(Camera.main);
                }
                var center = new Vector3(0f, top - (rows - 1) * cell * 0.5f, 0f);
                if (!string.IsNullOrEmpty(multiLineEffect)) sandbox.Play(multiLineEffect, center, Color.white, multiLineEffectScale);
                if (cameraKick) sandbox.Kick(kickAmount, kickTime);
            }
            for (int r = 0; r < rows; r++)
            for (int i = 0; i < columns; i++)
                StartCoroutine(PopBlock(blocks[r, i], colors[r, i], (Mathf.Abs(i - origin) + r) * stagger));
            yield return new WaitForSeconds((Mathf.Max(origin, columns - 1 - origin) + rows) * stagger + flashTime + shrinkTime + 0.8f);
            Destroy(root.gameObject);
            running = false;
        }

        private IEnumerator PopBlock(Transform block, Color color, float delay)
        {
            yield return new WaitForSeconds(delay);
            SandboxArt.SetFlash(block, 1f);
            yield return new WaitForSeconds(flashTime);
            foreach (var fx in popEffects) sandbox.Play(SandboxArt.Colored(fx, color), block.position, Color.white, popScale);
            for (float t = 0f; t < shrinkTime; t += Time.deltaTime)
            {
                block.localScale = Vector3.one * (1f - Tween.Evaluate(Ease.InBack, t / shrinkTime));
                yield return null;
            }
            block.gameObject.SetActive(false);
        }
    }
}
