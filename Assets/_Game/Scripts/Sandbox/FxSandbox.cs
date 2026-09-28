using System.Collections;
using System.Collections.Generic;
using CasualGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CasualGame.Sandbox
{
    /// <summary>
    /// FX design bench. Every effect is a prefab with an FxEffect root in Assets/_Game/Fx/Prefabs.
    /// Click = play the selected effect at the cursor, Space = replay, R = auto-repeat, H = hide panel, 1–9 = select.
    /// Each play instantiates the prefab asset, so edits made to the prefab (even during Play mode) show on the next click
    /// and are kept after Play mode ends.
    /// </summary>
    public sealed class FxSandbox : MonoBehaviour
    {
        public const string EffectsFolder = "Assets/_Game/Fx/Prefabs";

        [SerializeField] private List<FxEffect> effects = new();
        [SerializeField] private MergeChainDemo mergeChain;
        [SerializeField] private LineClearDemo lineClear;
        [SerializeField] private DropDemo drop;
        [SerializeField] private GlassJarDemo jar;
        [SerializeField] private InkArrowDemo ink;

        private static readonly (string name, Color color)[] Backgrounds =
        {
            ("navy", UIKit.Hex("#2B2F55")), ("night", UIKit.Hex("#1B1640")), ("cream", UIKit.Hex("#FFF8EC")), ("grey", new Color(0.5f, 0.5f, 0.5f)),
        };
        private static readonly float[] Speeds = { 0.1f, 0.25f, 0.5f, 1f };

        private int selected;
        private int tint = -1; // -1 = white (authored colors)
        private float scale = 1f;
        private bool repeat;
        private float repeatEvery = 1.2f, repeatTimer;
        private int background;
        private bool showReference = true, showPanel = true;
        private Vector3 lastPos;
        private Transform reference;
        private int particles;
        private float statsTimer;
        private Vector2 scroll;
        private Camera cam;

        public float TimeScale { get; private set; } = 1f;
        public Color CurrentTint => tint < 0 ? Color.white : SandboxArt.Palette[tint];

        private void Awake()
        {
            cam = Camera.main;
#if UNITY_EDITOR
            Rescan();
#endif
            BuildReference();
            ApplyBackground();
        }

        private void OnDisable() => Time.timeScale = 1f;

        public void SetTimeScale(float value) { TimeScale = value; Time.timeScale = value; }

        /// <summary>Instantiates the named effect prefab (fresh copy, so live prefab edits apply) and plays it.</summary>
        public void Play(string effectName, Vector3 position, Color color, float size = 1f)
        {
            var prefab = effects.Find(e => e != null && e.name == effectName);
            if (prefab == null) { Debug.LogWarning($"FxSandbox: no effect prefab '{effectName}' in {EffectsFolder}"); return; }
            Play(prefab, position, color, size);
        }

        private void Play(FxEffect prefab, Vector3 position, Color color, float size)
        {
            var fx = Instantiate(prefab);
            fx.Play(position, color, size);
            Destroy(fx.gameObject, fx.Duration + 0.25f);
        }

        private bool kicking;

        public void Kick(float amount, float duration) => StartCoroutine(KickRoutine(amount, duration));

        private IEnumerator KickRoutine(float amount, float duration)
        {
            kicking = true;
            var home = CameraHome;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                var k = amount * (1f - t / duration);
                cam.transform.position = home + (Vector3)(Random.insideUnitCircle * k);
                yield return null;
            }
            cam.transform.position = CameraHome;
            kicking = false;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame) PlaySelected(lastPos);
                if (kb.rKey.wasPressedThisFrame) repeat = !repeat;
                if (kb.hKey.wasPressedThisFrame) showPanel = !showPanel;
                for (int i = 0; i < 9 && i < effects.Count; i++)
                    if (kb[Key.Digit1 + i].wasPressedThisFrame) selected = i;
            }
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !OverPanel(mouse.position.ReadValue()))
            {
                var p = cam.ScreenToWorldPoint(mouse.position.ReadValue());
                lastPos = new Vector3(p.x, p.y, 0f);
                PlaySelected(lastPos);
            }
            if (repeat && (repeatTimer += Time.unscaledDeltaTime) >= repeatEvery)
            {
                repeatTimer = 0f;
                PlaySelected(lastPos);
            }
            if (!kicking) cam.transform.position = CameraHome;
            if ((statsTimer += Time.unscaledDeltaTime) > 0.25f)
            {
                statsTimer = 0f;
                particles = 0;
                foreach (var ps in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)) particles += ps.particleCount;
            }
        }

        private void PlaySelected(Vector3 pos)
        {
            if (selected < effects.Count && effects[selected] != null) Play(effects[selected], pos, CurrentTint, scale);
        }

        private void BuildReference()
        {
            reference = new GameObject("Reference (scale)").transform;
            SandboxArt.Character(reference, "circle", SandboxArt.Palette[6], 1.4f, "face_happy_0", new Vector3(-1.9f, -6f, 0f));
            SandboxArt.Character(reference, "block", SandboxArt.Palette[3], 1.1f, "face_grin_0", new Vector3(0f, -6f, 0f));
            SandboxArt.Character(reference, "circle", SandboxArt.Palette[2], 2.2f, "face_starstruck_0", new Vector3(2.1f, -6f, 0f));
            reference.gameObject.SetActive(showReference);
        }

        private void ApplyBackground() => cam.backgroundColor = Backgrounds[background].color;

#if UNITY_EDITOR
        private void Rescan()
        {
            effects.Clear();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { EffectsFolder }))
            {
                var fx = UnityEditor.AssetDatabase.LoadAssetAtPath<FxEffect>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (fx != null) effects.Add(fx);
            }
            effects.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, effects.Count - 1));
        }
#endif

        // ---------- panel (IMGUI: a dev tool, never shipped) ----------

        private float GuiScale => Screen.height / 1000f;
        private const float PanelW = 210f;

        /// <summary>Camera shifted left so world x = 0 sits in the middle of the area not covered by the panel.</summary>
        private Vector3 CameraHome => new(showPanel ? -(PanelW + 16f) * GuiScale / Screen.width * cam.orthographicSize * cam.aspect : 0f, 0f, -10f);

        private bool OverPanel(Vector2 screen) => showPanel && screen.x / GuiScale < PanelW + 16f;

        private void OnGUI()
        {
            GUI.matrix = Matrix4x4.Scale(new Vector3(GuiScale, GuiScale, 1f));
            float h = Screen.height / GuiScale;
            if (!showPanel)
            {
                if (GUI.Button(new Rect(8, 8, 90, 30), "Panel (H)")) showPanel = true;
                return;
            }
            GUILayout.BeginArea(new Rect(8, 8, PanelW, h - 16), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label("<b>FX SANDBOX</b>  click=play  Space=replay\nR=repeat  H=hide  1-9=select", Rich());
            GUILayout.Label($"particles alive: {particles}");

            GUILayout.Label("<b>Effect</b>", Rich());
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] == null) continue;
                var label = (i < 9 ? $"{i + 1}. " : "   ") + effects[i].name;
                if (GUILayout.Toggle(i == selected, label, GUI.skin.button)) selected = i;
            }
#if UNITY_EDITOR
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rescan")) Rescan();
            if (selected < effects.Count && effects[selected] != null && GUILayout.Button("Select prefab"))
                UnityEditor.Selection.activeObject = effects[selected].gameObject;
            GUILayout.EndHorizontal();
#endif

            GUILayout.Label("<b>Time</b>", Rich());
            GUILayout.BeginHorizontal();
            foreach (var s in Speeds)
                if (GUILayout.Toggle(Mathf.Approximately(TimeScale, s), s + "x", GUI.skin.button)) SetTimeScale(s);
            GUILayout.EndHorizontal();

            GUILayout.Label($"<b>Scale</b> {scale:0.00}", Rich());
            scale = GUILayout.HorizontalSlider(scale, 0.25f, 3f);

            GUILayout.BeginHorizontal();
            repeat = GUILayout.Toggle(repeat, "Repeat every");
            GUILayout.Label($"{repeatEvery:0.0}s", GUILayout.Width(40));
            GUILayout.EndHorizontal();
            repeatEvery = GUILayout.HorizontalSlider(repeatEvery, 0.2f, 3f);

            GUILayout.Label("<b>Tint</b> (tintable systems)", Rich());
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(tint < 0, "white", GUI.skin.button, GUILayout.Width(52))) tint = -1;
            for (int i = 0; i < SandboxArt.Palette.Length; i++)
            {
                if (GUILayout.Toggle(tint == i, "", GUI.skin.button, GUILayout.Width(18), GUILayout.Height(22))) tint = i;
                var r = GUILayoutUtility.GetLastRect();
                var inset = tint == i ? 2f : 4f;
                var old = GUI.color;
                GUI.color = SandboxArt.Palette[i];
                GUI.DrawTexture(new Rect(r.x + inset, r.y + inset, r.width - inset * 2f, r.height - inset * 2f), Texture2D.whiteTexture);
                GUI.color = old;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("<b>Background</b>", Rich());
            GUILayout.BeginHorizontal();
            for (int i = 0; i < Backgrounds.Length; i++)
                if (GUILayout.Toggle(background == i, Backgrounds[i].name, GUI.skin.button) && background != i) { background = i; ApplyBackground(); }
            GUILayout.EndHorizontal();
            var show = GUILayout.Toggle(showReference, "Scale reference");
            if (show != showReference) { showReference = show; reference.gameObject.SetActive(show); }

            GUILayout.Label("<b>Sequences</b> (tune in inspector)", Rich());
            if (mergeChain != null && GUILayout.Button("Merge chain")) mergeChain.Run();
            if (lineClear != null && GUILayout.Button("Blast line clear")) lineClear.Run();
            if (lineClear != null && GUILayout.Button("Blast 3 lines (impact frame)")) lineClear.Run(3);
            if (drop != null && GUILayout.Button("Drop & wobble")) drop.Run();
            if (jar != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Jar: drop")) jar.Drop();
                if (GUILayout.Button("fill")) jar.Fill();
                if (GUILayout.Button("clear")) jar.Teardown();
                GUILayout.EndHorizontal();
            }
            if (ink != null && GUILayout.Button("Arrow ink exit")) ink.Run();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static GUIStyle rich;
        private static GUIStyle Rich() => rich ??= new GUIStyle(GUI.skin.label) { richText = true };
    }
}
