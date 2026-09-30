using CasualGame.Core;
using UnityEngine;

namespace CasualGame.EyeMerge
{
    /// <summary>
    /// One ball. Reports touching same-tier balls to the game and wobbles like jelly on every hit. While it falls a
    /// thin fading trail shows the motion and the face goes from shock to panic.
    /// Hierarchy: Ball (physics, rotates, trail) → Deform (world-aligned jelly squash) → Spin (follows the rotation, holds the face).
    /// </summary>
    public class MergeBall : MonoBehaviour
    {
        private const float ShockSpeed = 3.5f, PanicSpeed = 8f, TrailSpeed = 3f;
        private static Material trailMaterial;

        public int Tier { get; private set; }
        public Face Face { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public JellyWobble Jelly { get; private set; }
        public Transform Deform { get; private set; }
        public float Born;
        public bool Merging;
        public bool Landed;
        private EyeMergeGame game;
        private float radius;
        private SpriteRenderer fill, line;
        private Transform spin;
        private TrailRenderer trail;

        public void Init(EyeMergeGame owner, int tier, Face face, Rigidbody2D body, JellyWobble jelly, float r,
            SpriteRenderer fillRenderer, SpriteRenderer lineRenderer, Transform spinRoot, Color color)
        {
            game = owner;
            Tier = tier;
            Face = face;
            Body = body;
            Jelly = jelly;
            Deform = jelly.transform;
            radius = r;
            fill = fillRenderer;
            line = lineRenderer;
            spin = spinRoot;
            trail = CreateTrail(color);
        }

        // thin tapered streak behind a moving ball, lighter than the ball, gone in 0.2 s
        private TrailRenderer CreateTrail(Color color)
        {
            if (trailMaterial == null) trailMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            var t = gameObject.AddComponent<TrailRenderer>();
            t.sharedMaterial = trailMaterial;
            t.time = 0.2f;
            t.minVertexDistance = 0.05f;
            t.widthMultiplier = radius * 0.45f;
            t.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            var light = Color.Lerp(color, Color.white, 0.6f);
            t.colorGradient = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(light, 0f), new GradientColorKey(light, 1f) },
                alphaKeys = new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) },
            };
            t.numCapVertices = 2;
            t.sortingOrder = 9;
            t.emitting = false;
            return t;
        }

        public void SetBodyVisible(bool visible)
        {
            fill.enabled = line.enabled = visible;
            if (!visible) { trail.emitting = false; trail.Clear(); }
        }

        public void FadeFace(float alpha) => Face.SetAlpha(alpha);

        private void LateUpdate()
        {
            Deform.rotation = Quaternion.identity;
            spin.rotation = transform.rotation;
            if (!Body.simulated || Merging) { trail.emitting = false; return; }

            var fall = -Body.linearVelocity.y;
            trail.emitting = Body.linearVelocity.sqrMagnitude > TrailSpeed * TrailSpeed;
            if (fall > PanicSpeed) Face.Keep(FaceId.Panic);
            else if (fall > ShockSpeed) Face.Keep(FaceId.Shock);
        }

        private void OnCollisionEnter2D(Collision2D c)
        {
            if (Body.simulated)
            {
                float speed = c.relativeVelocity.magnitude;
                if (speed > 1f)
                {
                    var normal = ((Vector2)transform.position - c.GetContact(0).point).normalized; // from the contact into this ball
                    Jelly.Impact(speed / 9f, normal, radius); // bouncy: a harder hit squashes more
                }
                game.OnBallHit(this, c, speed);
            }
            Check(c);
        }

        private void OnCollisionStay2D(Collision2D c) => Check(c);

        private void Check(Collision2D c)
        {
            if (!Merging && c.gameObject.TryGetComponent<MergeBall>(out var other) && other.Tier == Tier && !other.Merging)
                game.RequestMerge(this, other);
        }
    }
}
