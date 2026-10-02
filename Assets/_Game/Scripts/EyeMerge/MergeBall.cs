using System.Collections.Generic;
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

        /// <summary>The ball waiting to be dropped (interactive sets react to it), set by the game.</summary>
        public static MergeBall Held;

        /// <summary>Every ball in play (the compass needle looks for its twin).</summary>
        private static readonly List<MergeBall> all = new();

        // interactive sets (MergeLooks.LiveKind): per-ball state
        private MergeLooks.LiveKind live;
        private Color tierColor;
        private Vector2 liveLook;
        private float mouth, sulk, stride, sleep, tumbleT = 99f, needle = Mathf.PI / 2f, needleVel, shake, blinkOffset;
        private Vector3 lastPos;

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

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
            tierColor = color;
            blinkOffset = Random.value * 4f;
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
            live = look.Live;
            fill.color = live != MergeLooks.LiveKind.None ? tierColor : Color.white; // interactive sets tint body / shell / sky by tier
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
                if (live != MergeLooks.LiveKind.None) Live(skinProps);
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
                    if (speed > 6f) tumbleT = 0f;                       // hamster: a hard landing flips it over
                    shake = Mathf.Max(shake, Mathf.Clamp01(speed / 8f)); // snow globe: a hit whirls the snow
                }
                game.OnBallHit(this, c, speed);
            }
            Check(c);
        }

        private MergeBall NearestTwin()
        {
            MergeBall best = null;
            var bestD = float.MaxValue;
            foreach (var o in all)
            {
                if (o == this || o == Held || o.Tier != Tier || o.Merging || !o.fill.enabled) continue;
                var d = ((Vector2)(o.transform.position - transform.position)).sqrMagnitude;
                if (d < bestD) { bestD = d; best = o; }
            }
            return best;
        }

        // The interactive sets' live values (Docs/SHADER_LAB.md): what each ball reacts to this frame.
        private void Live(MaterialPropertyBlock p)
        {
            var dt = Time.deltaTime;
            Vector2 pos = transform.position;
            var isHeld = this == Held;
            switch (live)
            {
                case MergeLooks.LiveKind.Hungry:
                    // balls of the held ball's tier stare up at it with their mouth open; the rest look away and sulk
                    var twin = isHeld ? NearestTwin() : null;
                    var target = isHeld ? (twin != null ? (Vector2)twin.transform.position : pos + Vector2.down * 3f) : LookTarget;
                    var same = isHeld ? twin != null : Held != null && Held.Tier == Tier;
                    var to = target - pos;
                    var lookTo = Vector2.ClampMagnitude(to / 3f, 1f);
                    liveLook = Vector2.Lerp(liveLook, same ? lookTo : -lookTo.normalized * 0.7f, dt * 6f);
                    mouth = Mathf.Lerp(mouth, same ? Mathf.Lerp(0.35f, 1f, Mathf.InverseLerp(9f, 2f, to.magnitude)) : 0f, dt * 5f);
                    sulk = Mathf.Lerp(sulk, same ? 0f : 0.8f, dt * 4f);
                    p.SetVector("_Look", liveLook);
                    p.SetFloat("_Mouth", mouth);
                    p.SetFloat("_Sulk", sulk);
                    var b = Mathf.Repeat(Time.time + blinkOffset, 3.7f);
                    p.SetFloat("_Blink", b < 0.14f ? Mathf.Sin(b / 0.14f * Mathf.PI) : 0f);
                    break;
                case MergeLooks.LiveKind.Hamster:
                    // runs the way the ball rolls, naps when it rests, tumbles after a hard landing
                    var run = isHeld ? Mathf.Clamp((pos.x - lastPos.x) / Mathf.Max(dt, 1e-4f) / 8f, -1f, 1f) : Mathf.Clamp(-Body.angularVelocity / 360f, -1f, 1f);
                    stride += Mathf.Abs(run) * dt * 14f;
                    var resting = Mathf.Abs(run) < 0.15f;
                    sleep = Mathf.MoveTowards(sleep, resting ? 1f : 0f, dt * (resting ? 0.6f : 4f));
                    tumbleT += dt;
                    var tumble = tumbleT < 0.7f ? Mathf.SmoothStep(0f, Mathf.PI * 2f, tumbleT / 0.7f) : 0f;
                    if (tumbleT < 0.7f) sleep = 0f;
                    p.SetFloat("_Run", run);
                    p.SetFloat("_Stride", stride);
                    p.SetFloat("_Tumble", tumble);
                    p.SetFloat("_Sleep", sleep);
                    break;
                case MergeLooks.LiveKind.Compass:
                    // the needle points at the nearest ball of the same tier (the held ball counts): a drop hint
                    var t = NearestTwin();
                    var want = t != null ? Mathf.Atan2(t.transform.position.y - pos.y, t.transform.position.x - pos.x) : Time.time * 0.8f + blinkOffset;
                    if (!isHeld && Held != null && Held.Tier == Tier && (t == null || ((Vector2)Held.transform.position - pos).sqrMagnitude < ((Vector2)t.transform.position - pos).sqrMagnitude))
                        want = Mathf.Atan2(Held.transform.position.y - pos.y, Held.transform.position.x - pos.x);
                    var diff = Mathf.DeltaAngle(needle * Mathf.Rad2Deg, want * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                    needleVel += diff * 60f * dt - needleVel * 7f * dt;
                    needle += needleVel * dt;
                    p.SetFloat("_Needle", needle);
                    break;
                case MergeLooks.LiveKind.Snow:
                    if (isHeld) shake = Mathf.Max(shake, Mathf.Clamp01(Mathf.Abs(pos.x - lastPos.x) / Mathf.Max(dt, 1e-4f) / 20f));
                    shake *= Mathf.Exp(-dt * 0.6f);
                    p.SetFloat("_Shake", shake);
                    break;
            }
            lastPos = pos;
        }

        private void OnCollisionStay2D(Collision2D c) => Check(c);

        private void Check(Collision2D c)
        {
            if (!Merging && c.gameObject.TryGetComponent<MergeBall>(out var other) && other.Tier == Tier && !other.Merging)
                game.RequestMerge(this, other);
        }
    }
}
