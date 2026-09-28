using System.Collections;
using CasualGame.Core;
using UnityEngine;

namespace CasualGame.Sandbox
{
    /// <summary>
    /// A ball falls, stretches with speed, lands with a jelly squash + wobble (JellyWobble), squints on impact, bounces
    /// a little and puffs smoke on both sides of the contact point.
    /// </summary>
    public sealed class DropDemo : MonoBehaviour
    {
        [SerializeField] private FxSandbox sandbox;

        [Header("Fall")]
        [SerializeField] private float ballSize = 1.4f;
        [SerializeField] private float dropHeight = 5f;
        [SerializeField] private float floorY = -3.2f;
        [SerializeField] private float gravity = 30f;
        [SerializeField, Tooltip("Vertical stretch per unit of speed while falling.")] private float fallStretch = 0.008f;
        [SerializeField] private float maxFallStretch = 0.14f;
        [SerializeField, Range(0f, 0.8f)] private float bounce = 0.28f;

        [Header("Landing")]
        [SerializeField, Tooltip("Impact speed that gives a full-strength squash.")] private float fullImpactSpeed = 16f;
        [SerializeField] private string[] landEffects = { "Land_Poof" };
        [SerializeField, Tooltip("Smoke puffs this far left/right of the contact point, in ball radii.")] private float puffSpread = 0.95f;
        [SerializeField] private float puffScale = 0.28f;
        [SerializeField, Tooltip("Minimum impact speed that still puffs smoke (small bounces stay quiet).")] private float puffMinSpeed = 5f;
        [SerializeField] private string squintFace = "face_laugh_0";
        [SerializeField] private float squintTime = 0.18f;
        [SerializeField] private float holdAfter = 0.9f;

        private bool running;

        public void Run()
        {
            if (!running) StartCoroutine(Sequence());
        }

        private IEnumerator Sequence()
        {
            running = true;
            var root = new GameObject("DropDemo").transform;
            float r = ballSize * SandboxArt.FillRadius;
            SandboxArt.Sprite(root, "round_rect", new Color(0.1f, 0.11f, 0.22f, 1f), 6f, 1).transform.position = new Vector3(0f, floorY - 0.3f, 0f);
            var floor = root.GetChild(0);
            floor.localScale = new Vector3(floor.localScale.x, floor.localScale.x * 0.1f, 1f);

            // the ball root moves; its "visual" child squashes (same split the Merge game will use: physics root + visual)
            var ball = new GameObject("Ball").transform;
            ball.SetParent(root, false);
            var visual = SandboxArt.Character(ball, "circle", SandboxArt.Palette[4], ballSize, "face_happy_0", Vector3.zero);
            var wobble = visual.gameObject.AddComponent<JellyWobble>();
            float y = floorY + r + dropHeight, v = 0f;
            ball.position = new Vector3(0f, y, 0f);
            yield return new WaitForSeconds(0.25f);

            int bounces = 0;
            while (bounces < 4)
            {
                v -= gravity * Time.deltaTime;
                y += v * Time.deltaTime;
                if (y <= floorY + r)
                {
                    y = floorY + r;
                    float speed = -v;
                    visual.localScale = Vector3.one;
                    wobble.Impact(speed / fullImpactSpeed, Vector2.up, r);
                    if (speed >= puffMinSpeed)
                    {
                        var contact = new Vector3(0f, floorY, 0f);
                        foreach (var fx in landEffects)
                        {
                            sandbox.Play(fx, contact + new Vector3(-r * puffSpread, 0f, 0f), Color.white, puffScale);
                            sandbox.Play(fx, contact + new Vector3(r * puffSpread, 0f, 0f), Color.white, puffScale);
                        }
                        StartCoroutine(Squint(visual));
                    }
                    v = speed * bounce;
                    bounces++;
                    if (v < 1.2f) break;
                }
                else if (!wobble.IsWobbling)
                {
                    // stretch along the fall while moving fast
                    float s = Mathf.Min(Mathf.Abs(v) * fallStretch, maxFallStretch);
                    visual.localScale = new Vector3(1f - s * 0.7f, 1f + s, 1f);
                }
                ball.position = new Vector3(0f, y, 0f);
                yield return null;
            }
            ball.position = new Vector3(0f, floorY + r, 0f);
            yield return new WaitForSeconds(holdAfter);
            Destroy(root.gameObject);
            running = false;
        }

        private IEnumerator Squint(Transform visual)
        {
            SandboxArt.SetFace(visual, squintFace);
            yield return new WaitForSeconds(squintTime);
            if (visual != null) SandboxArt.SetFace(visual, "face_happy_0");
        }
    }
}
