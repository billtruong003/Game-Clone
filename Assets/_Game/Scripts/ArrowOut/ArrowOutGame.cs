using System.Collections.Generic;
using System.Linq;
using CasualGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CasualGame.ArrowOut
{
    /// <summary>
    /// Arrow Out: a calm brain puzzle. Screens: Home → Level select (10 groups of 10) → Level (back / hint / restart /
    /// pause, previous / next) plus Endless and Daily. All UI is built from the shared UI sheet.
    /// </summary>
    public class ArrowOutGame : MonoBehaviour
    {
        private static Color Background => ArrowSkins.Paper[0];
        private static Color TextInk => ArrowSkins.Paper[2];
        private static Color TextMuted => ArrowSkins.Paper[3];
        private const float CellSize = 136f;
        private const int MaxHearts = 3;
        private const int PerPage = 10;
        private const int HeartEvery = 40;
        private const int FreeMistakesUpTo = 5; // levels 1–5 explain a blocked tap instead of costing a heart

        [SerializeField] private TextAsset levelsJson; // wired by the Build Switcher

        private Canvas canvas;
        private Image ground;
        private Camera cam;
        private RectTransform safe;
        private RectTransform screen;
        private LevelData[] levels;

        private enum ScreenKind { Home, Levels, Play }
        private enum State { Idle, Playing, Paused, Ended }
        private enum Mode { Level, Endless, Daily }
        private ScreenKind current;
        private State state;
        private Mode mode;
        private LevelData level;
        private Board board;
        private EndlessRun run;
        private ArrowBoardView view;
        private int lives, mistakes, runBest;
        private string dailyKey;
        private bool revived;
        private readonly List<Image> hearts = new();
        private TextMeshProUGUI scoreText, stageText, comboText, hintBadge, tipText, bestText;
        private Image comboDot;
        private RectTransform tutorialHand;
        private readonly List<Arrow> freeBuffer = new();

        private void Start()
        {
            cam = Camera.main;
            cam.clearFlags = CameraClearFlags.SolidColor;
            canvas = UIKit.CreateCameraCanvas("ArrowOutUI", cam);
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1; // theme shaders read uv1
            // the theme's ground (chalkboard, blueprint grid, CRT glass…) fills the whole screen behind everything
            ground = UIKit.AddImage(UIKit.Stretch(UIKit.Rect("Ground", canvas.transform)), (Sprite)null, Color.white);
            ground.enabled = false;
            safe = UIKit.Stretch(UIKit.Rect("Safe", canvas.transform));
            safe.gameObject.AddComponent<SafeArea>();
            levels = JsonUtility.FromJson<LevelPack>(levelsJson.text).levels;
            AppEvents.Back += OnBack;
            AppEvents.Suspended += OnSuspended;
            if (!TryRestore()) ShowHome();
            GameAudio.PlayMusic("music_arrow");
        }

        private void OnDestroy()
        {
            AppEvents.Back -= OnBack;
            AppEvents.Suspended -= OnSuspended;
        }

        // G4 / AO12: Home asks to quit, Level select goes Home, a level opens Pause.
        private void OnBack()
        {
            if (shop != null && shop.IsOpen) { shop.Close(); return; }
            switch (current)
            {
                case ScreenKind.Home: AskQuit(); break;
                case ScreenKind.Levels: ShowHome(); break;
                case ScreenKind.Play: if (state == State.Playing) OpenPause(); break;
            }
        }

        private void OnSuspended()
        {
            SaveRunBest();
            SaveRun();
            if (current == ScreenKind.Play && state == State.Playing) OpenPause();
        }

        // ---------------- run in progress (G6): the level / endless / daily board being played ----------------

        private const string RunKey = "arrow.run";

        [System.Serializable]
        private class RunData
        {
            public int mode, level, lives, mistakes, cleared, score, combo, comboColor, runBest;
            public bool revived;
            public string dailyKey;
            public ArrowData[] arrows;
        }

        private void SaveRun()
        {
            if (current != ScreenKind.Play || state == State.Ended || state == State.Idle) return;
            var d = new RunData
            {
                mode = (int)mode, level = level?.n ?? 0, lives = lives, mistakes = mistakes, revived = revived, dailyKey = dailyKey, runBest = runBest,
                cleared = run?.Cleared ?? 0, score = run?.Score ?? 0, combo = run?.Combo ?? 0, comboColor = run?.ComboColor ?? -1,
                arrows = LevelData.FromBoard(0, 0, 0, board, 0f).arrows,
            };
            SaveStore.SetJson(RunKey, d);
            SaveStore.SaveSoon(); // every tap: throttled, flushed on background
        }

        private static void ClearRun() => SaveStore.Delete(RunKey);

        private bool TryRestore()
        {
            if (!SaveStore.Has(RunKey)) return false;
            var d = SaveStore.GetJson<RunData>(RunKey);
            ClearRun();
            if (d.arrows == null || d.arrows.Length == 0 || d.lives <= 0) return false;
            var m = (Mode)Mathf.Clamp(d.mode, 0, 2);
            Board b;
            if (m == Mode.Level)
            {
                if (d.level < 1 || d.level > levels.Length) return false;
                level = levels[d.level - 1];
                b = new Board(Board.CenteredMask(level.w, level.h));
            }
            else
            {
                level = null;
                b = new Board();
            }
            foreach (var a in d.arrows)
            {
                var cells = new Pos[a.cells.Length / 2];
                for (int i = 0; i < cells.Length; i++) cells[i] = new Pos(a.cells[i * 2], a.cells[i * 2 + 1]);
                b.Place(new Arrow { Id = b.NextId++, Cells = cells, Dir = a.dir, Color = a.color });
            }
            EndlessRun r = null;
            if (m != Mode.Level)
            {
                dailyKey = d.dailyKey;
                // daily keeps its board; the remaining spawns come from a seed tied to the day and the progress
                var rand = m == Mode.Daily ? Rng.Seeded(DailySeedOf(d.dailyKey) * 31 + d.cleared) : Rng.Unity();
                r = new EndlessRun(rand, b, d.cleared, d.score, d.combo, d.comboColor);
            }
            BuildPlayScreen(m, b, r);
            lives = d.lives;
            mistakes = d.mistakes;
            revived = d.revived;
            runBest = Mathf.Max(runBest, d.runBest);
            RefreshHud();
            // killed during level 1's lesson: the lesson comes back with the board (else it would repeat forever)
            if (m == Mode.Level && level.n == 1 && !ArrowProgress.TutorialSeen && board.Count > 0) ShowTutorial();
            OpenPause();
            return true;
        }

        // the daily run keeps drawing from the day it started on, even when resumed after midnight
        private static int DailySeedOf(string key)
        {
            var digits = key != null && key.StartsWith("arrow.daily.") ? key.Substring("arrow.daily.".Length) : null;
            return int.TryParse(digits, out var seed) ? seed : Rng.DailySeed(System.DateTime.UtcNow);
        }

        // ---------------- screens ----------------

        private RectTransform NewScreen(string name, ScreenKind kind)
        {
            SaveRunBest(); // A11: leaving a run keeps its best
            if (screen != null) Destroy(screen.gameObject);
            ApplySkin();
            Tween.Kill(this);
            GameFx.StopAll();
            screen = UIKit.Stretch(UIKit.Rect(name, safe));
            var group = screen.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            Tween.Fade(group, 1f, 0.2f);
            if (kind != ScreenKind.Play) ClearRun(); // left the board on purpose: nothing to resume
            current = kind;
            state = State.Idle;
            run = null;
            tutorialHand = null;
            return screen;
        }

        private void ShowHome()
        {
            var s = NewScreen("Home", ScreenKind.Home);
            var top = new Vector2(0.5f, 1f);

            // Title doodle drawn like a real arrow, with the face on its tail.
            var doodle = UIKit.Rect("Doodle", s);
            UIKit.Place(doodle, top, new Vector2(0, -330), new Vector2(600, 150));
            var sample = new[] { new Pos(0, 3), new Pos(0, 2), new Pos(1, 2), new Pos(1, 1), new Pos(1, 0) };
            var points = new Vector2[sample.Length];
            for (int i = 0; i < sample.Length; i++) points[i] = new Vector2((sample[i].C - 1.5f) * 75, -(sample[i].R - 0.5f) * 75);
            var doodleStroke = ArrowBoardView.CreateStroke(doodle, points, 1, 75, ArrowBoardView.Palette[1]);
            var cap = UIKit.Image(doodle, "dot", new Vector2(0.5f, 0.5f), new Vector2(-1.5f * 75, -0.5f * 75), new Vector2(46, 46), ArrowBoardView.Palette[1]);
            var doodleFace = Face.AddUI(cap.transform, new Vector2(34, 34), new Vector2(0, 1));
            ArrowSkins.Dress(ArrowSkins.Current, doodleStroke, cap, doodleFace, ArrowBoardView.Palette[1], ArrowSkins.Paper[0]);

            UIKit.Label(s, "BRUH ARROWS", 132, top, new Vector2(0, -520), new Vector2(1000, 180), TextInk);
            UIKit.Label(s, Loc.T("Tap. Yeet. Bruh.", "Chạm. Phóng. Bruh."), 50, top, new Vector2(0, -625), new Vector2(1000, 70), TextMuted);

            var mid = new Vector2(0.5f, 0.5f);
            var allClear = ArrowProgress.Cleared >= ArrowProgress.LevelCount;
            var next = Mathf.Min(ArrowProgress.Unlocked, ArrowProgress.LevelCount);
            if (allClear) // A14: after level 100 the main button leads to Endless
                UIKit.Button(s, "btn_green", Loc.T("Play Endless", "Chơi Vô hạn"), () => StartEndless(false), mid, new Vector2(0, 120), new Vector2(720, 170), "icon_infinity", 64);
            else
                UIKit.Button(s, "btn_green", Loc.F("Play · Level {0}", "Chơi · Màn {0}", next), () => StartLevel(next), mid, new Vector2(0, 120), new Vector2(720, 170), "icon_play", 64);
            UIKit.Button(s, "btn_blue", Loc.T("Levels", "Chọn màn"), () => ShowLevelSelect((next - 1) / PerPage), mid, new Vector2(0, -80), new Vector2(720, 150), "icon_levels");
            if (!allClear)
                UIKit.Button(s, "btn_yellow", Loc.T("Endless", "Vô hạn"), () => StartMode(false), mid, new Vector2(0, -260), new Vector2(720, 150), "icon_infinity");
            var daily = UIKit.Button(s, "btn_white", Loc.T("Daily challenge", "Thử thách hôm nay"), () => StartMode(true), mid, new Vector2(0, allClear ? -260 : -440), new Vector2(720, 150), "icon_calendar");
            // A10: red dot until today's (UTC) board is played — only once the basics are learned, so it can't pull a
            // brand-new player away from the tutorial
            if (ArrowProgress.TutorialSeen && !ArrowProgress.PlayedToday)
                UIKit.Image(daily.transform, "dot", new Vector2(1f, 1f), new Vector2(-24, -20), new Vector2(52, 52), ArrowBoardView.ErrorColor);

            var shopButton = UIKit.Button(s, "btn_white", Loc.T("Shop", "Cửa hàng"), () => OpenShop(ShowHome), mid, new Vector2(0, allClear ? -440 : -620), new Vector2(720, 150), "icon_coin");
            shopButton.GetComponent<Image>().color = UIKit.Hex("#F7B4D2");
            if (Skins.AnyAffordable()) // SH7: enough coins for a skin not owned yet
                UIKit.Image(shopButton.transform, "dot", new Vector2(1f, 1f), new Vector2(-24, -20), new Vector2(52, 52), ArrowBoardView.ErrorColor);
            CoinPill(s);

            var bottom = new Vector2(0.5f, 0f);
            UIKit.IconButton(s, "round_white", "icon_settings", () => SettingsPopup.Show(safe, null), bottom, new Vector2(-200, 150));
            UIKit.IconButton(s, "round_yellow", "icon_trophy", ShowRecords, bottom, new Vector2(0, 150));
            if (!Ads.RemoveAdsOwned)
                UIKit.IconButton(s, "round_white", "icon_noads", () => Store.BuyRemoveAds(ok => { if (ok) ShowHome(); }), bottom, new Vector2(200, 150));
        }

        private void AskQuit()
        {
            if (Popup.AnyOpen) return;
            Popup p = null;
            p = Popup.Open(safe, Loc.T("Quit game?", "Thoát game?"), 700);
            p.OnBack = () => p.Close();
            p.Text(Loc.T("Your progress is saved.", "Tiến độ đã được lưu."), 48, UIKit.Muted);
            p.Space(10);
            p.Button("btn_green", Loc.T("Keep playing", "Chơi tiếp"), () => p.Close());
            p.Button("btn_white", Loc.T("Quit", "Thoát"), () => { SaveStore.Save(); Application.Quit(); });
            p.Fit();
        }

        private void ShowRecords()
        {
            Popup p = null;
            p = Popup.Open(safe, Loc.T("Stats", "Thành tích"), 900);
            p.OnBack = () => p.Close();
            p.Text(Loc.F("Cleared: {0}/{1} levels", "Đã qua: {0}/{1} màn", ArrowProgress.Cleared, ArrowProgress.LevelCount), 54);
            p.Text(Loc.F("Stars: {0}/{1}", "Tổng sao: {0}/{1}", ArrowProgress.TotalStars, ArrowProgress.LevelCount * 3), 54);
            p.Text(Loc.F("Endless best: {0}", "Kỷ lục vô hạn: {0}", ArrowProgress.EndlessBest), 54);
            p.Text(Loc.F("Today: {0}", "Hôm nay: {0}", ArrowProgress.DailyBest(ArrowProgress.TodayKey)), 54);
            p.Space(20);
            p.Button("btn_green", Loc.T("Close", "Đóng"), () => p.Close());
            p.Fit();
        }

        // A7: 10 groups of 10 levels, one card per group with its star count.
        private void ShowLevelSelect(int page)
        {
            var pages = Mathf.CeilToInt(ArrowProgress.LevelCount / (float)PerPage);
            page = Mathf.Clamp(page, 0, pages - 1);
            var s = NewScreen("Levels", ScreenKind.Levels);
            var top = new Vector2(0.5f, 1f);
            UIKit.IconButton(s, "round_white", "icon_back", ShowHome, new Vector2(0f, 1f), new Vector2(100, -100), 116);
            UIKit.Label(s, Loc.T("Levels", "Chọn màn"), 84, top, new Vector2(0, -100), new Vector2(600, 110), TextInk);
            UIKit.Image(s, "icon_star", top, new Vector2(-95, -205), new Vector2(60, 60));
            UIKit.Label(s, $"{ArrowProgress.TotalStars} / {ArrowProgress.LevelCount * 3}", 50, top, new Vector2(40, -205), new Vector2(260, 70), TextInk);

            int first = page * PerPage + 1, last = Mathf.Min(first + PerPage - 1, ArrowProgress.LevelCount);
            var card = UIKit.Image(s, "panel", new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(960, 760));
            UIKit.Label(card.transform, Loc.F("Levels {0}–{1}", "Màn {0}–{1}", first, last), 68, top, new Vector2(0, -80), new Vector2(800, 90));
            int groupStars = 0;
            for (int n = first; n <= last; n++) groupStars += ArrowProgress.Stars(n);
            UIKit.Image(card.transform, "icon_star", top, new Vector2(-70, -160), new Vector2(46, 46));
            UIKit.Label(card.transform, $"{groupStars} / {(last - first + 1) * 3}", 42, top, new Vector2(30, -160), new Vector2(200, 60), UIKit.Muted);

            var unlocked = ArrowProgress.Unlocked;
            for (int n = first; n <= last; n++)
            {
                int i = n - first, col = i % 5, row = i / 5;
                var pos = new Vector2((col - 2f) * 176, 40 - row * 240);
                bool locked = n > unlocked, boss = n % 10 == 0, currentLevel = n == unlocked && n > ArrowProgress.Cleared;
                var sprite = locked ? "tile_locked" : currentLevel ? "tile_current" : boss ? "tile_boss" : "tile_open";
                int level = n;
                var tile = UIKit.Button(card.transform, sprite, "", () => StartLevel(level), new Vector2(0.5f, 0.5f), pos, new Vector2(156, 156));
                tile.interactable = !locked;
                UIKit.Label(tile.transform, n.ToString(), 64, new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(150, 90),
                    locked ? (boss ? ArrowBoardView.ErrorColor : UIKit.Hex("#9EA2B6")) : boss ? ArrowBoardView.ErrorColor : UIKit.Ink);
                if (locked)
                {
                    // small lock in the corner, never over the number
                    var lk = UIKit.Image(tile.transform, "icon_lock", new Vector2(1f, 1f), new Vector2(-30, -30), new Vector2(40, 40));
                    lk.color = boss ? ArrowBoardView.ErrorColor : UIKit.Hex("#9EA2B6");
                    continue;
                }
                if (n > ArrowProgress.Cleared) continue;
                var stars = ArrowProgress.Stars(n);
                for (int k = 0; k < 3; k++) // stars sit under the tile, inside the card
                    UIKit.Image(tile.transform, k < stars ? "icon_star" : "icon_star_empty", new Vector2(0.5f, 0f), new Vector2((k - 1) * 40, -26), new Vector2(38, 38));
            }

            var bottom = new Vector2(0.5f, 0f);
            var prev = UIKit.IconButton(s, "round_white", "icon_back", () => ShowLevelSelect(page - 1), bottom, new Vector2(-300, 330), 116);
            var nextBtn = UIKit.IconButton(s, "round_white", "icon_next", () => ShowLevelSelect(page + 1), bottom, new Vector2(300, 330), 116);
            UIKit.SetInteractable(prev, page > 0);
            UIKit.SetInteractable(nextBtn, page < pages - 1);
            UIKit.Label(s, $"{page + 1} / {pages}", 60, bottom, new Vector2(0, 330), new Vector2(300, 90), TextInk);
            for (int i = 0; i < pages; i++)
                UIKit.Image(s, "dot", bottom, new Vector2((i - (pages - 1) / 2f) * 44, 220), new Vector2(i == page ? 30 : 20, i == page ? 30 : 20),
                    i == page ? UIKit.Ink : i < page ? UIKit.Hex("#8E91A8") : UIKit.Hex("#D9D2C5"));
        }

        // ---------------- level mode ----------------

        private void StartLevel(int n)
        {
            level = levels[Mathf.Clamp(n, 1, levels.Length) - 1];
            BuildPlayScreen(Mode.Level, level.ToBoard());
            if (level.n == 1 && !ArrowProgress.TutorialSeen) ShowTutorial();
            else LevelTip();
        }

        // Endless and Daily cost hearts from the first mistake and have no tutorial: a player who never finished
        // level 1's lesson is sent there first.
        private void StartMode(bool daily)
        {
            if (ArrowProgress.TutorialSeen) { StartEndless(daily); return; }
            StartLevel(1);
            Toast.Show(safe, Loc.T("First, a 10-second lesson", "Học nhanh 10 giây trước nhé"));
        }

        private void StartEndless(bool daily)
        {
            level = null;
            var utc = System.DateTime.UtcNow;
            var r = new EndlessRun(daily ? Rng.Seeded(Rng.DailySeed(utc)) : Rng.Unity());
            dailyKey = daily ? ArrowProgress.DailyKeyFor(utc) : null; // A10: the score is filed under the day the run started
            if (daily) ArrowProgress.MarkPlayed(dailyKey);
            BuildPlayScreen(daily ? Mode.Daily : Mode.Endless, r.Board, r);
            var tipKey = daily ? "arrow.tip.daily" : "arrow.tip.endless";
            if (!SaveStore.GetBool(tipKey, false)) // A12: the tip shows once
            {
                SaveStore.SetBool(tipKey, true);
                Tip(daily ? Loc.T("Everyone plays the same board today. Same colour in a row = combo!", "Mọi người cùng một đề hôm nay. Liên tiếp cùng màu = combo!")
                          : Loc.T("Same colour in a row = combo. A x3 combo gives you a free move.", "Liên tiếp cùng màu = combo. Combo x3 được nghỉ 1 lượt."));
            }
        }

        private void BuildPlayScreen(Mode m, Board b, EndlessRun endless = null)
        {
            var s = NewScreen("Play", ScreenKind.Play);
            mode = m;
            board = b;
            run = endless;
            lives = MaxHearts;
            mistakes = 0;
            runBest = 0;
            revived = false;
            runCoins = runCoinsBanked = 0;
            runCoinsDoubled = false;
            hearts.Clear();
            var top = new Vector2(0.5f, 1f);
            var tl = new Vector2(0f, 1f);
            var tr = new Vector2(1f, 1f);
            // Level 1's first run: only Back, the board and the hand (A12)
            var bare = mode == Mode.Level && level.n == 1 && !ArrowProgress.TutorialSeen;

            UIKit.IconButton(s, "round_white", "icon_back", () => { if (mode == Mode.Level) ShowLevelSelect((level.n - 1) / PerPage); else ShowHome(); }, tl, new Vector2(100, -100), 116);
            if (!bare) UIKit.IconButton(s, "round_white", "icon_pause", OpenPause, tr, new Vector2(-100, -100), 116);

            if (mode == Mode.Level)
            {
                bool boss = level.n % 10 == 0;
                scoreText = UIKit.Label(s, Loc.F("Level {0}", "Màn {0}", level.n), 88, top, new Vector2(0, -100), new Vector2(460, 120), boss ? ArrowBoardView.ErrorColor : TextInk);
                scoreText.enableAutoSizing = true;
                scoreText.fontSizeMin = 50;
                scoreText.fontSizeMax = 88;
                stageText = null;
                if (!bare)
                {
                    UIKit.IconButton(s, "round_white", "icon_restart", () => StartLevel(level.n), tr, new Vector2(-240, -100), 116);
                    var hint = UIKit.IconButton(s, "round_yellow", "icon_hint", UseHint, tl, new Vector2(240, -100), 116);
                    hintButton = hint.transform;
                    var badge = UIKit.Image(hint.transform, "round_white", new Vector2(1f, 1f), new Vector2(-12, -12), new Vector2(56, 56));
                    hintBadge = UIKit.Label(badge.transform, "", 34);
                    UIKit.Stretch(hintBadge.rectTransform);
                }
                else { hintBadge = null; hintButton = null; }
                bestText = null;
            }
            else
            {
                scoreText = UIKit.Label(s, "0", 100, top, new Vector2(0, -95), new Vector2(460, 120), TextInk);
                var hint = UIKit.IconButton(s, "round_yellow", "icon_hint", UseHint, tl, new Vector2(240, -100), 116);
                hintButton = hint.transform;
                var badge = UIKit.Image(hint.transform, "round_white", new Vector2(1f, 1f), new Vector2(-12, -12), new Vector2(56, 56));
                hintBadge = UIKit.Label(badge.transform, "", 34);
                UIKit.Stretch(hintBadge.rectTransform);
                runBest = mode == Mode.Daily ? ArrowProgress.DailyBest(dailyKey) : ArrowProgress.EndlessBest;
                runStartBest = runBest;
                stageText = null;
                bestText = UIKit.Label(s, "", 42, top, new Vector2(0, -290), new Vector2(900, 60), TextMuted);
            }

            for (int i = 0; i < MaxHearts; i++)
                hearts.Add(UIKit.Image(s, "icon_heart", top, new Vector2((i - 1) * 86, -215), new Vector2(72, 72)));
            // level: "N arrows left" under the hearts; endless/daily: the colour combo sits right of the hearts
            comboDot = UIKit.Image(s, "dot", top, new Vector2(262, -215), new Vector2(36, 36));
            comboText = mode == Mode.Level
                ? UIKit.Label(s, "", 46, top, new Vector2(0, -290), new Vector2(700, 64), TextInk)
                : UIKit.Label(s, "", 48, top, new Vector2(330, -215), new Vector2(120, 64), TextInk);

            view = ArrowBoardView.Create(s, new Vector2(0, -40), CellSize);
            view.Tapped += OnTapped;
            view.Show(board, fit: mode == Mode.Level);
            // shader themes draw every line on when the board appears
            var look = ArrowSkins.Current;
            if (look != null)
            {
                var line = ArrowSkins.LineMaterial(look);
                if (line.HasProperty("_Reveal")) Tween.Run(this, 0.6f, k => line.SetFloat("_Reveal", k), Ease.OutQuad, 0f, () => line.SetFloat("_Reveal", 1f));
            }

            // the tip line lives in the free band under the board, never over its last row
            tipText = UIKit.Label(s, "", 38, new Vector2(0.5f, 0f), new Vector2(0, 285), new Vector2(940, 100), TextMuted);

            if (mode == Mode.Level && !bare)
            {
                var bottom = new Vector2(0.5f, 0f);
                var prev = UIKit.Button(s, "btn_white", Loc.T("Previous", "Màn trước"), () => StartLevel(level.n - 1), bottom, new Vector2(-250, 140), new Vector2(430, 140), "icon_back", 50);
                var next = UIKit.Button(s, "btn_white", Loc.T("Next", "Màn sau"), () => StartLevel(level.n + 1), bottom, new Vector2(250, 140), new Vector2(430, 140), "icon_next", 50);
                UIKit.SetInteractable(prev, level.n > 1);
                UIKit.SetInteractable(next, level.n < ArrowProgress.Unlocked && level.n < ArrowProgress.LevelCount);
            }
            missStreak = 0;
            freeTaps = 0;
            nudged = false;
            bestToastShown = false;
            RefreshHud();
            state = State.Playing;
        }

        // ---------------- first-time explanations (FTUE) ----------------

        private Transform hintButton;
        private int missStreak, freeTaps, runStartBest;
        private bool bestToastShown;
        private bool nudged;

        // The bulb bounces until it is pressed, so the player's eye finds it.
        private void PulseHint()
        {
            if (hintButton == null) return;
            var t = hintButton;
            Tween.Kill(t);
            Tween.Loop(t, sec => t.localScale = Vector3.one * (1f + 0.12f * Mathf.Abs(Mathf.Sin(sec * 4f))));
        }

        private void StopHintPulse()
        {
            if (hintButton == null) return;
            Tween.Kill(hintButton);
            hintButton.localScale = Vector3.one;
        }

        /// <summary>One tip per level start: hints are introduced on level 2, hearts the first time they start to count.</summary>
        private void LevelTip()
        {
            if (level.n == 2 && !SaveStore.GetBool("arrow.tip.hint", false))
            {
                SaveStore.SetBool("arrow.tip.hint", true);
                Tip(Loc.T("Stuck? The bulb shows an arrow that can go. You get 3.", "Bí? Bóng đèn chỉ ra mũi tên đi được. Bạn có 3 lượt."));
                PulseHint();
            }
            else if (level.n == FreeMistakesUpTo + 1 && !SaveStore.GetBool("arrow.tip.hearts", false))
            {
                SaveStore.SetBool("arrow.tip.hearts", true);
                Tip(Loc.T("From now on a blocked tap costs a heart. 0 hearts = try again.", "Từ giờ chạm sai mất 1 tim. Hết tim là chơi lại."));
                foreach (var h in hearts) Tween.Punch(h.transform, 0.3f, 0.4f);
            }
            else if (level.n <= 3) Tip(Loc.T("Tap an arrow with a clear path to the edge to send it flying.", "Chạm mũi tên có đường thoáng tới mép bàn để nó bay ra."));
        }

        private void OnTapped(Arrow a)
        {
            if (state != State.Playing || lives <= 0) return;
            HideTutorial();
            TapRules(a);
            SaveRun();
        }

        private void TapRules(Arrow a)
        {

            var blocker = board.FirstBlocker(a);
            if (blocker != -1)
            {
                var free = mode == Mode.Level && level.n <= FreeMistakesUpTo;
                view.Lunge(a, board.Arrows[blocker], !free);
                GameAudio.Play("blocked");
                GameAudio.Haptic(HapticLevel.Medium);
                run?.Miss();
                // two misses in a row: point at the bulb once per board (it is there for exactly this)
                if (++missStreak >= 2 && !nudged && hintButton != null)
                {
                    nudged = true;
                    PulseHint();
                    if (!free) Tip(Loc.T("Stuck? Try the bulb.", "Bí? Thử bóng đèn gợi ý."));
                }
                if (free)
                {
                    freeTaps++;
                    Tip(Loc.T("Blocked! Clear the arrow in its way first.", "Bị chặn! Gỡ mũi tên đang cản đường trước."));
                    RefreshHud();
                    return;
                }
                lives--;
                mistakes++;
                RefreshHud();
                Tween.Punch(hearts[Mathf.Clamp(lives, 0, hearts.Count - 1)].transform, 0.35f, 0.3f);
                if (lives <= 0) Lose();
                return;
            }

            missStreak = 0;
            if (view.Hinted == a) view.ClearHint();
            if (mode == Mode.Level)
            {
                board.Remove(a.Id);
                view.AnimateExit(a);
                GameAudio.Play("fly", 1f + Random.Range(-0.04f, 0.06f));
                RefreshHud();
                if (board.Count == 0) Win();
                return;
            }

            var res = run.Exit(a.Id);
            view.AnimateExit(res.Arrow);
            GameAudio.Play("fly", 1f + (res.Combo - 1) * 0.08f);
            if (res.Combo >= 3) GameFx.Play("Sparkle", view.WorldPos(a.Head), 1f + 0.1f * Mathf.Min(res.Combo, 8));
            foreach (var spawned in res.Spawned) view.AddArrow(spawned, true, 0.2f);
            // a new arrow can land in the hinted arrow's path: a hint that now points at a blocked arrow would cost a
            // heart if followed, so it goes away (the next Hint press picks a free arrow again)
            if (view.Hinted != null && board.Arrows.ContainsKey(view.Hinted.Id) && board.FirstBlocker(view.Hinted) != -1) view.ClearHint();
            if (res.StageUp)
            {
                GameAudio.Play("big");
                Tip(Loc.F("Stage {0}! Longer arrows, more colours.", "Lên cấp {0}! Mũi tên dài hơn, nhiều màu hơn.", EndlessRun.DifficultyAt(run.Cleared).Stage + 1));
            }
            var heartBack = run.Cleared % HeartEvery == 0 && lives < MaxHearts;
            if (heartBack) lives++;
            SaveRunBest();
            RefreshHud();
            if (heartBack) // a heart earned back is a moment: it pops in with a chime
            {
                Tween.Punch(hearts[lives - 1].transform, 0.5f, 0.4f);
                GameAudio.Play("star", 1.1f);
            }
            if (res.Combo >= 2) Tween.Punch(comboText.transform, 0.35f, 0.25f);
            if (!bestToastShown && runStartBest > 0 && run.Score > runStartBest)
            {
                bestToastShown = true;
                Toast.Show(safe, Loc.T("New best!", "Kỷ lục mới!"));
                GameAudio.Play("star", 1.25f);
            }
            if (run.Over) Lose();
        }

        // A11: endless / daily bests are kept the moment they are beaten
        private void SaveRunBest()
        {
            if (run == null || run.Score <= runBest) return;
            runBest = run.Score;
            if (mode == Mode.Daily) ArrowProgress.SubmitDaily(dailyKey, run.Score);
            else ArrowProgress.SubmitEndless(run.Score);
        }

        private void RefreshHud()
        {
            for (int i = 0; i < hearts.Count; i++) hearts[i].sprite = ArtLibrary.Instance.Get(i < lives ? "icon_heart" : "icon_heart_empty");
            if (hintBadge != null) hintBadge.text = ArrowProgress.Hints > 0 ? ArrowProgress.Hints.ToString() : "";
            if (hintBadge != null)
            {
                // no hints left: the badge shows the ad icon (tapping asks before any video)
                var empty = ArrowProgress.Hints <= 0;
                if (empty && hintBadge.transform.childCount == 0)
                    UIKit.Image(hintBadge.transform, "icon_ad", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38, 38)).preserveAspect = true;
                if (!empty) foreach (Transform child in hintBadge.transform) Destroy(child.gameObject);
            }

            if (mode == Mode.Level)
            {
                comboDot.enabled = false;
                comboText.text = Loc.F("{0} arrows left", "Còn {0} mũi tên", board.Count);
                comboText.color = TextMuted;
                return;
            }
            scoreText.text = run.Score.ToString();
            var beaten = run.Score > 0 && run.Score >= runBest;
            var stage = EndlessRun.DifficultyAt(run.Cleared).Stage + 1;
            bestText.text = mode == Mode.Daily
                ? Loc.F("Today {0} · best {1}", "Hôm nay {0} · kỷ lục {1}", UtcLabel(), Mathf.Max(runBest, run.Score))
                : Loc.F("Best {0} · Stage {1}", "Kỷ lục {0} · Cấp {1}", Mathf.Max(runBest, run.Score), stage);
            bestText.color = beaten && runBest > 0 ? UIKit.Hex("#E9A23B") : TextMuted;
            var on = run.ComboColor >= 0 && run.Combo >= 2;
            comboDot.enabled = on;
            if (on) comboDot.color = ArrowBoardView.Palette[run.ComboColor];
            comboText.text = on ? $"x{run.Combo}" : "";
            comboText.color = on ? ArrowBoardView.Palette[run.ComboColor] : TextMuted;
        }

        private string UtcLabel() => Loc.Vietnamese
            ? System.DateTime.UtcNow.ToString("d/M", System.Globalization.CultureInfo.InvariantCulture)
            : System.DateTime.UtcNow.ToString("MMM d", System.Globalization.CultureInfo.InvariantCulture);

        private void Tip(string message)
        {
            if (tipText == null) return;
            tipText.text = message;
            tipText.alpha = 0f;
            Tween.Run(tipText, 0.3f, k => tipText.alpha = k);
        }

        // ---------------- hint / tutorial ----------------

        private void UseHint()
        {
            if (state != State.Playing || board.Count == 0) return;
            StopHintPulse();
            // A4: a hint already showing (and still valid) costs nothing
            if (view.Hinted != null && board.Arrows.ContainsKey(view.Hinted.Id) && board.FirstBlocker(view.Hinted) == -1) return;
            if (ArrowProgress.Hints <= 0)
            {
                AskHintAd();
                return;
            }
            ArrowProgress.Hints -= 1;
            GameAudio.Play("hint");
            view.Hint(BestFreeArrow());
            RefreshHud();
        }

        // Rewarded ads are opt-in: the player is told what they get before any video plays (AdMob policy).
        private void AskHintAd()
        {
            state = State.Paused;
            Popup p = null;
            p = Popup.Open(safe, Loc.T("Out of hints", "Hết gợi ý"), 800);
            p.OnBack = () => p.Close(() => state = State.Playing);
            p.Text(Loc.T("Watch a short video to get 1 hint?", "Xem một video ngắn để nhận 1 gợi ý?"), 48, UIKit.Muted, 140);
            p.Space(10);
            RewardedButton.Add(p, Loc.T("+1 hint", "+1 gợi ý"), "arrow_hint", () =>
            {
                ArrowProgress.Hints += 1;
                state = State.Playing;
                UseHint();
            });
            p.Button("btn_white", Loc.T("No thanks", "Không, cảm ơn"), () => p.Close(() => state = State.Playing));
            p.Fit();
        }

        /// <summary>The free arrow whose removal frees the most others: a hint that actually unblocks the board.</summary>
        private Arrow BestFreeArrow()
        {
            board.FreeArrows(freeBuffer);
            var candidates = freeBuffer.ToList();
            var probe = new List<Arrow>();
            return candidates.OrderByDescending(a =>
            {
                board.Remove(a.Id);
                board.FreeArrows(probe);
                board.Place(a);
                return probe.Count;
            }).First();
        }

        private void ShowTutorial()
        {
            var target = BestFreeArrow();
            // fingertip on the middle of the arrow's body, the hand coming from the emptier side
            var mid = (view.CellPos(target.Cells[0]) + view.CellPos(target.Cells[^1])) * 0.5f;
            bool fromLeft = mid.x < view.PlayCentre.x;
            tutorialHand = UIKit.Hand(view.transform, new Vector2(0.5f, 0.5f), mid + new Vector2(fromLeft ? -8f : 8f, -10f), mirror: fromLeft);
            tutorialHand.localScale /= view.Zoom; // the hand keeps its size on a zoomed-in board
            var home = tutorialHand.anchoredPosition;
            var hand = tutorialHand;
            Tween.Loop(hand, s => hand.anchoredPosition = home + new Vector2(0, Mathf.Abs(Mathf.Sin(s * 2f)) * 30f));
            view.Hint(target);
            Tip(Loc.T("Tap an arrow to slide it out.\nClear them all to win.", "Chạm mũi tên để nó trượt ra.\nGỡ hết để thắng."));
        }

        private void HideTutorial()
        {
            if (tutorialHand == null) return;
            Destroy(tutorialHand.gameObject);
            tutorialHand = null;
            ArrowProgress.TutorialSeen = true;
        }

        // ---------------- end states ----------------

        private void Win()
        {
            state = State.Ended;
            ClearRun();
            var stars = mistakes == 0 ? 3 : mistakes <= 2 ? 2 : 1;
            var firstClear = level.n > ArrowProgress.Cleared;
            ArrowProgress.Complete(level.n, stars);
            // SH1: 20 coins per level (raised from 10 to match the other games' pace); a replay pays 2, so replaying level 1 cannot farm the shop
            var coins = firstClear ? 20 : 2;
            Wallet.Add(coins);
            var coinsDoubled = false;
            GameAudio.Play("win");
            GameAudio.Haptic(HapticLevel.Strong);
            GameFx.Play("Arrow_LevelStar", view.transform.position, 1.2f);
            // confetti fills the screen: a big centre burst plus one from each lower corner
            GameFx.Play("Win_Confetti", view.transform.position + Vector3.down * 3f, 2.4f);
            var cam = Camera.main;
            if (cam != null)
                foreach (var x in new[] { 0.08f, 0.92f })
                {
                    var corner = cam.ViewportToWorldPoint(new Vector3(x, 0.12f, -cam.transform.position.z));
                    Tween.Delay(this, 0.12f, () => GameFx.Play("Win_Confetti", corner, 1.8f));
                }
            Tween.Delay(this, 0.5f, () =>
            {
                var last = level.n >= ArrowProgress.LevelCount;
                Popup p = null;
                p = Popup.Open(safe, last ? Loc.T("All clear!", "Phá đảo!") : Loc.F("Level {0} clear!", "Qua màn {0}!", level.n), 980);
                var row = p.Row(170);
                for (int k = 0; k < 3; k++)
                {
                    var star = UIKit.Image(row, k < stars ? "icon_star" : "icon_star_empty", new Vector2(0.5f, 0.5f), new Vector2((k - 1) * 170, k == 1 ? 20 : 0), new Vector2(150, 150));
                    if (k >= stars) continue;
                    star.transform.localScale = Vector3.zero;
                    int idx = k;
                    Tween.Scale(star.transform, Vector3.one, 0.35f, Ease.OutBack, 0.2f + idx * 0.18f, () =>
                    {
                        GameAudio.Play("star", 1f + idx * 0.12f);
                        GameFx.Play("Sparkle", star.transform.position, 1.2f);
                    });
                }
                if (last)
                {
                    p.Text(Loc.T("You cleared all 100 levels!", "Bạn đã qua cả 100 màn!"), 50, UIKit.Ink);
                    p.Space(10);
                    p.Button("btn_green", Loc.T("Play Endless", "Chơi Vô hạn"), () => p.Close(() => { ReviewPrompt.GoodMoment(); StartEndless(false); }), "icon_infinity");
                    p.Button("btn_white", Loc.T("Daily challenge", "Thử thách hôm nay"), () => p.Close(() => { ReviewPrompt.GoodMoment(); StartEndless(true); }), "icon_calendar");
                    p.Button("btn_white", Loc.T("Home", "Về menu"), () => p.Close(ShowHome), "icon_home");
                    p.Fit();
                    return;
                }
                // levels 1–5 don't count blocked taps as mistakes, but calling it "perfect" after several would be a lie
                p.Text(mistakes > 0 ? Loc.F(mistakes == 1 ? "{0} mistake" : "{0} mistakes", "Sai {0} lần", mistakes)
                    : freeTaps > 0 ? Loc.T("Cleared!", "Qua màn!")
                    : Loc.T("Perfect, no mistakes!", "Hoàn hảo, không sai lần nào!"), 48, UIKit.Muted);
                CoinReward.Add(p, coins, coinsDoubled, "arrow_coins", () => coinsDoubled = true, () => p.Close(() => OpenShop(ShowHome)));
                p.Space(10);
                // G10: the review prompt only after the card has closed, never over it
                void Good() { if (stars >= 2) ReviewPrompt.GoodMoment(); }
                p.Button("btn_green", Loc.T("Next level", "Màn tiếp"), () => p.Close(() => { Good(); Ads.OnBreak("arrow_level", () => StartLevel(level.n + 1)); }), "icon_next");
                p.Button("btn_white", Loc.T("Play again", "Chơi lại"), () => p.Close(() => { Good(); StartLevel(level.n); }), "icon_restart");
                p.Button("btn_blue", Loc.T("Levels", "Chọn màn"), () => p.Close(() => { Good(); ShowLevelSelect((level.n - 1) / PerPage); }), "icon_levels");
                p.Fit();
            });
        }

        private void Lose()
        {
            state = State.Ended;
            ClearRun();
            GameAudio.Play("lose");
            GameAudio.Haptic(HapticLevel.Strong);
            view.AllFaces(FaceId.Cry);
            SaveRunBest();
            BankRunCoins();
            Tween.Delay(this, 0.45f, ShowLose);
        }

        private void ShowLose()
        {
            var outOfHearts = lives <= 0;
            var p = Popup.Open(safe, outOfHearts ? Loc.T("Out of hearts", "Hết tim") : Loc.T("Out of moves!", "Hết lượt!"), 1050);
            if (mode == Mode.Level) p.Text(Loc.F("Level {0} · {1} arrows left", "Màn {0} · còn {1} mũi tên", level.n, board.Count), 50, UIKit.Muted);
            else
            {
                p.Text(run.Score.ToString(), 130, UIKit.Ink, 150);
                p.Text(Loc.F("Best {0}", "Kỷ lục {0}", Mathf.Max(runBest, run.Score)), 48, UIKit.Muted);
                CoinReward.Add(p, runCoins, runCoinsDoubled, "arrow_coins", () => runCoinsDoubled = true, () => p.Close(() => OpenShop(ShowHome)));
            }
            p.Space(10);
            if (!revived) RewardedButton.Add(p, outOfHearts ? Loc.T("+1 heart", "+1 tim") : Loc.T("Revive", "Hồi sinh"), "arrow_revive", Revive);
            p.Button("btn_green", mode == Mode.Level ? Loc.T("Restart level", "Chơi lại màn") : Loc.T("Play again", "Chơi lại"), () => p.Close(() => Ads.OnBreak("arrow_gameover", () =>
            {
                if (mode == Mode.Level) StartLevel(level.n);
                else StartEndless(mode == Mode.Daily);
            })), "icon_restart");
            p.Button("btn_white", Loc.T("Menu", "Về menu"), () => p.Close(ShowHome), "icon_home");
            p.Fit();
        }

        // One heart back when out of hearts (A9: an endless revive keeps the hearts you had); in endless also clears
        // a third of the board (removing arrows never creates a deadlock).
        private void Revive()
        {
            revived = true;
            if (lives <= 0) lives = 1;
            if (run != null && run.Over)
            {
                run.Over = false;
                var ids = board.Arrows.Keys.OrderBy(_ => Random.value).Take(Mathf.CeilToInt(board.Count / 3f)).ToList();
                foreach (var id in ids) view.RemoveInstant(board.Remove(id));
            }
            view.AllFaces(FaceId.Stare, 0.01f);
            RefreshHud();
            state = State.Playing;
        }

        // ---------------- skins and coins (shop) ----------------

        private ShopScreen shop;
        private int runCoins, runCoinsBanked;
        private bool runCoinsDoubled;

        private void ApplySkin()
        {
            var palette = Skins.Palette ?? Skins.CatalogOf(ArrowSkins.GameId).Default(0).Colors;
            for (int i = 0; i < ArrowBoardView.Palette.Length; i++) ArrowBoardView.Palette[i] = ArrowSkins.OnPaper(palette[Mathf.Min(i, palette.Length - 1)], ArrowSkins.Paper);
            cam.backgroundColor = Background;
            var look = ArrowSkins.Current;
            ground.material = ArrowSkins.GroundMaterial(look, ArrowSkins.Paper, 140f);
            ground.enabled = look != null;
        }

        private void OpenShop(System.Action onClose) => shop = ShopScreen.Open(safe, new ArrowShopPainter(), onClose);

        // From the pause popup: the run is saved, and rebuilt in the new skin when the shop closes (back to pause).
        private void OpenShopFromPause()
        {
            SaveRun();
            OpenShop(() => { if (!TryRestore()) ShowHome(); });
        }

        private void CoinPill(RectTransform s)
        {
            var pill = UIKit.Image(s, "btn_white", new Vector2(1f, 1f), new Vector2(-206, -108), new Vector2(324, 108));
            UIKit.Image(pill.transform, "icon_coin", new Vector2(0f, 0.5f), new Vector2(62, 2), new Vector2(64, 64)).preserveAspect = true;
            UIKit.Label(pill.transform, Wallet.Format(Wallet.Coins), 52, new Vector2(0.5f, 0.5f), new Vector2(30, 4), new Vector2(220, 90));
        }

        // Endless / daily: 1 coin per cleared arrow, +30 for the first daily run of the day. A revive keeps going, so
        // only what was cleared since the last payout is paid.
        private void BankRunCoins()
        {
            if (run == null) { runCoins = 0; return; }
            var earned = run.Cleared - runCoinsBanked;
            if (mode == Mode.Daily && !SaveStore.GetBool("arrow.coins." + dailyKey, false))
            {
                SaveStore.SetBool("arrow.coins." + dailyKey, true);
                earned += 30;
            }
            runCoinsBanked = run.Cleared;
            runCoins = Mathf.Max(0, earned);
            Wallet.Add(runCoins);
        }

        private void OpenPause()
        {
            if (state != State.Playing) return;
            state = State.Paused;
            SaveRun();
            // AO6: closing the pause only resumes a run that can actually continue
            SettingsPopup.Show(safe, () => { if (state == State.Paused && lives > 0) state = State.Playing; },
                (Loc.T("Shop", "Cửa hàng"), "btn_yellow", "icon_bag", OpenShopFromPause),
                (Loc.T("Play again", "Chơi lại"), "btn_green", "icon_restart", () => { if (mode == Mode.Level) StartLevel(level.n); else StartEndless(mode == Mode.Daily); }),
                (Loc.T("Menu", "Về menu"), "btn_white", "icon_home", ShowHome));
        }
    }
}
