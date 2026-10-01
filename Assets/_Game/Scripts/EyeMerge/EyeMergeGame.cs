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
        // the worn ball skin's colours (MergeSkins), copied in at the start of every game
        internal static readonly Color[] TierColors = new Color[Tiers];
        private static readonly int[] SpawnWeights = { 5, 4, 3, 2, 1 };
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
        private SpriteRenderer dangerLine, dangerBar, dangerBarBg, ghostFill, ghostLine;
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
        private float lastDrop = -10f, overTimer, lastMerge = -10f;
        private bool playing, revived, aiming, dangerShown, bestToastShown, legendShown;
        private PhysicsMaterial2D material;
        private readonly List<SpriteRenderer> stageLines = new(); // shelf + pillar outlines, flash red in danger

        // Merge feel (goo melt -> white flash -> new ball pops out of the blob)
        private const float MeltTime = 0.14f, FlashTime = 0.035f, PopTime = 0.1f, SettleTime = 0.18f;
        private const float GooBlend = 1.1f;
        private const float SplashScale = 1.8f; // Merge_Splash size per unit of ball radius: rings just past the ball

        private void Start()
        {
            cam = Camera.main;
            // Keep the 10.8-unit-wide jar area visible on tall and wide screens alike.
            cam.orthographicSize = Mathf.Max(9.6f, 5.4f / cam.aspect);
            canvas = UIKit.CreateCameraCanvas("EyeMergeUI", cam, 100);
            canvas.sortingLayerName = "Glass"; // HUD and popups above the whole play field (top sorting layer)
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
            public bool revived, legend, bestToast;
            public int maxTier;
            public List<BallData> balls = new();
        }

        [System.Serializable]
        private class BallData { public int t; public float x, y, rot; }

        private void SaveRun()
        {
            if (ended) return; // game over: nothing to resume
            var d = new RunData { score = score, startBest = startBest, nextTier = nextTier, heldTier = held != null ? held.Tier : nextTier, heldX = lastHeldX, revived = revived, legend = legendShown, bestToast = bestToastShown, maxTier = maxTier };
            foreach (var b in balls)
                if (b != null) d.balls.Add(new BallData { t = b.Tier, x = b.transform.position.x, y = b.transform.position.y, rot = b.transform.eulerAngles.z });
            foreach (var m in inFlight) // a pair mid-merge is saved as the ball it is about to become
                if (m.tier < Tiers) d.balls.Add(new BallData { t = m.tier + 1, x = m.pos.x, y = m.pos.y });
            SaveStore.SetJson(RunKey, d);
            SaveStore.SaveSoon();
        }

        private bool TryRestore()
        {
            if (!SaveStore.Has(RunKey)) return false;
            var d = SaveStore.GetJson<RunData>(RunKey);
            if (d.balls == null || d.heldTier < 1) { SaveStore.Delete(RunKey); return false; }
            d.heldTier = Mathf.Clamp(d.heldTier, 1, SpawnWeights.Length); // a damaged save must not index past the colours
            NewGame(false);
            score = shownScore = d.score;
            startBest = d.startBest;
            best = Mathf.Max(best, score);
            revived = d.revived;
            legendShown = d.legend;
            bestToastShown = d.bestToast; // the "New best!" toast already showed in this run
            foreach (var bd in d.balls)
            {
                if (bd.t < 1 || bd.t > Tiers) continue;
                var b = CreateBall(bd.t, new Vector2(bd.x, bd.y), true);
                b.transform.rotation = Quaternion.Euler(0, 0, bd.rot);
                b.Born = Time.time - 5f;
                b.Landed = true;
                balls.Add(b);
            }
            for (int t = 1; t <= Mathf.Clamp(d.maxTier, 0, Tiers); t++) LightStrip(t, true); // tiers reached earlier stay lit
            lastHeldX = d.heldX;
            nextTier = d.heldTier;
            SpawnHeld();
            nextTier = Mathf.Clamp(d.nextTier, 1, SpawnWeights.Length);
            RefreshHud();
            if (!SaveStore.GetBool("merge.tutorial", false)) ShowTutorial(); // killed before the first drop
            OpenPause();
            return true;
        }

        private void OnBack()
        {
            if (shop != null && shop.IsOpen) { shop.Close(); return; }
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
            inFlight.Clear();
            dropsSinceSave = 0;
            aiming = false;
            aimDots.Clear();
            held = null;
            score = shownScore = combo = maxTier = 0;
            overTimer = 0f;
            revived = dangerShown = bestToastShown = legendShown = false;
            best = startBest = SaveStore.GetInt("merge.best");
            ApplySkin();
            coinsEarned = 0;
            coinsDoubled = false;
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
            var bubble = UIKit.Image(tutorialRoot, "panel", new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(660, 190));
            var t = UIKit.Label(bubble.transform, Loc.T("Drag to aim,\nrelease to drop", "Kéo để ngắm,\nthả để rơi"), 56);
            UIKit.Stretch(t.rectTransform, 20);
            var hand = UIKit.Hand(tutorialRoot, new Vector2(0.5f, 1f), new Vector2(12, -448));
            var home = hand.anchoredPosition;
            Tween.Loop(hand, s => hand.anchoredPosition = home + new Vector2(Mathf.Sin(s * 2.2f) * 180f, 0f));
        }

        private void HideTutorial()
        {
            if (tutorialRoot == null) return;
            Destroy(tutorialRoot.gameObject);
            tutorialRoot = null;
            SaveStore.SetBool("merge.tutorial", true);
            SaveStore.Save();
        }

        // ---------------- skins (shop) ----------------

        private void ApplySkin()
        {
            var palette = Skins.Palette ?? Skins.CatalogOf(MergeSkins.GameId).Default(0).Colors;
            for (int i = 0; i < Tiers; i++) TierColors[i] = palette[Mathf.Min(i, palette.Length - 1)];
            cam.backgroundColor = MergeSkins.Stage[0];
        }

        // The merge effect of the worn ball skin (default: the jelly splat, plus the big one from tier 9 up).
        private static void Splash(Vector3 at, int tier, float scale, bool big = false)
        {
            var skin = Skins.Equipped(0);
            var color = TierColors[tier - 1];
            if (skin == null || skin.Fx == null)
            {
                GameFx.Play("Merge_Splash", at, color, scale);
                if (big) GameFx.Play("Merge_SplashBig", at, color, scale * 1.3f);
                return;
            }
            GameFx.Play(skin.Fx, at, skin.FxPieceColor ? color : skin.FxTint, scale * skin.FxScale * (big ? 1.3f : 1f));
        }

        private ShopScreen shop;

        // From the pause popup: the run is saved, and rebuilt in the new skin when the shop closes (back to pause).
        private void OpenShopFromPause()
        {
            SaveRun();
            shop = ShopScreen.Open(hud, new MergeShopPainter(), () => { if (!TryRestore()) NewGame(); });
        }

        private void BuildJar()
        {
            // Open stage (mockup JAR_D): no jar drawn over the balls. A darker floor band, a shelf the pile sits on and
            // two pillars marking the walls; everything stays behind the balls, the danger dashes too.
            stageLines.Clear();
            // floor band: sliced at native scale so its top edge stays crisp (a scaled-up pill would blur it); the round
            // corners sit far off screen
            Sprite(world, "bar_fill", new Vector3(0f, JarBottom - 10f, 0f), new Vector2(40f, 20f), StageFloor, -3, SpriteDrawMode.Sliced);
            StagePill(new Vector2(0f, JarBottom - StageThick / 2f), JarRight - JarLeft + 0.72f, StageThick, StageShelf, -1);
            var pillarTop = DangerY + 0.2f;
            foreach (var x in new[] { JarLeft - StageThick / 2f, JarRight + StageThick / 2f })
            {
                var pillar = StagePill(new Vector2(x, (pillarTop + JarBottom) / 2f), pillarTop - JarBottom, StageThick, StagePillar, -1);
                pillar.transform.rotation = Quaternion.Euler(0, 0, 90f);
                stageLines[^1].transform.rotation = pillar.transform.rotation;
            }
            dangerLine = Sprite(world, "danger_dash", new Vector3(0, DangerY, 0), new Vector2(JarRight - JarLeft - 0.2f, 0.16f), DangerRed, 0, SpriteDrawMode.Tiled);

            // M8: 2 s countdown bar just above the danger line, visible only while a ball is over it
            dangerBarBg = Sprite(world, "bar_fill", new Vector3(0, DangerY + 0.32f, 0), Vector2.one, new Color(0f, 0f, 0f, 0.35f), 26, SpriteDrawMode.Sliced);
            dangerBar = Sprite(world, "bar_fill", new Vector3(0, DangerY + 0.32f, 0), Vector2.one, DangerRed, 27, SpriteDrawMode.Sliced);
            SetPill(dangerBarBg, 4.8f, 0.28f, -2.4f);
            SetPill(dangerBar, 0f, 0.2f, -2.36f);
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

        // A bar drawn from the 9-sliced "bar_fill" (48 px tall, 24 px round ends). The slice keeps its native height and
        // the transform scales it down, so the round ends never get squeezed; the width never goes below one full pill
        // (both ends touching), which is where a sliced sprite would otherwise break apart.
        private const float BarNative = 0.48f;
        private const float StageThick = 0.36f, StageOutline = 0.1f;
        private static Color StageFloor => MergeSkins.Stage[1];
        private static Color StageShelf => MergeSkins.Stage[2];
        private static Color StagePillar => MergeSkins.Stage[3];

        // A round-ended bar centred at `centre` (horizontal; rotate it for a pillar) with an ink outline drawn as a
        // slightly bigger pill underneath. Returns the fill.
        private SpriteRenderer StagePill(Vector2 centre, float length, float thick, Color fill, int order)
        {
            var line = Sprite(world, "bar_fill", centre, Vector2.one, UIKit.Ink, order - 1, SpriteDrawMode.Sliced);
            SetPill(line, length + 2f * StageOutline, thick + 2f * StageOutline, centre.x - length / 2f - StageOutline);
            stageLines.Add(line);
            var sr = Sprite(world, "bar_fill", centre, Vector2.one, fill, order, SpriteDrawMode.Sliced);
            SetPill(sr, length, thick, centre.x - length / 2f);
            return sr;
        }

        private static void SetPill(SpriteRenderer sr, float width, float height, float left)
        {
            var s = height / BarNative;
            var w = Mathf.Max(width, height);
            sr.transform.localScale = new Vector3(s, s, 1f);
            sr.size = new Vector2(w / s, BarNative);
            var p = sr.transform.position;
            sr.transform.position = new Vector3(left + w / 2f, p.y, p.z);
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

            // the tier chart grows like the balls do (44 → 84 px), so "bigger = later" reads at a glance
            strip = new Image[Tiers];
            const float gap = 14f;
            float width = 0f;
            for (int t = 1; t <= Tiers; t++) width += StripSize(t) + (t > 1 ? gap : 0f);
            var x = -width / 2f;
            for (int t = 1; t <= Tiers; t++)
            {
                var size = StripSize(t);
                var img = UIKit.Image(hud, "circle_fill", new Vector2(0.5f, 0f), new Vector2(x + size / 2f, 74), new Vector2(size, size), TierColors[t - 1]);
                UIKit.Image(img.transform, "circle_line", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
                x += size + gap;
                img.color = new Color(img.color.r, img.color.g, img.color.b, 0.25f);
                strip[t - 1] = img;
            }
            RefreshHud();
        }

        private static float StripSize(int tier) => 44f + 4f * (tier - 1);

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

        // a brand-new player's first drops only use the three smallest balls: merges come fast while the rule sinks in
        private const int GentleDrops = 15;

        private int RollTier()
        {
            var count = SaveStore.GetInt("merge.drops.total") < GentleDrops ? 3 : SpawnWeights.Length;
            int total = 0;
            for (int i = 0; i < count; i++) total += SpawnWeights[i];
            var x = Random.Range(0, total);
            for (int i = 0; i < count; i++)
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
            if (held != null || ended) return; // lost inside the drop cooldown: no new ball behind the game-over card
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
        private int dropsSinceSave, maxTier;
        private bool ended;
        private int coinsEarned;
        private bool coinsDoubled;

        private class PendingMerge { public int tier; public Vector3 pos; }
        private readonly List<PendingMerge> inFlight = new();

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
            // a save taken while this pair melts must still contain the result (the pair already left `balls`)
            var pending = new PendingMerge { tier = tier, pos = mid };
            inFlight.Add(pending);
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
                inFlight.Remove(pending);
                // the hardest thing in the game: two legends melt into nothing — make it worth it
                AddScore(1000 * combo, mid);
                FloatText(Loc.T("MEGA MEH!", "SIÊU MEH!"), mid + Vector3.up * 2.6f, 110, UIKit.Hex("#FFD23F"));
                GameAudio.Haptic(HapticLevel.Strong);
                GameAudio.Play("big");
                Splash(mid, Tiers, Radius(Tiers) * 1.1f, true);
                GameFx.Play("Win_Confetti", mid, 1.6f);
                yield break;
            }
            var next = tier + 1;
            var ball = CreateBall(next, mid, true);
            // Paused or lost while the pair was melting: every other ball is frozen (not simulated, no collider), so
            // a live ball here would fall straight through the pile. It joins the frozen state and wakes with the rest.
            ball.Body.simulated = playing;
            ball.Born = Time.time;
            ball.Landed = true;
            balls.Add(ball);
            inFlight.Remove(pending);
            if (ended) { ball.Face.React(FaceId.Dizzy, -1f); yield break; }
            ball.Face.React(FaceId.Grin, 1.2f);
            if (!SaveStore.GetBool("merge.tip.merge", false)) // first merge ever explains the rule once
            {
                SaveStore.SetBool("merge.tip.merge", true);
                Toast.Show(hud, Loc.T("Same ones merge!", "Cùng loại thì gộp!"));
            }
            AddScore(next * (next + 1) / 2 * combo, mid);
            if (combo >= 2) FloatText("x" + combo, mid + Vector3.up * Radius(next), 90, UIKit.Hex("#FFD23F"));
            GameAudio.Play("merge", 0.8f + next * 0.07f + 0.06f * (combo - 1));
            // a jelly splat in the new ball's colour, a touch wider than the ball so it reads around it
            Splash(mid, next, SplashScale * Radius(next), next >= 9);
            GameAudio.Haptic(next >= 9 || combo >= 3 ? HapticLevel.Strong : HapticLevel.Medium);
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

        /// <summary>A ball hit something: the first landing after a drop puffs smoke.</summary>
        internal void OnBallHit(MergeBall ball, Collision2D c, float speed)
        {
            if (speed > 7f) ball.Face.React(FaceId.Dizzy, 0.45f);
            if (ball.Landed) return;
            ball.Landed = true;
            if (speed <= 3f) return;
            var contact = c.GetContact(0).point;
            var r = Radius(ball.Tier);
            GameFx.Play("Land_Poof", contact + Vector2.left * r * 0.6f, r * 0.45f);
            GameFx.Play("Land_Poof", contact + Vector2.right * r * 0.6f, r * 0.45f);
        }

        private void LightStrip(int tier, bool quiet = false)
        {
            maxTier = Mathf.Max(maxTier, tier);
            var dot = strip[tier - 1];
            if (dot.color.a >= 1f) return;
            dot.color = TierColors[tier - 1];
            Tween.Punch(dot.transform, 0.5f, 0.4f);
            // M10: a tier seen for the first time this run gets a rising chime (the small spawn tiers stay silent)
            if (!quiet && tier >= 4) GameAudio.Play("merge", 1.2f + 0.05f * tier, 0.7f);
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
                SaveStore.SaveSoon();
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
            t.rectTransform.sizeDelta = new Vector2(900, size * 1.4f);
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
            GameAudio.Haptic(HapticLevel.Strong);
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
                if (danger && !SaveStore.GetBool("merge.tip.danger", false)) // the first time ever: what the red line means
                {
                    SaveStore.SetBool("merge.tip.danger", true);
                    Toast.Show(hud, Loc.T("Over the line for 2 s = game over!", "Quá vạch đỏ 2 giây là thua!"));
                }
                foreach (var b in balls)
                {
                    if (danger) b.Face.React(b.transform.position.y + Radius(b.Tier) > DangerY - 1f ? FaceId.Panic : FaceId.Shock, -1f);
                    else b.Face.ClearReaction();
                }
            }
            overTimer = danger ? overTimer + Time.deltaTime : 0f;
            var pulse = danger ? 0.55f + 0.45f * Mathf.Sin(Time.time * 18f) : 0.45f;
            dangerLine.color = new Color(DangerRed.r, DangerRed.g, DangerRed.b, pulse);
            var edge = danger ? Color.Lerp(UIKit.Ink, DangerRed, 0.5f + 0.5f * Mathf.Sin(Time.time * 12f)) : UIKit.Ink;
            foreach (var line in stageLines) line.color = edge;
            dangerBar.enabled = dangerBarBg.enabled = danger;
            if (danger)
            {
                var k = Mathf.Clamp01(overTimer / LoseAfter);
                SetPill(dangerBar, 4.72f * k, 0.2f, -2.36f);
                if (Mathf.Repeat(overTimer, 0.5f) < Time.deltaTime) { GameAudio.Play("thud", 0.7f, 0.6f); GameAudio.Haptic(HapticLevel.Light); } // heartbeat (M8)
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
            GameAudio.Haptic(HapticLevel.Light);
            HideTutorial();
            var dropped = SaveStore.GetInt("merge.drops.total");
            if (dropped < GentleDrops) SaveStore.SetInt("merge.drops.total", dropped + 1);
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
            GameAudio.Haptic(HapticLevel.Strong);
            dangerBar.enabled = dangerBarBg.enabled = false;
            foreach (var b in balls) { b.Body.simulated = false; b.Face.React(FaceId.Dizzy, -1f); }
            if (held != null) held.gameObject.SetActive(false);
            best = SaveStore.SubmitBest("merge.best", score);
            coinsEarned = score / 50; // SH1: score / 50
            Wallet.Add(coinsEarned);
            Tween.Delay(this, 0.6f, ShowGameOver);
        }

        private void ShowGameOver()
        {
            var newBest = score > startBest && score > 0;
            var p = Popup.Open(hud, Loc.T("The jar is full!", "Hũ đầy rồi!"), 1050);
            p.Text(score.ToString(), 140, UIKit.Ink, 160);
            p.Text(newBest ? Loc.T("New best!", "Kỷ lục mới!") : Loc.F("Best {0}", "Kỷ lục {0}", best), 50, newBest ? UIKit.Hex("#E9A23B") : UIKit.Muted);
            if (maxTier > 0) // spec 3.3: the biggest ball of the run
            {
                var row = p.Row(110);
                UIKit.Label(row, Loc.T("Biggest ball", "Bóng to nhất"), 46, new Vector2(0.5f, 0.5f), new Vector2(-70, 0), new Vector2(420, 70), UIKit.Muted);
                var ball = UIKit.Image(row, "circle_fill", new Vector2(0.5f, 0.5f), new Vector2(200, 0), new Vector2(96, 96), TierColors[maxTier - 1]);
                UIKit.Image(ball.transform, "circle_line", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96, 96));
                Face.AddUI(ball.transform, new Vector2(62, 62), new Vector2(0, 4)).SetIdle(FaceId.Smug);
            }
            CoinReward.Add(p, coinsEarned, coinsDoubled, "merge_coins", () => coinsDoubled = true,
                () => p.Close(() => shop = ShopScreen.Open(hud, new MergeShopPainter(), ShowGameOver)));
            p.Space(10);
            if (!revived) RewardedButton.Add(p, Loc.T("Revive", "Hồi sinh"), "merge_revive", Revive);
            // review only once the card is gone (G10), and never an interstitial right on top of it
            p.Button("btn_green", Loc.T("Play again", "Chơi lại"), () => p.Close(() =>
            {
                if (newBest && ReviewPrompt.GoodMoment()) NewGame();
                else Ads.OnBreak("merge_gameover", NewGame);
            }), "icon_restart");
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
                // "near the top" by the ball's upper part, capped at 1 unit: a tier-11 ball (r 3.5) resting on the
                // floor reaches above the cut-off with its full radius and would be deleted
                if (b.transform.position.y + Mathf.Min(Radius(b.Tier), 1f) > DangerY - 3.2f)
                {
                    Splash(b.transform.position, b.Tier, SplashScale * Radius(b.Tier));
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
                (Loc.T("Shop", "Cửa hàng"), "btn_yellow", "icon_bag", OpenShopFromPause),
                (Loc.T("Play again", "Chơi lại"), "btn_green", "icon_restart", NewGame));
        }
    }
}
