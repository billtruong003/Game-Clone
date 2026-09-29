using System.Collections.Generic;
using System.Linq;
using CasualGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CasualGame.ArrowOut
{
    /// <summary>
    /// Arrow Out: a calm brain puzzle. Screens: Home → Level select (100 baked levels) → Level (back / prev / next,
    /// hint, restart) plus Endless and Daily. All UI is built from the shared UI sheet.
    /// </summary>
    public class ArrowOutGame : MonoBehaviour
    {
        private static readonly Color Background = UIKit.Hex("#F5F1EA");
        private const float CellSize = 136f;
        private const int MaxHearts = 3;
        private const int PerPage = 20;
        private const int HeartEvery = 40;

        [SerializeField] private TextAsset levelsJson; // wired by the Build Switcher

        private Canvas canvas;
        private RectTransform safe;
        private RectTransform screen;
        private LevelData[] levels;

        // current play session
        private enum Mode { Level, Endless, Daily }
        private Mode mode;
        private LevelData level;
        private Board board;
        private EndlessRun run;
        private ArrowBoardView view;
        private int lives, mistakes;
        private bool playing, revived;
        private readonly List<Image> hearts = new();
        private TextMeshProUGUI scoreText, stageText, comboText, hintBadge, tipText;
        private Image comboDot;
        private RectTransform tutorialHand;
        private readonly List<Arrow> freeBuffer = new();

        private void Start()
        {
            var cam = Camera.main;
            cam.backgroundColor = Background;
            cam.clearFlags = CameraClearFlags.SolidColor;
            canvas = UIKit.CreateCameraCanvas("ArrowOutUI", cam);
            safe = UIKit.Stretch(UIKit.Rect("Safe", canvas.transform));
            safe.gameObject.AddComponent<SafeArea>();
            levels = JsonUtility.FromJson<LevelPack>(levelsJson.text).levels;
            ShowHome();
            GameAudio.PlayMusic("music_arrow");
        }

        // ---------------- screens ----------------

        private RectTransform NewScreen(string name)
        {
            if (screen != null) Destroy(screen.gameObject);
            Tween.Kill(this);
            screen = UIKit.Stretch(UIKit.Rect(name, safe));
            var group = screen.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            Tween.Fade(group, 1f, 0.2f);
            playing = false;
            return screen;
        }

        private void ShowHome()
        {
            var s = NewScreen("Home");
            var top = new Vector2(0.5f, 1f);

            // Title with a little snake doodle built from the real tiles.
            var doodle = UIKit.Rect("Doodle", s);
            UIKit.Place(doodle, top, new Vector2(0, -330), new Vector2(600, 150));
            var sample = new Arrow { Cells = new[] { new Pos(0, 3), new Pos(0, 2), new Pos(1, 2), new Pos(1, 1), new Pos(1, 0) }, Dir = 1, Color = 1 };
            for (int i = 0; i < sample.Cells.Length; i++)
            {
                var (tile, angle) = ArrowBoardView.TileFor(sample.Cells, i, sample.Dir);
                var img = UIKit.Image(doodle, tile, new Vector2(0.5f, 0.5f), new Vector2((sample.Cells[i].C - 1.5f) * 75, -(sample.Cells[i].R - 0.5f) * 75), new Vector2(75, 75), ArrowBoardView.Palette[i % 4 == 0 ? 1 : 1]);
                img.rectTransform.localEulerAngles = new Vector3(0, 0, -angle);
            }
            UIKit.Label(s, "ARROW OUT", 150, top, new Vector2(0, -520), new Vector2(1000, 180));
            UIKit.Label(s, Loc.T("Brain puzzle · clear the arrows", "Trò chơi trí tuệ · gỡ mũi tên"), 50, top, new Vector2(0, -625), new Vector2(1000, 70), UIKit.Muted);

            var mid = new Vector2(0.5f, 0.5f);
            var next = Mathf.Min(ArrowProgress.Unlocked, ArrowProgress.LevelCount);
            UIKit.Button(s, "btn_green", Loc.F("Play · Level {0}", "Chơi · Màn {0}", next), () => StartLevel(next), mid, new Vector2(0, 120), new Vector2(720, 170), "icon_play", 64);
            UIKit.Button(s, "btn_blue", Loc.T("Levels", "Chọn màn"), () => ShowLevelSelect((next - 1) / PerPage), mid, new Vector2(0, -80), new Vector2(720, 150), "icon_levels");
            UIKit.Button(s, "btn_yellow", Loc.T("Endless mode", "Chế độ vô hạn"), () => StartEndless(false), mid, new Vector2(0, -260), new Vector2(720, 150), "icon_infinity");
            UIKit.Button(s, "btn_white", Loc.T("Daily challenge", "Thử thách hôm nay"), () => StartEndless(true), mid, new Vector2(0, -440), new Vector2(720, 150), "icon_calendar");

            var bottom = new Vector2(0.5f, 0f);
            UIKit.IconButton(s, "round_white", "icon_settings", () => SettingsPopup.Show(safe, null), bottom, new Vector2(-200, 150));
            UIKit.IconButton(s, "round_yellow", "icon_trophy", ShowRecords, bottom, new Vector2(0, 150));
            if (!Ads.RemoveAdsOwned)
                UIKit.IconButton(s, "round_white", "icon_noads", () => Store.BuyRemoveAds(_ => ShowHome()), bottom, new Vector2(200, 150));
        }

        private void ShowRecords()
        {
            Popup p = null;
            p = Popup.Open(safe, Loc.T("Stats", "Thành tích"), 900);
            p.Text(Loc.F("Cleared: {0}/{1} levels", "Đã qua: {0}/{1} màn", ArrowProgress.Cleared, ArrowProgress.LevelCount), 54);
            p.Text(Loc.F("Stars: {0}/{1}", "Tổng sao: {0}/{1}", ArrowProgress.TotalStars, ArrowProgress.LevelCount * 3), 54);
            p.Text(Loc.F("Endless best: {0}", "Kỷ lục vô hạn: {0}", ArrowProgress.EndlessBest), 54);
            p.Text(Loc.F("Today: {0}", "Hôm nay: {0}", ArrowProgress.DailyBest), 54);
            p.Space(20);
            p.Button("btn_green", Loc.T("Close", "Đóng"), () => p.Close());
            p.Fit();
        }

        private void ShowLevelSelect(int page)
        {
            var pages = Mathf.CeilToInt(ArrowProgress.LevelCount / (float)PerPage);
            page = Mathf.Clamp(page, 0, pages - 1);
            var s = NewScreen("Levels");
            var top = new Vector2(0.5f, 1f);
            UIKit.IconButton(s, "round_white", "icon_back", ShowHome, new Vector2(0f, 1f), new Vector2(100, -100), 116);
            UIKit.Label(s, Loc.T("Levels", "Chọn màn"), 84, top, new Vector2(0, -100), new Vector2(600, 110));
            UIKit.Image(s, "icon_star", top, new Vector2(-95, -185), new Vector2(64, 64));
            UIKit.Label(s, $"{ArrowProgress.TotalStars}/{ArrowProgress.LevelCount * 3}", 50, top, new Vector2(40, -185), new Vector2(240, 70), UIKit.Hex("#E9A23B"));

            var grid = UIKit.Place(UIKit.Rect("Grid", s), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(920, 1160));
            var unlocked = ArrowProgress.Unlocked;
            for (int i = 0; i < PerPage; i++)
            {
                var n = page * PerPage + i + 1;
                if (n > ArrowProgress.LevelCount) break;
                int col = i % 4, row = i / 4;
                var pos = new Vector2((col - 1.5f) * 225, (2 - row) * 228);
                bool locked = n > unlocked, boss = n % 10 == 0, current = n == unlocked;
                var sprite = locked ? "tile_locked" : current ? "tile_current" : boss ? "tile_boss" : "tile_open";
                var tile = UIKit.Button(grid, sprite, "", () => StartLevel(n), new Vector2(0.5f, 0.5f), pos, new Vector2(200, 200));
                tile.interactable = !locked;
                if (locked)
                {
                    UIKit.Image(tile.transform, "icon_lock", new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(70, 70), new Color(1, 1, 1, 0.5f)).color = new Color(0.12f, 0.13f, 0.25f, 0.45f);
                    continue;
                }
                UIKit.Label(tile.transform, n.ToString(), 76, new Vector2(0.5f, 0.5f), new Vector2(0, 18), new Vector2(180, 100));
                var stars = ArrowProgress.Stars(n);
                if (n <= ArrowProgress.Cleared)
                    for (int k = 0; k < 3; k++)
                    UIKit.Image(tile.transform, k < stars ? "icon_star" : "icon_star_empty", new Vector2(0.5f, 0f), new Vector2((k - 1) * 46, 40), new Vector2(48, 48));
                if (boss) UIKit.Label(tile.transform, "BOSS", 28, new Vector2(0.5f, 1f), new Vector2(0, -38), new Vector2(180, 36), ArrowBoardView.ErrorColor);
            }

            var bottom = new Vector2(0.5f, 0f);
            var prev = UIKit.IconButton(s, "round_blue", "icon_back", () => ShowLevelSelect(page - 1), bottom, new Vector2(-260, 150));
            var nextBtn = UIKit.IconButton(s, "round_blue", "icon_next", () => ShowLevelSelect(page + 1), bottom, new Vector2(260, 150));
            UIKit.SetInteractable(prev, page > 0);
            UIKit.SetInteractable(nextBtn, page < pages - 1);
            UIKit.Label(s, $"{page * PerPage + 1}–{Mathf.Min((page + 1) * PerPage, ArrowProgress.LevelCount)}", 56, bottom, new Vector2(0, 150), new Vector2(360, 90));
        }

        // ---------------- level mode ----------------

        private void StartLevel(int n)
        {
            mode = Mode.Level;
            level = levels[Mathf.Clamp(n, 1, levels.Length) - 1];
            board = level.ToBoard();
            run = null;
            BuildPlayScreen();

            if (level.n == 1 && !ArrowProgress.TutorialSeen) ShowTutorial();
            else if (level.n <= 3) Tip(Loc.T("Tap an arrow with a clear path to the edge to send it flying.", "Chạm mũi tên có đường thoáng tới mép bàn để nó bay ra."));
        }

        private void StartEndless(bool daily)
        {
            mode = daily ? Mode.Daily : Mode.Endless;
            level = null;
            run = new EndlessRun(daily ? Rng.Seeded(Rng.DailySeed(System.DateTime.Now)) : Rng.Unity());
            board = run.Board;
            BuildPlayScreen();
            Tip(daily ? Loc.T("Everyone plays the same board today. Chain arrows of one color to multiply your score!", "Mọi người cùng một đề hôm nay. Bay liên tiếp cùng màu để nhân điểm!") : Loc.T("Chain arrows of one color to multiply your score. A x3 combo gives you a free move.", "Bay liên tiếp cùng màu để nhân điểm. Combo x3 được nghỉ 1 lượt."));
        }

        private void BuildPlayScreen()
        {
            var s = NewScreen("Play");
            lives = MaxHearts;
            mistakes = 0;
            revived = false;
            hearts.Clear();
            var top = new Vector2(0.5f, 1f);
            var tl = new Vector2(0f, 1f);
            var tr = new Vector2(1f, 1f);

            UIKit.IconButton(s, "round_white", "icon_back", () => { if (mode == Mode.Level) ShowLevelSelect((level.n - 1) / PerPage); else ShowHome(); }, tl, new Vector2(100, -100), 116);
            UIKit.IconButton(s, "round_white", "icon_settings", OpenPause, tr, new Vector2(-100, -100), 116);

            if (mode == Mode.Level)
            {
                bool boss = level.n % 10 == 0;
                scoreText = UIKit.Label(s, Loc.F("Level {0}", "Màn {0}", level.n), 92, top, new Vector2(0, -100), new Vector2(600, 120), boss ? ArrowBoardView.ErrorColor : UIKit.Ink);
                stageText = UIKit.Label(s, boss ? "BOSS" : "", 40, top, new Vector2(0, -175), new Vector2(400, 60), ArrowBoardView.ErrorColor);
                UIKit.IconButton(s, "round_white", "icon_restart", () => StartLevel(level.n), tr, new Vector2(-240, -100), 116);
                var hint = UIKit.IconButton(s, "round_yellow", "icon_hint", UseHint, tl, new Vector2(240, -100), 116);
                var badge = UIKit.Image(hint.transform, "round_white", new Vector2(1f, 1f), new Vector2(-12, -12), new Vector2(56, 56));
                hintBadge = UIKit.Label(badge.transform, "", 34);
                UIKit.Stretch(hintBadge.rectTransform);
            }
            else
            {
                scoreText = UIKit.Label(s, "0", 110, top, new Vector2(0, -100), new Vector2(600, 130));
                var best = mode == Mode.Daily ? ArrowProgress.DailyBest : ArrowProgress.EndlessBest;
                stageText = UIKit.Label(s, "", 40, top, new Vector2(0, -180), new Vector2(700, 60), UIKit.Muted);
                UIKit.Label(s, (mode == Mode.Daily ? Loc.T("Today", "Hôm nay") : Loc.T("Best", "Kỷ lục")) + " " + best, 40, tr, new Vector2(-240, -100), new Vector2(220, 60), UIKit.Muted);
                hintBadge = null;
            }

            for (int i = 0; i < MaxHearts; i++)
                hearts.Add(UIKit.Image(s, "icon_heart", top, new Vector2((i - 1) * 86, -250), new Vector2(76, 76)));
            comboDot = UIKit.Image(s, "dot", top, new Vector2(-120, -330), new Vector2(46, 46));
            comboText = UIKit.Label(s, "", 50, top, new Vector2(40, -330), new Vector2(360, 70));

            view = ArrowBoardView.Create(s, new Vector2(0, -30), CellSize);
            view.Tapped += OnTapped;
            view.Show(board);

            tipText = UIKit.Label(s, "", 40, new Vector2(0.5f, 0f), new Vector2(0, 300), new Vector2(900, 110), UIKit.Muted);

            if (mode == Mode.Level)
            {
                var bottom = new Vector2(0.5f, 0f);
                var prev = UIKit.Button(s, "btn_white", Loc.T("Previous", "Màn trước"), () => StartLevel(level.n - 1), bottom, new Vector2(-250, 140), new Vector2(430, 140), "icon_back", 50);
                var next = UIKit.Button(s, "btn_white", Loc.T("Next", "Màn sau"), () => StartLevel(level.n + 1), bottom, new Vector2(250, 140), new Vector2(430, 140), "icon_next", 50);
                UIKit.SetInteractable(prev, level.n > 1);
                UIKit.SetInteractable(next, level.n < ArrowProgress.Unlocked && level.n < ArrowProgress.LevelCount);
            }
            RefreshHud();
            playing = true;
        }

        private void OnTapped(Arrow a)
        {
            if (!playing) return;
            HideTutorial();
            view.ClearHint();

            if (!board.IsFree(a))
            {
                lives--;
                mistakes++;
                run?.Miss();
                view.Shake(a);
                GameAudio.Play("blocked");
                GameAudio.Haptic();
                if (mistakes == 1 && mode == Mode.Level && level.n <= 10) Tip(Loc.T("That arrow is blocked! Clear the one in its way first.", "Mũi tên đó bị chặn! Gỡ mũi tên đang cản đường trước."));
                RefreshHud();
                if (lives <= 0) Lose();
                return;
            }

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
            if (res.StageUp)
            {
                GameAudio.Play("big");
                Tip(Loc.F("Stage {0}! Longer arrows, more colors.", "Lên cấp {0}! Mũi tên dài hơn, nhiều màu hơn.", EndlessRun.DifficultyAt(run.Cleared).Stage + 1));
            }
            if (run.Cleared % HeartEvery == 0 && lives < MaxHearts) lives++;
            RefreshHud();
            if (run.Over) Lose();
        }

        private void RefreshHud()
        {
            for (int i = 0; i < hearts.Count; i++) hearts[i].sprite = ArtLibrary.Instance.Get(i < lives ? "icon_heart" : "icon_heart_empty");
            if (hintBadge != null) hintBadge.text = ArrowProgress.Hints > 0 ? ArrowProgress.Hints.ToString() : "+";

            if (mode == Mode.Level)
            {
                comboDot.enabled = false;
                comboText.text = Loc.F("{0} arrows left", "Còn {0} mũi tên", board.Count);
                comboText.color = UIKit.Muted;
                comboText.rectTransform.anchoredPosition = new Vector2(0, -330);
                return;
            }
            scoreText.text = run.Score.ToString();
            stageText.text = Loc.F("Stage {0} · cleared {1}", "Cấp {0} · đã gỡ {1}", EndlessRun.DifficultyAt(run.Cleared).Stage + 1, run.Cleared);
            var on = run.ComboColor >= 0 && run.Combo >= 1;
            comboDot.enabled = on;
            if (on) comboDot.color = ArrowBoardView.Palette[run.ComboColor];
            comboText.text = on ? $"Combo x{run.Combo}" : "";
            comboText.color = on ? ArrowBoardView.Palette[run.ComboColor] : UIKit.Muted;
        }

        private void Tip(string message)
        {
            tipText.text = message;
            tipText.alpha = 0f;
            Tween.Run(tipText, 0.3f, k => tipText.alpha = k);
        }

        // ---------------- hint / tutorial ----------------

        private void UseHint()
        {
            if (!playing || board.Count == 0) return;
            if (ArrowProgress.Hints <= 0)
            {
                Ads.ShowRewarded("arrow_hint", ok => { if (ok) { ArrowProgress.Hints += 1; UseHint(); } });
                return;
            }
            ArrowProgress.Hints -= 1;
            GameAudio.Play("hint");
            view.Hint(BestFreeArrow());
            RefreshHud();
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
            tutorialHand = (RectTransform)UIKit.Image(view.transform, "hand", new Vector2(0.5f, 0.5f), view.CellPos(target.Head) + new Vector2(40, -80), new Vector2(170, 170)).transform;
            var home = tutorialHand.anchoredPosition;
            Tween.Run(tutorialHand, 30f, k => tutorialHand.anchoredPosition = home + new Vector2(0, Mathf.Abs(Mathf.Sin(k * 60f)) * 30f), Ease.Linear);
            view.Hint(target);
            Tip(Loc.T("Tap an arrow with a clear path to the edge of the board to send it flying.", "Chạm vào mũi tên có đường thoáng tới mép bàn cờ để nó bay ra."));
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
            playing = false;
            var stars = mistakes == 0 ? 3 : mistakes <= 2 ? 2 : 1;
            ArrowProgress.Complete(level.n, stars);
            if (stars >= 2) ReviewPrompt.GoodMoment();
            GameAudio.Play("win");
            GameFx.Play("Arrow_LevelStar", view.transform.position, 1.2f);
            GameFx.Play("Win_Confetti", view.transform.position + Vector3.down * 3f, 1f);
            Tween.Delay(this, 0.5f, () =>
            {
                var last = level.n >= ArrowProgress.LevelCount;
                Popup p = null;
                p = Popup.Open(safe, last ? Loc.T("All clear!", "Phá đảo!") : Loc.T("Level clear!", "Hoàn thành!"), 980);
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
                p.Text(mistakes == 0 ? Loc.T("Perfect, no mistakes!", "Hoàn hảo, không sai lần nào!") : Loc.F("{0} mistakes", "Sai {0} lần", mistakes), 48, UIKit.Muted);
                p.Space(10);
                if (!last)
                    p.Button("btn_green", Loc.T("Next level", "Màn tiếp"), () => p.Close(() => Ads.OnBreak("arrow_level", () => StartLevel(level.n + 1))), "icon_next");
                p.Button("btn_white", Loc.T("Play again", "Chơi lại"), () => p.Close(() => StartLevel(level.n)), "icon_restart");
                p.Button("btn_blue", Loc.T("Levels", "Chọn màn"), () => p.Close(() => ShowLevelSelect((level.n - 1) / PerPage)), "icon_levels");
                p.Fit();
            });
        }

        private void Lose()
        {
            playing = false;
            GameAudio.Play("lose");
            int best = 0;
            if (mode == Mode.Endless) best = ArrowProgress.SubmitEndless(run.Score);
            if (mode == Mode.Daily) best = ArrowProgress.SubmitDaily(run.Score);

            Tween.Delay(this, 0.45f, () =>
            {
                Popup p = null;
                p = Popup.Open(safe, mode == Mode.Level ? Loc.T("Out of hearts!", "Hết tim!") : Loc.T("Out of moves!", "Hết lượt!"), 1050);
                if (mode == Mode.Level) p.Text(Loc.F("Level {0} · {1} arrows left", "Màn {0} · còn {1} mũi tên", level.n, board.Count), 50, UIKit.Muted);
                else
                {
                    p.Text(run.Score.ToString(), 130, UIKit.Ink, 150);
                    p.Text(Loc.F("Best {0}", "Kỷ lục {0}", best), 48, UIKit.Muted);
                }
                p.Space(10);
                if (!revived)
                    p.Button("btn_blue", mode == Mode.Level ? Loc.T("+1 heart", "+1 tim") : Loc.T("Revive", "Hồi sinh"), () => p.Close(() => Ads.ShowRewarded("arrow_revive", ok => { if (ok) Revive(); })), "icon_ad");
                p.Button("btn_green", Loc.T("Play again", "Chơi lại"), () => p.Close(() => Ads.OnBreak("arrow_gameover", () =>
                {
                    if (mode == Mode.Level) StartLevel(level.n);
                    else StartEndless(mode == Mode.Daily);
                })), "icon_restart");
                p.Button("btn_white", Loc.T("Menu", "Về menu"), () => p.Close(ShowHome), "icon_home");
                p.Fit();
            });
        }

        // One heart back; in endless also clears a third of the board (removing arrows never creates a deadlock).
        private void Revive()
        {
            revived = true;
            lives = 1;
            if (run != null)
            {
                run.Over = false;
                var ids = board.Arrows.Keys.OrderBy(_ => Random.value).Take(Mathf.CeilToInt(board.Count / 3f)).ToList();
                foreach (var id in ids) view.RemoveInstant(board.Remove(id));
            }
            RefreshHud();
            playing = true;
        }

        private void OpenPause()
        {
            playing = false;
            SettingsPopup.Show(safe, () => playing = true,
                (Loc.T("Play again", "Chơi lại"), "btn_green", "icon_restart", () => { if (mode == Mode.Level) StartLevel(level.n); else StartEndless(mode == Mode.Daily); }),
                (Loc.T("Menu", "Về menu"), "btn_white", "icon_home", ShowHome));
        }
    }
}
