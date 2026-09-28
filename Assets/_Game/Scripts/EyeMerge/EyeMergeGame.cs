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

        private void Start()
        {
            cam = Camera.main;
            cam.backgroundColor = Background;
            // Keep the 10.8-unit-wide jar area visible on tall and wide screens alike.
            cam.orthographicSize = Mathf.Max(9.6f, 5.4f / cam.aspect);
            canvas = UIKit.CreateCameraCanvas("EyeMergeUI", cam, 100); // above world sprites (0–30), below Fx (500)
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
            Sprite(world, "jar_line", center, jarSize, Color.white, 30, SpriteDrawMode.Sliced);
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
            var fill = Sprite(go.transform, "circle_fill", pos, new Vector2(2.56f, 2.56f), TierColors[tier - 1], 10, SpriteDrawMode.Simple);
            fill.transform.localScale = Vector3.one * visualScale;
            var line = Sprite(go.transform, "circle_line", pos, new Vector2(2.56f, 2.56f), Color.white, 11, SpriteDrawMode.Simple);
            line.transform.localScale = Vector3.one * visualScale;
            var faceGo = new GameObject("Face", typeof(SpriteRenderer));
            faceGo.transform.SetParent(go.transform, false);
            faceGo.transform.localScale = Vector3.one * visualScale;
            var faceSr = faceGo.GetComponent<SpriteRenderer>();
            faceSr.sortingOrder = 12;
            var face = faceGo.AddComponent<EmojiFace>();
            face.SetRandom();
            if (tier == Tiers) face.SetEmoji(ArtLibrary.Instance.GetEmoji("cool"));

            var ball = go.AddComponent<MergeBall>();
            ball.Init(this, tier, face, body);
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
                var tier = a.Tier;
                var mid = (a.transform.position + b.transform.position) / 2f;
                balls.Remove(a);
                balls.Remove(b);
                Destroy(a.gameObject);
                Destroy(b.gameObject);
                var color = TierColors[tier - 1];
                if (tier == Tiers)
                {
                    AddScore(100);
                    GameAudio.Play("big");
                    Fx.Burst(FxKind.Confetti, mid, TierColors[Random.Range(0, Tiers)], 40);
                    Fx.Burst(FxKind.Ring, mid, Color.white, 1, 3f);
                    continue;
                }
                var next = tier + 1;
                var ball = CreateBall(next, mid, true);
                ball.Born = Time.time;
                balls.Add(ball);
                var target = ball.transform.localScale;
                ball.transform.localScale = target * 0.6f;
                Tween.Scale(ball.transform, target, 0.2f, Ease.OutBack);
                ball.Face.React("laugh", 1.2f);
                AddScore(next * (next + 1) / 2);
                GameAudio.Play("merge", 0.8f + next * 0.07f);
                Fx.Burst(FxKind.Pop, mid, color, 10 + next);
                Fx.Burst(FxKind.Ring, mid, TierColors[next - 1], 1, Radius(next) * 1.4f);
                if (next >= 8) { Fx.Burst(FxKind.Sparkle, mid, Color.white, 16); GameAudio.Haptic(); }
            }
            merges.Clear();
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
                    Fx.Burst(FxKind.Pop, b.transform.position, TierColors[b.Tier - 1], 8);
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
                ("Chơi lại", "btn_green", "icon_restart", NewGame),
                ("Về menu", "btn_white", "icon_home", () => SceneFlow.Load(Application.CanStreamedLevelBeLoaded("Hub") ? "Hub" : "EyeMerge")));
        }
    }

    /// <summary>One ball: reports touching same-tier balls to the game.</summary>
    public class MergeBall : MonoBehaviour
    {
        public int Tier { get; private set; }
        public EmojiFace Face { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public float Born;
        public bool Merging;
        private EyeMergeGame game;

        public void Init(EyeMergeGame owner, int tier, EmojiFace face, Rigidbody2D body)
        {
            game = owner;
            Tier = tier;
            Face = face;
            Body = body;
        }

        private void OnCollisionEnter2D(Collision2D c) => Check(c);
        private void OnCollisionStay2D(Collision2D c) => Check(c);

        private void Check(Collision2D c)
        {
            if (!Merging && c.gameObject.TryGetComponent<MergeBall>(out var other) && other.Tier == Tier && !other.Merging)
                game.RequestMerge(this, other);
        }
    }
}
