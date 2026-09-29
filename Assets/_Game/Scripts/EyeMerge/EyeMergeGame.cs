using System.Collections;
using System.Collections.Generic;
using CasualGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CasualGame.EyeMerge
{
    /// <summary>
    /// Eye Merge (Suika-style): drop emoji balls into a jar, equal tiers merge into the next tier.
    /// World-space physics (1 unit = 100 px); HUD on a camera canvas. Pure gameplay screen.
    /// </summary>
    public class EyeMergeGame : MonoBehaviour
    {
        public const int Tiers = 11;
        private static readonly Color[] TierColors =
        {
            UIKit.Hex("#FFD23F"), UIKit.Hex("#FF9F1C"), UIKit.Hex("#3DDC97"), UIKit.Hex("#4EA8DE"), UIKit.Hex("#F15BB5"),
            UIKit.Hex("#FF5A5F"), UIKit.Hex("#9B5DE5"), UIKit.Hex("#2A9D8F"), UIKit.Hex("#E76F51"), UIKit.Hex("#3A5BD9"), UIKit.Hex("#FFC83D"),
        };
        private static readonly int[] SpawnWeights = { 5, 4, 3, 2, 1 };
        private static readonly Color Background = UIKit.Hex("#2B2F55");

        // Jar interior in world units.
        private const float JarLeft = -4.5f, JarRight = 4.5f, JarBottom = -6.9f, JarTop = 4.4f;
        private const float DangerY = 3.3f;
        private const float HoldY = 5.5f;
        private const float DropCooldown = 0.5f;
        private const float LoseAfter = 2f;

        public static float Radius(int tier) => 0.38f * Mathf.Pow(1.25f, tier - 1);

        private Camera cam;
        private Canvas canvas;
        private RectTransform hud;
        private Transform world;
        private SpriteRenderer dangerLine;
        private MergeBall held;
        private int nextTier;
        private Image nextFill;
        private Image[] strip;
        private TextMeshProUGUI scoreText, bestText;
        private readonly List<MergeBall> balls = new();
        private readonly List<(MergeBall a, MergeBall b)> merges = new();
        private int score, best;
        private float lastDrop = -10f, overTimer;
        private bool playing, revived, aiming;
        private PhysicsMaterial2D material;
        private GlassJar glass;

        // Merge feel (goo melt -> white flash -> new ball pops out of the blob)
        private const float MeltTime = 0.14f, FlashTime = 0.035f, PopTime = 0.1f, SettleTime = 0.18f;
        private const float GooBlend = 1.1f, JarCorner = 0.9f;

        private void Start()
        {
            cam = Camera.main;
            cam.backgroundColor = Background;
            // Keep the 10.8-unit-wide jar area visible on tall and wide screens alike.
            cam.orthographicSize = Mathf.Max(9.6f, 5.4f / cam.aspect);
            canvas = UIKit.CreateCameraCanvas("EyeMergeUI", cam, 100);
            canvas.sortingLayerName = "Glass"; // HUD and popups above the jar glass (which is on the Glass layer)
            hud = UIKit.Stretch(UIKit.Rect("Safe", canvas.transform));
            hud.gameObject.AddComponent<SafeArea>();
            material = new PhysicsMaterial2D("Ball") { bounciness = 0.12f, friction = 0.35f };
            NewGame();
            GameAudio.PlayMusic("music_merge");
        }

        private void NewGame()
        {
            if (world != null) Destroy(world.gameObject);
            foreach (Transform child in hud) Destroy(child.gameObject);
            balls.Clear();
            merges.Clear();
            score = 0;
            overTimer = 0f;
            revived = false;
            best = SaveStore.GetInt("merge.best");
            world = new GameObject("World").transform;
            nextTier = RollTier(); // the HUD's "next" preview reads it
            BuildJar();
            BuildHud();
            SpawnHeld();
            playing = true;
        }

        private void BuildJar()
        {
            var jarSize = new Vector2(JarRight - JarLeft + 0.36f, JarTop - JarBottom + 0.36f);
            var center = new Vector3(0f, (JarTop + JarBottom) / 2f, 0f);
            Sprite(world, "jar_back", center, jarSize, UIKit.Hex("#3A3F72"), 0, SpriteDrawMode.Sliced);
            Sprite(world, "jar_line", center, jarSize, Color.white, 1, SpriteDrawMode.Sliced).sortingLayerName = "Glass"; // outline over the glass
            // front glass: rim, reflection streak, refraction near the walls, glint when a ball hits the glass
            glass = GlassJar.Create(world, new Rect(JarLeft, JarBottom, JarRight - JarLeft, JarTop - JarBottom), JarCorner, "Glass", 0);
            dangerLine = Sprite(world, "danger_dash", new Vector3(0, DangerY, 0), new Vector2(JarRight - JarLeft - 0.2f, 0.16f), UIKit.Hex("#FF5A5F"), 25, SpriteDrawMode.Tiled);

            void Wall(Vector2 pos, Vector2 size)
            {
                var go = new GameObject("Wall", typeof(BoxCollider2D));
                go.transform.SetParent(world, false);
                go.transform.position = pos;
                go.GetComponent<BoxCollider2D>().size = size;
            }
            Wall(new Vector2(JarLeft - 0.5f, 0), new Vector2(1f, 30f));
            Wall(new Vector2(JarRight + 0.5f, 0), new Vector2(1f, 30f));
            Wall(new Vector2(0, JarBottom - 0.5f), new Vector2(20f, 1f));
        }

        private static SpriteRenderer Sprite(Transform parent, string name, Vector3 pos, Vector2 size, Color color, int order, SpriteDrawMode mode)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = ArtLibrary.Instance.Get(name);
            sr.drawMode = mode;
            sr.size = size;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        private void BuildHud()
        {
            var top = new Vector2(0.5f, 1f);
            scoreText = UIKit.Label(hud, "0", 110, top, new Vector2(0, -110), new Vector2(600, 140), UIKit.Paper);
            bestText = UIKit.Label(hud, "", 44, top, new Vector2(0, -200), new Vector2(600, 60), UIKit.Hex("#AEB3D9"));
            UIKit.IconButton(hud, "round_white", "icon_pause", OpenPause, new Vector2(0f, 1f), new Vector2(100, -100), 116);

            UIKit.Label(hud, "Tiếp", 40, new Vector2(1f, 1f), new Vector2(-110, -60), new Vector2(200, 60), UIKit.Hex("#AEB3D9"));
            var bubble = UIKit.Image(hud, "round_white", new Vector2(1f, 1f), new Vector2(-110, -150), new Vector2(130, 130));
            nextFill = UIKit.Image(bubble.transform, "circle_fill", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 80));
            UIKit.Image(nextFill.transform, "circle_line", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 80));

            strip = new Image[Tiers];
            for (int t = 1; t <= Tiers; t++)
            {
                var img = UIKit.Image(hud, "circle_fill", new Vector2(0.5f, 0f), new Vector2((t - 6) * 88, 70), new Vector2(72, 72), TierColors[t - 1]);
                UIKit.Image(img.transform, "circle_line", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72, 72));
                img.color = new Color(img.color.r, img.color.g, img.color.b, 0.25f);
                strip[t - 1] = img;
            }
            RefreshHud();
        }

        private void RefreshHud()
        {
            scoreText.text = score.ToString();
            bestText.text = $"Kỷ lục {Mathf.Max(best, score)}";
            nextFill.color = TierColors[nextTier - 1];
            var s = Mathf.Lerp(0.6f, 1f, (nextTier - 1) / 4f);
            nextFill.transform.localScale = Vector3.one * s;
        }

        private int RollTier()
        {
            int total = 0;
            foreach (var w in SpawnWeights) total += w;
            var x = Random.Range(0, total);
            for (int i = 0; i < SpawnWeights.Length; i++)
                if ((x -= SpawnWeights[i]) < 0) return i + 1;
            return 1;
        }

        // ---------------- balls ----------------

        private MergeBall CreateBall(int tier, Vector2 pos, bool simulated)
        {
            var r = Radius(tier);
            var go = new GameObject($"Ball{tier}", typeof(Rigidbody2D), typeof(CircleCollider2D));
            go.transform.SetParent(world, false);
            go.transform.position = pos;
            var body = go.GetComponent<Rigidbody2D>();
            body.gravityScale = 1.4f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.simulated = simulated;
            body.freezeRotation = true; // faces stay upright
            var col = go.GetComponent<CircleCollider2D>();
            col.radius = r;
            col.sharedMaterial = material;

            // 256 px body sprite at 100 PPU is 2.56 units across; the drawn circle is 244 px.
            var visualScale = 2f * r / 2.44f;
            // everything drawn lives under "Visual" so the jelly wobble can squash it without touching the physics body
            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            var fill = Sprite(visual, "circle_fill", pos, new Vector2(2.56f, 2.56f), TierColors[tier - 1], 10, SpriteDrawMode.Simple);
            fill.transform.localScale = Vector3.one * visualScale;
            var line = Sprite(visual, "circle_line", pos, new Vector2(2.56f, 2.56f), Color.white, 11, SpriteDrawMode.Simple);
            line.transform.localScale = Vector3.one * visualScale;
            var faceGo = new GameObject("Face", typeof(SpriteRenderer));
            faceGo.transform.SetParent(visual, false);
            faceGo.transform.localScale = Vector3.one * visualScale;
            var faceSr = faceGo.GetComponent<SpriteRenderer>();
            faceSr.sortingOrder = 12;
            var face = faceGo.AddComponent<EmojiFace>();
            face.SetRandom();
            if (tier == Tiers) face.SetEmoji(ArtLibrary.Instance.GetEmoji("cool"));

            var ball = go.AddComponent<MergeBall>();
            ball.Init(this, tier, face, body, visual.gameObject.AddComponent<JellyWobble>(), r, fill, line);
            if (simulated) LightStrip(tier);
            return ball;
        }

        private void SpawnHeld()
        {
            held = CreateBall(nextTier, new Vector2(held != null ? held.transform.position.x : 0f, HoldY), false);
            nextTier = RollTier();
            RefreshHud();
        }

        internal void RequestMerge(MergeBall a, MergeBall b)
        {
            if (a.Merging || b.Merging || a.Tier != b.Tier || !playing) return;
            a.Merging = b.Merging = true;
            merges.Add((a, b));
        }

        private void FixedUpdate()
        {
            if (merges.Count == 0) return;
            foreach (var (a, b) in merges)
            {
                if (a == null || b == null) continue;
                balls.Remove(a);
                balls.Remove(b);
                StartCoroutine(Merge(a, b));
            }
            merges.Clear();
        }

        // Two same-tier balls melt into one goo blob (GooMerge), flash white, and the next tier pops out of it.
        private IEnumerator Merge(MergeBall a, MergeBall b)
        {
            var tier = a.Tier;
            var color = TierColors[tier - 1];
            float r = Radius(tier);
            a.Body.simulated = b.Body.simulated = false;
            a.SetBodyVisible(false);
            b.SetBodyVisible(false);
            a.Face.React("surprised", 1f);
            b.Face.React("surprised", 1f);
            var goo = GooMerge.Create(world, 10);
            Vector3 pa = a.transform.position, pb = b.transform.position, mid = (pa + pb) / 2f;
            float grow = tier < Tiers ? Radius(tier + 1) / r : 1.25f;
            for (float t = 0f; t < MeltTime; t += Time.deltaTime)
            {
                if (a == null || b == null) { Destroy(goo.gameObject); yield break; } // game reset mid-merge
                var u = t / MeltTime;
                var m = Tween.Evaluate(Ease.InOutSine, u);
                a.transform.position = Vector3.Lerp(pa, mid, m);
                b.transform.position = Vector3.Lerp(pb, mid, m);
                var rr = r * Mathf.Lerp(1f, grow, m);
                goo.Set(a.transform.position, rr, color, b.transform.position, rr, color, 0.02f + r * GooBlend * Tween.Evaluate(Ease.OutQuad, Mathf.Min(1f, u * 2.5f)));
                a.FadeFace(1f - Mathf.SmoothStep(0.05f, 0.35f, u));
                b.FadeFace(1f - Mathf.SmoothStep(0.05f, 0.35f, u));
                yield return null;
            }
            goo.Set(mid, r * grow, color, mid, r * grow, color, r * GooBlend, 1f); // white flash
            Destroy(a.gameObject);
            Destroy(b.gameObject);
            yield return new WaitForSeconds(FlashTime);
            Destroy(goo.gameObject);

            if (tier == Tiers)
            {
                AddScore(100);
                GameAudio.Play("big");
                GameFx.Play("Merge_BigFusion", mid, 1.6f);
                GameFx.Play("Win_Confetti", mid, 1f);
                yield break;
            }
            var next = tier + 1;
            var ball = CreateBall(next, mid, true);
            ball.Born = Time.time;
            ball.Landed = true;
            balls.Add(ball);
            ball.Face.React("laugh", 1.2f);
            AddScore(next * (next + 1) / 2);
            GameAudio.Play("merge", 0.8f + next * 0.07f);
            GameFx.Play(GameFx.Colored("Merge_Fusion_{color}", TierColors[next - 1]), mid, Radius(next) * 1.6f);
            if (next >= 9) { GameFx.Play("Merge_BigFusion", mid, Radius(next)); GameAudio.Haptic(); }
            // pop: from the blob's size -> overshoot -> settle
            var target = ball.transform.localScale;
            Tween.Run(ball, PopTime + SettleTime, k =>
            {
                var t = k * (PopTime + SettleTime);
                var s = t < PopTime ? Mathf.Lerp(1f, 1.15f, Tween.Evaluate(Ease.OutQuad, t / PopTime))
                                    : Mathf.Lerp(1.15f, 1f, Tween.Evaluate(Ease.OutElastic, (t - PopTime) / SettleTime));
                ball.transform.localScale = target * s;
            }, Ease.Linear);
        }

        /// <summary>A ball hit something: the glass glints when it is a wall; the first landing after a drop puffs smoke.</summary>
        internal void OnBallHit(MergeBall ball, Collision2D c, float speed)
        {
            if (c.collider.name == "Wall" && speed > 2.5f && glass != null) glass.Glint(speed / 10f);
            if (ball.Landed) return;
            ball.Landed = true;
            if (speed <= 3f) return;
            var contact = c.GetContact(0).point;
            var r = Radius(ball.Tier);
            GameFx.Play("Land_Poof", contact + Vector2.left * r * 0.6f, r * 0.45f);
            GameFx.Play("Land_Poof", contact + Vector2.right * r * 0.6f, r * 0.45f);
        }

        private void LightStrip(int tier)
        {
            var dot = strip[tier - 1];
            if (dot.color.a >= 1f) return;
            dot.color = TierColors[tier - 1];
            Tween.Punch(dot.transform, 0.5f, 0.4f);
        }

        private void AddScore(int n)
        {
            score += n;
            RefreshHud();
            Tween.Punch(scoreText.transform, 0.12f, 0.2f);
        }

        // ---------------- input & rules ----------------

        private void Update()
        {
            if (!playing) return;
            var pointer = Pointer.current;
            if (pointer != null && held != null)
            {
                if (pointer.press.wasPressedThisFrame)
                    aiming = EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject();
                if (aiming && pointer.press.isPressed) Aim(pointer.position.ReadValue());
                if (aiming && pointer.press.wasReleasedThisFrame)
                {
                    aiming = false;
                    Drop();
                }
            }

            bool danger = false;
            foreach (var b in balls)
            {
                if (Time.time - b.Born < 1f) continue;
                if (b.transform.position.y + Radius(b.Tier) > DangerY) { danger = true; break; }
            }
            if (danger && overTimer == 0f)
                foreach (var b in balls) b.Face.React("surprised", LoseAfter);
            overTimer = danger ? overTimer + Time.deltaTime : 0f;
            var pulse = danger ? 0.55f + 0.45f * Mathf.Sin(Time.time * 18f) : 0.45f;
            dangerLine.color = new Color(dangerLine.color.r, dangerLine.color.g, dangerLine.color.b, pulse);
            if (overTimer > LoseAfter) GameOver();
        }

        private void Aim(Vector2 screen)
        {
            var world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
            var r = Radius(held.Tier);
            var x = Mathf.Clamp(world.x, JarLeft + r, JarRight - r);
            held.transform.position = new Vector3(x, HoldY, 0f);
        }

        private void Drop()
        {
            if (held == null || Time.time - lastDrop < DropCooldown) return;
            lastDrop = Time.time;
            held.Body.simulated = true;
            held.Born = Time.time;
            LightStrip(held.Tier);
            balls.Add(held);
            GameAudio.Play("drop");
            held = null;
            Tween.Delay(this, DropCooldown, () => { if (playing) SpawnHeld(); });
        }

        private void GameOver()
        {
            playing = false;
            GameAudio.Play("lose");
            foreach (var b in balls) { b.Body.simulated = false; b.Face.React("dizzy", -1f); }
            if (held != null) held.gameObject.SetActive(false);
            best = SaveStore.SubmitBest("merge.best", score);
            Tween.Delay(this, 0.6f, () =>
            {
                Popup p = null;
                p = Popup.Open(hud, "Hũ đầy rồi!", 1050);
                p.Text(score.ToString(), 140, UIKit.Ink, 160);
                p.Text(score >= best && score > 0 ? "Kỷ lục mới!" : $"Kỷ lục {best}", 50, UIKit.Muted);
                p.Space(10);
                if (!revived)
                    p.Button("btn_blue", "Hồi sinh", () => p.Close(() => Ads.ShowRewarded("merge_revive", ok => { if (ok) Revive(); })), "icon_ad");
                p.Button("btn_green", "Chơi lại", () => p.Close(() => Ads.OnBreak("merge_gameover", NewGame)), "icon_restart");
                p.Fit();
            });
        }

        // Removes every ball in the top part of the jar and resumes.
        private void Revive()
        {
            revived = true;
            for (int i = balls.Count - 1; i >= 0; i--)
            {
                var b = balls[i];
                if (b.transform.position.y + Radius(b.Tier) > DangerY - 3.2f)
                {
                    GameFx.Play(GameFx.Colored("Merge_Fusion_{color}", TierColors[b.Tier - 1]), b.transform.position, Radius(b.Tier));
                    balls.RemoveAt(i);
                    Destroy(b.gameObject);
                }
            }
            foreach (var b in balls) { b.Body.simulated = true; b.Face.ClearReaction(); }
            if (held != null) held.gameObject.SetActive(true);
            overTimer = 0f;
            playing = true;
        }

        private void OpenPause()
        {
            if (!playing) return;
            playing = false;
            foreach (var b in balls) b.Body.simulated = false;
            SettingsPopup.Show(hud, () =>
                {
                    foreach (var b in balls) b.Body.simulated = true;
                    playing = true;
                },
                ("Chơi lại", "btn_green", "icon_restart", NewGame));
        }
    }

    /// <summary>One ball: reports touching same-tier balls to the game; wobbles like jelly on every hit.</summary>
    public class MergeBall : MonoBehaviour
    {
        public int Tier { get; private set; }
        public EmojiFace Face { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public float Born;
        public bool Merging;
        public bool Landed;
        private EyeMergeGame game;
        private JellyWobble wobble;
        private float radius;
        private SpriteRenderer fill, line, faceRenderer;

        public void Init(EyeMergeGame owner, int tier, EmojiFace face, Rigidbody2D body, JellyWobble jelly, float r, SpriteRenderer fillRenderer, SpriteRenderer lineRenderer)
        {
            game = owner;
            Tier = tier;
            Face = face;
            Body = body;
            wobble = jelly;
            radius = r;
            fill = fillRenderer;
            line = lineRenderer;
            faceRenderer = face.GetComponent<SpriteRenderer>();
        }

        public void SetBodyVisible(bool visible) => fill.enabled = line.enabled = visible;

        public void FadeFace(float alpha) => faceRenderer.color = new Color(1f, 1f, 1f, alpha);

        private void OnCollisionEnter2D(Collision2D c)
        {
            if (Body.simulated)
            {
                float speed = c.relativeVelocity.magnitude;
                if (speed > 1f)
                {
                    var normal = ((Vector2)transform.position - c.GetContact(0).point).normalized; // from the contact into this ball
                    wobble.Impact(speed / 12f, normal, radius);
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
