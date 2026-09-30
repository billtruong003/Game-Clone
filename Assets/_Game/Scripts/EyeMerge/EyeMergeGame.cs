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
    /// Eye Merge (Suika-style): drop balls into a jar, equal tiers merge into the next tier.
    /// World-space physics (1 unit = 100 px); HUD on a camera canvas. Pure gameplay screen.
    /// </summary>
    public class EyeMergeGame : MonoBehaviour
    {
        public const int Tiers = 11;
        internal static readonly Color[] TierColors =
        {
            UIKit.Hex("#FFD23F"), UIKit.Hex("#FF9F1C"), UIKit.Hex("#3DDC97"), UIKit.Hex("#4EA8DE"), UIKit.Hex("#F15BB5"),
            UIKit.Hex("#FF5A5F"), UIKit.Hex("#9B5DE5"), UIKit.Hex("#2A9D8F"), UIKit.Hex("#E76F51"), UIKit.Hex("#3A5BD9"), UIKit.Hex("#FFC83D"),
        };
        private static readonly int[] SpawnWeights = { 5, 4, 3, 2, 1 };
        private static readonly Color Background = UIKit.Hex("#2F2552");
        private static readonly Color DangerRed = UIKit.Hex("#FF5A5F");

        // Jar interior in world units.
        internal const float JarLeft = -4.5f, JarRight = 4.5f, JarBottom = -6.9f, JarTop = 4.4f;
        private const float DangerY = 3.3f;
        private const float HoldY = 5.5f;
        private const float DropCooldown = 0.5f;
        private const float LoseAfter = 2f;
        private const float ComboWindow = 1f;
        private const int AimDots = 40;
        private const float AimSpacing = 0.34f;

        public static float Radius(int tier) => 0.38f * Mathf.Pow(1.25f, tier - 1);

        private Camera cam;
        private Canvas canvas;
        private RectTransform hud;
        private Transform world;
        private SpriteRenderer dangerLine, jarLine, dangerBar, dangerBarBg, ghostFill, ghostLine;
        private readonly List<SpriteRenderer> aimDots = new();
        private MergeBall held;
        private int nextTier;
        private Image nextFill;
        private Face nextFace;
        private Image[] strip;
        private TextMeshProUGUI scoreText, bestText;
        private readonly List<MergeBall> balls = new();
        private readonly List<(MergeBall a, MergeBall b)> merges = new();
        private int score, shownScore, best, startBest, combo;
        private float lastDrop = -10f, overTimer, lastMerge = -10f, nextBestSave;
        private bool playing, revived, aiming, dangerShown, bestToastShown, legendShown;
        private PhysicsMaterial2D material;
        private GlassJar glass;

        // Merge feel (goo melt -> white flash -> new ball pops out of the blob)
        private const float MeltTime = 0.14f, FlashTime = 0.035f, PopTime = 0.1f, SettleTime = 0.18f;
        private const float GooBlend = 1.1f, JarCorner = 0.7f;

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
            // Balls must slide off each other and spread, never stack in a column: low friction, a little bounce.
            material = new PhysicsMaterial2D("Ball") { bounciness = 0.1f, friction = 0.05f };
            AppEvents.Back += OnBack;
            AppEvents.Suspended += OnSuspended;
            if (!TryRestore()) NewGame();
            GameAudio.PlayMusic("music_merge");
        }

        private void OnDestroy()
        {
            AppEvents.Back -= OnBack;
            AppEvents.Suspended -= OnSuspended;
        }

        // G5 / EM10: going to the background saves the run and pauses; a killed app comes back to the same jar, paused.
        private void OnSuspended()
        {
            SaveRun();
            OpenPause();
        }

        // ---------------- run in progress (M15) ----------------

        private const string RunKey = "merge.run";

        [System.Serializable]
        private class RunData
        {
            public int score, startBest, nextTier, heldTier;
            public float heldX;
            public bool revived, legend;
            public List<BallData> balls = new();
        }

        [System.Serializable]
        private class BallData { public int t; public float x, y, rot; }

        private void SaveRun()
        {
            if (ended) return; // game over: nothing to resume
            var d = new RunData { score = score, startBest = startBest, nextTier = nextTier, heldTier = held != null ? held.Tier : nextTier, heldX = lastHeldX, revived = revived, legend = legendShown };
            foreach (var b in balls)
                if (b != null) d.balls.Add(new BallData { t = b.Tier, x = b.transform.position.x, y = b.transform.position.y, rot = b.transform.eulerAngles.z });
            SaveStore.SetJson(RunKey, d);
            SaveStore.Save();
        }

        private bool TryRestore()
        {
            if (!SaveStore.Has(RunKey)) return false;
            var d = SaveStore.GetJson<RunData>(RunKey);
            if (d.balls == null || d.heldTier < 1) { SaveStore.Delete(RunKey); return false; }
            NewGame(false);
            score = shownScore = d.score;
            startBest = d.startBest;
            best = Mathf.Max(best, score);
            revived = d.revived;
            legendShown = d.legend;
            foreach (var bd in d.balls)
            {
                if (bd.t < 1 || bd.t > Tiers) continue;
                var b = CreateBall(bd.t, new Vector2(bd.x, bd.y), true);
                b.transform.rotation = Quaternion.Euler(0, 0, bd.rot);
                b.Born = Time.time - 5f;
                b.Landed = true;
                balls.Add(b);
            }
            lastHeldX = d.heldX;
            nextTier = d.heldTier;
            SpawnHeld();
            nextTier = Mathf.Clamp(d.nextTier, 1, SpawnWeights.Length);
            RefreshHud();
            OpenPause();
            return true;
        }

        private void OnBack()
        {
            if (playing) OpenPause();
        }

        private void NewGame() => NewGame(true);

        private void NewGame(bool fresh)
        {
            if (fresh) SaveStore.Delete(RunKey);
            ended = false;
            Tween.Kill(this);
            StopAllCoroutines();
            if (world != null) Destroy(world.gameObject);
            foreach (Transform child in hud) Destroy(child.gameObject);
            balls.Clear();
            merges.Clear();
            aimDots.Clear();
            held = null;
            score = shownScore = combo = 0;
            overTimer = 0f;
            revived = dangerShown = bestToastShown = legendShown = false;
            best = startBest = SaveStore.GetInt("merge.best");
            world = new GameObject("World").transform;
            nextTier = RollTier(); // the HUD's "next" preview reads it
            BuildJar();
            BuildHud();
            playing = true;
            if (!fresh) return;
            SpawnHeld();
            if (!SaveStore.GetBool("merge.tutorial", false)) ShowTutorial();
        }

        // ---------------- first launch (M14) ----------------

        private RectTransform tutorialRoot;

        // A hand sliding left-right under the held ball and a bubble over the jar, until the first drop.
        private void ShowTutorial()
        {
            tutorialRoot = UIKit.Stretch(UIKit.Rect("Tutorial", hud));
            var bubble = UIKit.Image(tutorialRoot, "panel", new Vector2(0.5f, 0.5f), new Vector2(0, 300), new Vector2(660, 190));
            var t = UIKit.Label(bubble.transform, Loc.T("Drag to aim,\nrelease to drop", "Kéo để ngắm,\nthả để rơi"), 56);
            UIKit.Stretch(t.rectTransform, 20);
            var hand = (RectTransform)UIKit.Image(tutorialRoot, "hand", new Vector2(0.5f, 1f), new Vector2(60, -640), new Vector2(170, 170)).transform;
            var home = hand.anchoredPosition;
            Tween.Run(hand, 60f, k => hand.anchoredPosition = home + new Vector2(Mathf.Sin(k * 60f * 2.2f) * 180f, 0f), Ease.Linear);
        }

        private void HideTutorial()
        {
            if (tutorialRoot == null) return;
            Destroy(tutorialRoot.gameObject);
            tutorialRoot = null;
            SaveStore.SetBool("merge.tutorial", true);
            SaveStore.Save();
        }

        private void BuildJar()
        {
            var jarSize = new Vector2(JarRight - JarLeft + 0.36f, JarTop - JarBottom + 0.36f);
            var center = new Vector3(0f, (JarTop + JarBottom) / 2f, 0f);
            Sprite(world, "jar_back", center, jarSize, UIKit.Hex("#3F3470"), 0, SpriteDrawMode.Sliced);
            jarLine = Sprite(world, "jar_line", center, jarSize, Color.white, 1, SpriteDrawMode.Sliced);
            jarLine.sortingLayerName = "Glass"; // outline over the glass
            glass = GlassJar.Create(world, new Rect(JarLeft, JarBottom, JarRight - JarLeft, JarTop - JarBottom), JarCorner, "Glass", 0);
            dangerLine = Sprite(world, "danger_dash", new Vector3(0, DangerY, 0), new Vector2(JarRight - JarLeft - 0.2f, 0.16f), DangerRed, 25, SpriteDrawMode.Tiled);

            // M8: 2 s countdown bar just above the danger line, visible only while a ball is over it
            dangerBarBg = Sprite(world, "round_rect", new Vector3(0, DangerY + 0.32f, 0), new Vector2(4.8f, 0.26f), new Color(0f, 0f, 0f, 0.35f), 26, SpriteDrawMode.Sliced);
            dangerBar = Sprite(world, "round_rect", new Vector3(-2.4f, DangerY + 0.32f, 0), new Vector2(0.01f, 0.2f), DangerRed, 27, SpriteDrawMode.Sliced);
            dangerBarBg.enabled = dangerBar.enabled = false;

            // M1: aim line (dots) from the held ball down to where it will first touch, and a ghost circle there
            for (int i = 0; i < AimDots; i++)
            {
                var d = Sprite(world, "dot", Vector3.zero, new Vector2(0.12f, 0.12f), new Color(1f, 1f, 1f, 0.4f), 5, SpriteDrawMode.Simple);
                d.transform.localScale = Vector3.one * (0.12f / Mathf.Max(0.01f, d.sprite != null ? d.sprite.bounds.size.x : 1f));
                d.enabled = false;
                aimDots.Add(d);
            }
            ghostFill = Sprite(world, "circle_fill", Vector3.zero, new Vector2(2.56f, 2.56f), new Color(1f, 1f, 1f, 0.1f), 4, SpriteDrawMode.Simple);
            ghostLine = Sprite(world, "circle_line", Vector3.zero, new Vector2(2.56f, 2.56f), new Color(1f, 1f, 1f, 0.45f), 4, SpriteDrawMode.Simple);
            ghostFill.enabled = ghostLine.enabled = false;

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

        internal static SpriteRenderer Sprite(Transform parent, string name, Vector3 pos, Vector2 size, Color color, int order, SpriteDrawMode mode)
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
            bestText = UIKit.Label(hud, "", 44, top, new Vector2(0, -200), new Vector2(600, 60), UIKit.Hex("#C9C3E8"));
            UIKit.IconButton(hud, "round_white", "icon_pause", OpenPause, new Vector2(0f, 1f), new Vector2(100, -100), 116);

            UIKit.Label(hud, Loc.T("Next", "Tiếp"), 36, new Vector2(1f, 1f), new Vector2(-110, -240), new Vector2(200, 50), UIKit.Hex("#C9C3E8"));
            var bubble = UIKit.Image(hud, "round_white", new Vector2(1f, 1f), new Vector2(-110, -150), new Vector2(130, 130), UIKit.Hex("#463A73"));
            nextFill = UIKit.Image(bubble.transform, "circle_fill", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(92, 92));
            UIKit.Image(nextFill.transform, "circle_line", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(92, 92));
            nextFace = Face.AddUI(nextFill.transform, new Vector2(60, 60), new Vector2(0, 4));

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
            scoreText.text = shownScore.ToString();
            var beaten = score > startBest && startBest > 0;
            bestText.text = beaten ? Loc.T("New best!", "Kỷ lục mới!") : Loc.F("Best {0}", "Kỷ lục {0}", Mathf.Max(best, score));
            bestText.color = beaten ? UIKit.Hex("#FFD23F") : UIKit.Hex("#C9C3E8");
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
            body.gravityScale = 1.1f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.simulated = simulated;
            // M2: balls roll (faces turn with them); mass by area so big balls push small ones aside
            body.freezeRotation = false;
            body.useAutoMass = false;
            body.mass = r * r * 4f;
            body.angularDamping = 0.4f;
            var col = go.GetComponent<CircleCollider2D>();
            col.radius = r;
            col.sharedMaterial = BallMaterial(tier);

            // 256 px body sprite at 100 PPU is 2.56 units across; the drawn circle is 244 px.
            var visualScale = 2f * r / 2.44f;
            // Deform never rotates (the jelly squash acts along world axes); Spin follows the body's rotation.
            var deform = new GameObject("Deform").transform;
            deform.SetParent(go.transform, false);
            var fill = Sprite(deform, "circle_fill", pos, new Vector2(2.56f, 2.56f), TierColors[tier - 1], 10, SpriteDrawMode.Simple);
            fill.transform.localScale = Vector3.one * visualScale;
            var line = Sprite(deform, "circle_line", pos, new Vector2(2.56f, 2.56f), Color.white, 11, SpriteDrawMode.Simple);
            line.transform.localScale = Vector3.one * visualScale;
            var spin = new GameObject("Spin").transform;
            spin.SetParent(deform, false);
            var face = Face.AddWorld(spin, 2f * r * 0.66f, 12);
            face.transform.localPosition = new Vector3(0f, 2f * r * 0.04f, 0f);
            if (tier == Tiers) face.SetIdle(FaceId.Smug);

            var ball = go.AddComponent<MergeBall>();
            ball.Init(this, tier, face, body, deform.gameObject.AddComponent<JellyWobble>(), r, fill, line, spin, TierColors[tier - 1]);
            if (simulated) LightStrip(tier);
            return ball;
        }

        // bouncy small balls, heavier big ones; one shared material per tier
        private readonly PhysicsMaterial2D[] ballMaterials = new PhysicsMaterial2D[Tiers + 1];

        private PhysicsMaterial2D BallMaterial(int tier) =>
            ballMaterials[tier] ??= new PhysicsMaterial2D("Ball" + tier) { friction = material.friction, bounciness = Mathf.Lerp(0.25f, 0.08f, (tier - 1) / (Tiers - 1f)) };

        private void SpawnHeld()
        {
            if (held != null) return;
            var x = lastHeldX;
            held = CreateBall(nextTier, new Vector2(Mathf.Clamp(x, JarLeft + Radius(nextTier), JarRight - Radius(nextTier)), HoldY), false);
            held.Face.React(FaceId.Smug, 0.8f);
            nextTier = RollTier();
            nextFace.Restart();
            RefreshHud();
            // M7: a finger already down while waiting keeps aiming the new ball straight away
            if (aiming && Pointer.current != null && Pointer.current.press.isPressed) Aim(Pointer.current.position.ReadValue());
        }

        private float lastHeldX;
        private int dropsSinceSave;
        private bool ended;

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
            a.Face.React(FaceId.Shock, 1f);
            b.Face.React(FaceId.Shock, 1f);
            var goo = GooMerge.Create(world, 10);
            Vector3 pa = a.transform.position, pb = b.transform.position, mid = (pa + pb) / 2f;
            float grow = tier < Tiers ? Radius(tier + 1) / r : 1.25f;
            for (float t = 0f; t < MeltTime; t += Time.deltaTime)
            {
                if (a == null || b == null) { if (goo != null) Destroy(goo.gameObject); yield break; } // game reset mid-merge
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

            // M5: merges less than a second apart chain into a combo
            combo = Time.time - lastMerge <= ComboWindow ? combo + 1 : 1;
            lastMerge = Time.time;

            if (tier == Tiers)
            {
                AddScore(100 * combo, mid);
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
            ball.Face.React(FaceId.Grin, 1.2f);
            if (!SaveStore.GetBool("merge.tip.merge", false)) // first merge ever explains the rule once
            {
                SaveStore.SetBool("merge.tip.merge", true);
                Toast.Show(hud, Loc.T("Same ones merge!", "Cùng loại thì gộp!"));
            }
            AddScore(next * (next + 1) / 2 * combo, mid);
            if (combo >= 2) FloatText("x" + combo, mid + Vector3.up * Radius(next), 90, UIKit.Hex("#FFD23F"));
            GameAudio.Play("merge", 0.8f + next * 0.07f + 0.06f * (combo - 1));
            GameFx.Play(GameFx.Colored("Merge_Fusion_{color}", TierColors[next - 1]), mid, Radius(next) * 1.6f);
            if (next >= 9) { GameFx.Play("Merge_BigFusion", mid, Radius(next)); GameAudio.Haptic(); }
            if (next == Tiers && !legendShown) Legend(mid);
            PushNeighbours(ball, next);
            // pop: from the blob's size -> overshoot -> settle. Only the visual scales; the collider stays put (M4).
            var visual = ball.Deform;
            Tween.Run(ball, PopTime + SettleTime, k =>
            {
                var t = k * (PopTime + SettleTime);
                var s = t < PopTime ? Mathf.Lerp(1f, 1.15f, Tween.Evaluate(Ease.OutQuad, t / PopTime))
                                    : Mathf.Lerp(1.15f, 1f, Tween.Evaluate(Ease.OutElastic, (t - PopTime) / SettleTime));
                if (!ball.Jelly.IsWobbling) visual.localScale = Vector3.one * s;
            }, Ease.Linear, 0f, () => { if (visual != null && !ball.Jelly.IsWobbling) visual.localScale = Vector3.one; });
        }

        // M4: the new ball nudges its neighbours out (strength by tier, radius 1.5 r)
        private void PushNeighbours(MergeBall born, int tier)
        {
            var r = Radius(tier);
            var reach = r * 1.5f;
            foreach (var b in balls)
            {
                if (b == born || b == null || !b.Body.simulated) continue;
                var d = (Vector2)(b.transform.position - born.transform.position);
                var dist = d.magnitude - Radius(b.Tier);
                if (dist > reach) continue;
                var k = 1f - Mathf.Clamp01(dist / reach);
                b.Body.AddForce(d.normalized * (0.8f + 0.25f * tier) * k * b.Body.mass, ForceMode2D.Impulse);
            }
        }

        /// <summary>A ball hit something: the glass glints when it is a wall; the first landing after a drop puffs smoke.</summary>
        internal void OnBallHit(MergeBall ball, Collision2D c, float speed)
        {
            if (c.collider.name == "Wall" && speed > 2.5f && glass != null) glass.Glint(speed / 10f);
            if (speed > 7f) ball.Face.React(FaceId.Dizzy, 0.45f);
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

        private void AddScore(int n, Vector3 at)
        {
            score += n;
            FloatText("+" + n, at, 56, UIKit.Paper);
            if (score > best)
            {
                // G6: the best is kept the moment it is beaten, not only at game over
                best = score;
                SaveStore.SetInt("merge.best", best);
                if (Time.unscaledTime >= nextBestSave) { SaveStore.Save(); nextBestSave = Time.unscaledTime + 3f; }
                if (!bestToastShown && startBest > 0)
                {
                    bestToastShown = true;
                    Toast.Show(hud, Loc.T("New best!", "Kỷ lục mới!"));
                }
            }
            RefreshHud();
        }

        // M9: the counter counts up and bounces instead of jumping
        private void LateUpdate()
        {
            if (scoreText == null || shownScore == score) return;
            var step = Mathf.Max(1, Mathf.CeilToInt((score - shownScore) * Mathf.Min(1f, Time.deltaTime * 10f)));
            shownScore = Mathf.Min(score, shownScore + step);
            scoreText.text = shownScore.ToString();
            if (shownScore == score) Tween.Punch(scoreText.transform, 0.12f, 0.2f);
        }

        private void FloatText(string text, Vector3 worldPos, float size, Color color)
        {
            var t = UIKit.Label(hud, text, size, color);
            t.rectTransform.sizeDelta = new Vector2(400, size * 1.4f);
            t.outlineWidth = 0.25f;
            t.outlineColor = UIKit.Ink;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hud, cam.WorldToScreenPoint(worldPos), cam, out var local);
            t.rectTransform.anchoredPosition = local + hud.rect.size * (new Vector2(0.5f, 0.5f) - t.rectTransform.anchorMin);
            var start = t.rectTransform.anchoredPosition;
            Tween.Run(t, 0.8f, k =>
            {
                t.rectTransform.anchoredPosition = start + new Vector2(0, 120 * k);
                t.alpha = 1f - Mathf.Max(0f, k - 0.55f) / 0.45f;
            }, Ease.OutCubic, 0f, () => Destroy(t.gameObject));
        }

        // M11: first tier 11 in a run
        private void Legend(Vector3 at)
        {
            legendShown = true;
            GameAudio.Play("big");
            GameAudio.Haptic();
            GameFx.Play("Win_Confetti", at + Vector3.up * 3f, 1.4f);
            var t = UIKit.Label(hud, Loc.T("LEGENDARY MEH!", "MEH HUYỀN THOẠI!"), 130, UIKit.Hex("#FFD23F"));
            t.rectTransform.sizeDelta = new Vector2(1000, 180);
            t.enableWordWrapping = false;
            t.enableAutoSizing = true;
            t.fontSizeMin = 60;
            t.fontSizeMax = 120;
            t.outlineWidth = 0.3f;
            t.outlineColor = UIKit.Ink;
            t.rectTransform.anchoredPosition = new Vector2(0, 250);
            t.rectTransform.localEulerAngles = new Vector3(0, 0, -4);
            t.transform.localScale = Vector3.zero;
            Tween.Scale(t.transform, Vector3.one, 0.35f, Ease.OutBack);
            Tween.Run(t, 0.4f, k => t.alpha = 1f - k, Ease.Linear, 1.8f, () => Destroy(t.gameObject));
        }

        // ---------------- input & rules ----------------

        private void Update()
        {
            if (!playing) { HideAim(); return; }
            var pointer = Pointer.current;
            if (pointer != null)
            {
                if (pointer.press.wasPressedThisFrame)
                    aiming = EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(); // EM12: Pause never drops
                if (aiming && pointer.press.isPressed && held != null) Aim(pointer.position.ReadValue());
                if (aiming && pointer.press.wasReleasedThisFrame)
                {
                    aiming = false;
                    Drop();
                }
            }
            UpdateAim();
            UpdateDanger();
            if (dropsSinceSave >= 3 && Time.time - lastDrop > 1f) { dropsSinceSave = 0; SaveRun(); }
        }

        private void UpdateDanger()
        {
            bool danger = false;
            foreach (var b in balls)
            {
                if (Time.time - b.Born < 1f) continue;
                if (b.transform.position.y + Radius(b.Tier) > DangerY) { danger = true; break; }
            }
            if (danger != dangerShown)
            {
                dangerShown = danger;
                foreach (var b in balls)
                {
                    if (danger) b.Face.React(b.transform.position.y + Radius(b.Tier) > DangerY - 1f ? FaceId.Panic : FaceId.Shock, -1f);
                    else b.Face.ClearReaction();
                }
            }
            overTimer = danger ? overTimer + Time.deltaTime : 0f;
            var pulse = danger ? 0.55f + 0.45f * Mathf.Sin(Time.time * 18f) : 0.45f;
            dangerLine.color = new Color(DangerRed.r, DangerRed.g, DangerRed.b, pulse);
            jarLine.color = danger ? Color.Lerp(Color.white, DangerRed, 0.5f + 0.5f * Mathf.Sin(Time.time * 12f)) : Color.white;
            dangerBar.enabled = dangerBarBg.enabled = danger;
            if (danger)
            {
                var k = Mathf.Clamp01(overTimer / LoseAfter);
                dangerBar.size = new Vector2(Mathf.Max(0.01f, 4.8f * k), 0.2f);
                dangerBar.transform.position = new Vector3(-2.4f + 2.4f * k, DangerY + 0.32f, 0f);
                if (Mathf.Repeat(overTimer, 0.5f) < Time.deltaTime) GameAudio.Play("thud", 0.7f, 0.6f); // heartbeat
            }
            if (overTimer > LoseAfter) GameOver();
        }

        private void Aim(Vector2 screen)
        {
            var w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
            var r = Radius(held.Tier);
            var x = Mathf.Clamp(w.x, JarLeft + r, JarRight - r);
            held.transform.position = new Vector3(x, HoldY, 0f);
            lastHeldX = x;
        }

        // M1: dotted line straight down from the held ball to the first contact (circle cast with its radius)
        private void UpdateAim()
        {
            if (held == null) { HideAim(); return; }
            var r = Radius(held.Tier);
            var from = (Vector2)held.transform.position;
            var hit = Physics2D.CircleCast(from, r * 0.98f, Vector2.down, 30f);
            var land = hit.collider != null ? from + Vector2.down * hit.distance : new Vector2(from.x, JarBottom + r);
            var top = from.y - r - 0.15f;
            var bottom = land.y + r + 0.05f;
            int n = 0;
            for (var y = top; y > bottom && n < aimDots.Count; y -= AimSpacing, n++)
            {
                aimDots[n].enabled = true;
                aimDots[n].transform.position = new Vector3(from.x, y, 0f);
            }
            for (int i = n; i < aimDots.Count; i++) aimDots[i].enabled = false;
            ghostFill.enabled = ghostLine.enabled = true;
            var s = Vector3.one * (2f * r / 2.44f);
            ghostFill.transform.position = ghostLine.transform.position = land;
            ghostFill.transform.localScale = ghostLine.transform.localScale = s;
        }

        private void HideAim()
        {
            foreach (var d in aimDots) if (d != null) d.enabled = false;
            if (ghostFill != null) ghostFill.enabled = ghostLine.enabled = false;
        }

        private void Drop()
        {
            if (held == null || Time.time - lastDrop < DropCooldown) return;
            lastDrop = Time.time;
            // ±0.02 so a ball never balances perfectly on top of another
            held.transform.position += new Vector3(Random.Range(-0.02f, 0.02f), 0f, 0f);
            held.Body.simulated = true;
            held.Born = Time.time;
            LightStrip(held.Tier);
            balls.Add(held);
            GameAudio.Play("drop");
            HideTutorial();
            held = null;
            dropsSinceSave++;
            HideAim();
            // not tied to `playing`: pausing inside the cooldown must still leave a ball to drop (EM6)
            Tween.Delay(this, DropCooldown, SpawnHeld);
        }

        private void GameOver()
        {
            playing = false;
            ended = true;
            SaveStore.Delete(RunKey);
            aiming = false;
            HideAim();
            GameAudio.Play("lose");
            dangerBar.enabled = dangerBarBg.enabled = false;
            foreach (var b in balls) { b.Body.simulated = false; b.Face.React(FaceId.Dizzy, -1f); }
            if (held != null) held.gameObject.SetActive(false);
            best = SaveStore.SubmitBest("merge.best", score);
            var newBest = score > startBest && score > 0;
            if (newBest) ReviewPrompt.GoodMoment();
            Tween.Delay(this, 0.6f, ShowGameOver);
        }

        private void ShowGameOver()
        {
            var newBest = score > startBest && score > 0;
            var p = Popup.Open(hud, Loc.T("The jar is full!", "Hũ đầy rồi!"), 1050);
            p.Text(score.ToString(), 140, UIKit.Ink, 160);
            p.Text(newBest ? Loc.T("New best!", "Kỷ lục mới!") : Loc.F("Best {0}", "Kỷ lục {0}", best), 50, newBest ? UIKit.Hex("#E9A23B") : UIKit.Muted);
            p.Space(10);
            if (!revived) RewardedButton.Add(p, Loc.T("Revive", "Hồi sinh"), "merge_revive", Revive);
            p.Button("btn_green", Loc.T("Play again", "Chơi lại"), () => p.Close(() => Ads.OnBreak("merge_gameover", NewGame)), "icon_restart");
            p.Fit();
        }

        // M13: pops every ball near the top and hands over a new ball straight away.
        private void Revive()
        {
            revived = true;
            ended = false;
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
            else SpawnHeld();
            overTimer = 0f;
            dangerShown = false;
            playing = true;
        }

        private void OpenPause()
        {
            if (!playing) return;
            SaveRun();
            playing = false;
            aiming = false;
            foreach (var b in balls) b.Body.simulated = false;
            SettingsPopup.Show(hud, () =>
                {
                    foreach (var b in balls) if (b != null) b.Body.simulated = true;
                    playing = true;
                },
                (Loc.T("Play again", "Chơi lại"), "btn_green", "icon_restart", NewGame));
        }
    }
}
