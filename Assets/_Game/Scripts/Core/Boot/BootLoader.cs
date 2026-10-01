using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CasualGame.Core
{
    /// <summary>
    /// The Boot scene, shared by the three games (wired per game by the Build Switcher):
    /// 1. Bill The Dev credit (~4.8 s, long enough to remember the brand): the fox head pops in, its sunglasses drop
    ///    on, a glare sweeps, the name types out. No "Made with Unity" (optional since Unity 6).
    /// 2. The game's splash art with a loading bar that is the game in miniature.
    /// The real loading (<see cref="LoadPipeline"/>) starts on the first frame and runs under both, so the credit
    /// costs no time. When everything is loaded the game scene activates under the splash, draws its first frame, and
    /// the splash fades away. The first launch of the day always plays the full credit; on later launches a tap (after
    /// the first second) skips it.
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        [SerializeField] private string nextScene;
        [SerializeField] private GameConfig config;
        [SerializeField] private AudioLibrary audioLibrary;
        [SerializeField] private Sprite foxHead, foxShades, foxShadesMask;

        // time on the splash once it is up (the art has to be seen), the intro before the bar moves, the final fade
        private const float MinSplash = 2.8f, IntroTime = 0.9f, FadeOut = 0.5f;
        // how fast the bar may climb (fraction per second): slow enough to watch the little game run it
        private const float BarSpeed = 0.7f;
        private static readonly Color Night = UIKit.Hex("#0B0B0F"), Amber = UIKit.Hex("#FFB84D");

        private readonly LoadPipeline pipeline = new();
        private AsyncOperation sceneOp;
        private CanvasGroup rootGroup;
        private RectTransform root;
        private bool skipCredits, stillFrame;
        private float speed = 1f; // scales the credit's beats (1 = the full 4.8 s)
        private bool repeatLaunch; // opened before today: the credit may be skipped

        private void Start()
        {
            pipeline.Add("scene", 30, BootJobs.Scene(nextScene, op => sceneOp = op));
            pipeline.Add("save", 5, BootJobs.Save);
            pipeline.Add("audio", 10, BootJobs.Audio(audioLibrary));
            pipeline.Add("shaders", 15, BootJobs.Shaders(() => stillFrame));
            pipeline.Add("fonts", 5, BootJobs.Fonts);
            // with the shop: pipeline.Add("skins", 20, …) and pipeline.Add("store", 10, …)
            pipeline.Start(this);
            bootStarted = Time.realtimeSinceStartup;

            var today = System.DateTime.Now.ToString("yyyy-MM-dd");
            repeatLaunch = SaveStore.GetString("boot.day") == today;
            SaveStore.SetString("boot.day", today);

            // the loader and its canvas outlive the Boot scene: they fade out over the game after it has taken over
            DontDestroyOnLoad(gameObject);
            var canvas = UIKit.CreateOverlayCanvas("Boot", 5000);
            DontDestroyOnLoad(canvas.gameObject);
            rootGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            root = UIKit.Stretch(UIKit.Rect("Root", canvas.transform));
            UIKit.AddImage(root, (Sprite)null, Night).raycastTarget = true; // swallows taps until the game is shown
            StartCoroutine(Run());
        }

        // Tap to skip only on later launches of the day, and not in the first second: the first launch always shows the
        // whole credit (the brand has to be seen), and the tap that opened the app (or the Play click in the editor)
        // must not count as a skip.
        private const float SkipAfter = 1f;
        private float bootStarted;

        private void Update()
        {
#if UNITY_EDITOR
            return; // in the editor the Play click and window focus would skip it
#else
            if (!repeatLaunch || Time.realtimeSinceStartup - bootStarted < SkipAfter) return;
#endif
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame) skipCredits = true;
        }

        private IEnumerator Run()
        {
            var t0 = Time.realtimeSinceStartup;
            yield return Credit();
            Mark("credit", t0);
            stillFrame = true; // skipped credits: warm up now rather than never
            yield return SplashAndLoad();
            Mark("game shown", t0);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void Mark(string phase, float t0) =>
            Debug.Log($"[Boot] {phase} at {Time.realtimeSinceStartup - t0:F2} s (load {pipeline.Progress:P0}{(pipeline.Done ? ", done" : "")})");

        // Waits unscaled time, cut short by a tap during the credits.
        private IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds * speed && !skipCredits; t += Time.unscaledDeltaTime) yield return null;
        }

        // ---------------- 1. Bill The Dev ----------------

        private IEnumerator Credit()
        {
            var group = UIKit.Stretch(UIKit.Rect("Credit", root)).gameObject.AddComponent<CanvasGroup>();
            var mark = UIKit.Place(UIKit.Rect("Fox", group.transform), new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(460, 460));
            var head = UIKit.AddImage(UIKit.Stretch(UIKit.Rect("Head", mark)), foxHead);
            var shades = UIKit.AddImage(UIKit.Stretch(UIKit.Rect("Shades", mark)), foxShades);
            // the glare sweep: a white bar moving across, masked to the lens shape
            var lens = UIKit.AddImage(UIKit.Stretch(UIKit.Rect("Lens", mark)), foxShadesMask);
            lens.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var glare = UIKit.Image(lens.transform, null, new Vector2(0.5f, 0.5f), new Vector2(-420, 0), new Vector2(60, 600), new Color(1f, 1f, 1f, 0.75f));
            glare.rectTransform.localEulerAngles = new Vector3(0, 0, -24f);
            var wordmark = UIKit.Label(group.transform, "", 76, new Vector2(0.5f, 0.5f), new Vector2(0, -190), new Vector2(900, 110), new Color(0.96f, 0.96f, 0.96f));
            wordmark.fontStyle = FontStyles.Bold;
            wordmark.richText = true;

            mark.localScale = Vector3.zero;
            shades.enabled = false;
            yield return Wait(0.3f);

            // the bare fox pops in, then sits there a beat so the drop reads
            Tween.Run(mark, 0.5f * speed, k => mark.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, k), Ease.OutBack, 0f, null, true);
            yield return Wait(0.9f);

            // the sunglasses drop onto the face; the head squashes a little on impact
            shades.enabled = true;
            var shadesRt = shades.rectTransform;
            Tween.Run(shadesRt, 0.5f * speed, k => shadesRt.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(320f, 0f, k)), Ease.OutBounce, 0f, null, true);
            yield return Wait(0.3f);
            GameAudio.Play("ui_click");
            Tween.Run(mark, 0.24f, k => mark.localScale = new Vector3(1f + 0.07f * Mathf.Sin(k * Mathf.PI), 1f - 0.07f * Mathf.Sin(k * Mathf.PI), 1f), Ease.Linear, 0f,
                () => mark.localScale = Vector3.one, true);
            yield return Wait(0.5f);

            // glare sweep + the name types out with a blinking cursor
            var glareRt = glare.rectTransform;
            Tween.Run(glareRt, 0.7f * speed, k => glareRt.anchoredPosition = new Vector2(Mathf.Lerp(-420f, 420f, k), 0f), Ease.InOutSine, 0f, null, true);
            const string full = "Bill The Dev";
            for (int i = 1; i <= full.Length && !skipCredits; i++)
            {
                wordmark.text = full.Substring(0, i) + "<color=#FFB84D>_</color>";
                for (float t = 0f; t < 0.06f * speed; t += Time.unscaledDeltaTime) yield return null;
            }
            wordmark.text = full + "<color=#FFB84D>_</color>";
            for (int blink = 0; blink < 2 && !skipCredits; blink++)
            {
                yield return Wait(0.25f);
                wordmark.text = full;
                yield return Wait(0.25f);
                wordmark.text = full + "<color=#FFB84D>_</color>";
            }

            stillFrame = true; // the credit is just sitting there: the shader warm-up may hitch now
            yield return Wait(0.5f);
            yield return FadeAway(group, group.transform, 0.5f);
        }

        private IEnumerator FadeAway(CanvasGroup group, Transform shrink, float seconds)
        {
            seconds *= skipCredits ? 0.5f : speed;
            var from = group.alpha;
            Tween.Run(group, seconds, k =>
            {
                group.alpha = Mathf.Lerp(from, 0f, k);
                if (shrink != null) shrink.localScale = Vector3.one * Mathf.Lerp(1f, 0.96f, k);
            }, Ease.OutQuad, 0f, null, true);
            yield return new WaitForSecondsRealtime(seconds);
            Destroy(group.gameObject);
        }

        // ---------------- 2. splash art + loading ----------------

        private IEnumerator SplashAndLoad()
        {
            var ground = config != null ? config.splashColor : UIKit.Hex("#2B2F55");
            var light = 0.2126f * ground.r + 0.7152f * ground.g + 0.0722f * ground.b > 0.5f;
            var splash = UIKit.Stretch(UIKit.Rect("Splash", root));

            // the game's colour wipes up from the bottom
            var bg = UIKit.Stretch(UIKit.Rect("Ground", splash));
            UIKit.AddImage(bg, (Sprite)null, ground);
            bg.pivot = new Vector2(0.5f, 0f);
            bg.localScale = new Vector3(1f, 0f, 1f);
            Tween.Run(bg, 0.5f, k => bg.localScale = new Vector3(1f, k, 1f), Ease.OutCubic, 0f, null, true);
            if (config != null && !string.IsNullOrEmpty(config.music)) GameAudio.PlayMusic(config.music);
            yield return new WaitForSecondsRealtime(0.3f);
            var started = Time.unscaledTime;

            // logo drops in, then the characters pop up
            RectTransform logo = null;
            if (config != null && config.logo != null)
            {
                var img = UIKit.Image(splash, null, new Vector2(0.5f, 0.5f), new Vector2(0, 600), new Vector2(940, 352));
                img.sprite = config.logo;
                img.preserveAspect = true;
                logo = img.rectTransform;
                Tween.Run(logo, 0.55f, k => logo.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(900f, 600f, k)), Ease.OutBack, 0f, null, true);
            }
            RectTransform art = null;
            if (config != null && config.splashArt != null)
            {
                art = UIKit.Place(UIKit.Rect("Art", splash), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(980, 980));
                art.gameObject.AddComponent<RawImage>().texture = config.splashArt;
                art.localScale = Vector3.zero;
                Tween.Run(art, 0.5f, k => art.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, k), Ease.OutBack, 0.3f, null, true);
            }

            var bar = new BootBar(splash, config != null ? config.gameId : "", ground);
            bar.Root.anchoredPosition = new Vector2(0f, -560f);
            var tip = UIKit.Label(splash, "", 40, new Vector2(0.5f, 0.5f), new Vector2(0, -650), new Vector2(940, 70),
                light ? UIKit.Hex("#3B4166") : new Color(1f, 1f, 1f, 0.82f));
            tip.fontStyle = FontStyles.Bold;
            var tips = Tips(config != null ? config.gameId : "");

            // the bar follows the real progress, only ever forward, eased so it never jumps
            float shown = 0f, tipClock = 0f;
            int tipIndex = 0;
            tip.text = tips[0];
            while (!(pipeline.Done && shown >= 0.999f && Time.unscaledTime - started >= MinSplash))
            {
                // the bar waits for the logo and the characters to land, so the art is never late to its own loading
                var target = Time.unscaledTime - started < IntroTime ? 0f : pipeline.Progress;
                shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * BarSpeed);
                bar.Set(shown);
                if (art != null) art.anchoredPosition = new Vector2(0f, 40f + 8f * Mathf.Sin(Time.unscaledTime * 3f)); // idle bob
                tipClock += Time.unscaledDeltaTime;
                if (tipClock > 0.8f && tipIndex < tips.Length - 2)
                {
                    tipClock = 0f;
                    tip.text = tips[++tipIndex];
                }
                yield return null;
            }
            bar.Set(1f);
            tip.text = tips[^1];
            yield return bar.Payoff();

            // the game takes over underneath, draws its first frames, then the splash fades away
            if (sceneOp != null)
            {
                sceneOp.allowSceneActivation = true;
                while (!sceneOp.isDone) yield return null;
            }
            else if (!string.IsNullOrEmpty(nextScene))
            {
                SceneManager.LoadScene(nextScene);
            }
            yield return null;
            yield return null;
            var raycast = root.GetComponent<Image>();
            if (raycast != null) raycast.raycastTarget = false;
            Tween.Fade(rootGroup, 0f, FadeOut);
            yield return new WaitForSecondsRealtime(FadeOut + 0.05f);
            Destroy(rootGroup.gameObject);
            Destroy(gameObject);
        }

        private static string[] Tips(string gameId) => gameId switch
        {
            "ArrowOut" => new[] { Loc.T("Untangling arrows…", "Đang gỡ rối mũi tên…"), Loc.T("Teaching arrows to yeet…", "Đang dạy mũi tên bay…"), Loc.T("Pointing in every direction…", "Chỉ lung tung mọi hướng…"), "Bruh." },
            "EyeBlast" => new[] { Loc.T("Stacking attitude…", "Đang xếp thái độ…"), Loc.T("Asking the blocks nicely…", "Đang năn nỉ mấy khối…"), Loc.T("They still say no…", "Tụi nó vẫn từ chối…"), "Nah." },
            _ => new[] { Loc.T("Inflating balls…", "Đang bơm bóng…"), Loc.T("Lowering expectations…", "Đang hạ kỳ vọng…"), Loc.T("Squishing things together…", "Đang ép mọi thứ lại…"), "Meh." },
        };
    }
}
