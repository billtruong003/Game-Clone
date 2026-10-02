using CasualGame.Core;
using UnityEngine;

namespace CasualGame.EyeMerge
{
    /// <summary>
    /// One ball. Reports touching same-tier balls to the game and wobbles like jelly on every hit. While it falls the
    /// face goes from shock to panic (no motion trail: seen through the jar glass it read as a dark smear).
    /// Hierarchy: Ball (physics, rotates) → Deform (world-aligned jelly squash) → Spin (follows the rotation, holds the face).
    /// </summary>
    public class MergeBall : MonoBehaviour
    {
        private const float ShockSpeed = 3.5f, PanicSpeed = 8f;

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
        // premium sets: the fill is a shader quad; it rolls (_Spin) and eyeballs look at the held ball (_Look)
        private MaterialPropertyBlock skinProps;
        private bool lookAt, squashy;
        private float squash; // slimes: 0..1, set by a landing, settles back

        /// <summary>Where every eyeball looks (the ball waiting to be dropped), set by the game.</summary>
        public static Vector2 LookTarget;

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
        }

        public void SetBodyVisible(bool visible)
        {
            fill.enabled = visible;
            line.enabled = visible && skinProps == null; // a shader ball draws its own outline
        }

        /// <summary>Draws this ball with a premium set's shader instead of the circle sprites.</summary>
        public void UseSkin(MergeLooks.Look look)
        {
            skinProps = new MaterialPropertyBlock();
            MergeLooks.Configure(look, Tier, skinProps.SetFloat, skinProps.SetColor, skinProps.SetTexture);
            fill.sprite = MergeLooks.UnitQuad;
            fill.drawMode = SpriteDrawMode.Simple;
            fill.color = Color.white;
            fill.sharedMaterial = MergeLooks.Shared(look);
            fill.transform.localScale = Vector3.one * (2f * radius / MergeLooks.SphereR(look, Tier));
            fill.SetPropertyBlock(skinProps);
            line.enabled = false;
            lookAt = look.Set == 5;
            squashy = look.Set == 4;
            if (!look.Face) Face.gameObject.SetActive(false);
        }

        public void FadeFace(float alpha) => Face.SetAlpha(alpha);

        private void LateUpdate()
        {
            Deform.rotation = Quaternion.identity;
            spin.rotation = transform.rotation;
            if (skinProps != null)
            {
                skinProps.SetFloat("_Spin", transform.eulerAngles.z * Mathf.Deg2Rad);
                if (lookAt) skinProps.SetVector("_Look", Vector2.ClampMagnitude((LookTarget - (Vector2)transform.position) / 6f, 1f));
                if (squashy)
                {
                    squash = Mathf.MoveTowards(squash, 0f, Time.deltaTime * 4f);
                    skinProps.SetFloat("_Squash", squash);
                }
                fill.SetPropertyBlock(skinProps);
            }
            if (!Body.simulated || Merging) return;

            var fall = -Body.linearVelocity.y;
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
                    if (squashy) squash = Mathf.Max(squash, Mathf.Clamp01(speed / 9f));
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
