using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CasualGame.Core
{
    /// <summary>
    /// Anime impact frame for big moments (Blast multi-line clear): the game freezes for two frames that show the scene as
    /// flat two-tone silhouettes — paper background with ink shapes, then inverted — and everything snaps back.
    /// Run(cam): every SpriteRenderer becomes a silhouette (particles are hidden). RunUI(cam, canvas, subjects): for uGUI
    /// screens, only the given graphics become silhouettes and the rest of the canvas is hidden for those frames.
    /// Use as a coroutine: yield return frame.Run(cam).
    /// </summary>
    public sealed class ImpactFrame : MonoBehaviour
    {
        [SerializeField, Tooltip("Real seconds per frame (two frames are shown).")] private float frameTime = 0.05f;
        [SerializeField] private Color ink = new(0.118f, 0.133f, 0.251f, 1f);
        [SerializeField] private Color paper = new(1f, 0.973f, 0.925f, 1f);
        [SerializeField, Tooltip("Show ink-on-paper then paper-on-ink. Off = only the first frame.")] private bool invertSecond = true;

        private Material inkMat, paperMat, uiInkMat, uiPaperMat;
        private readonly List<(Graphic g, Material m)> uiSubjects = new();
        private readonly List<Behaviour> uiHidden = new();
        private readonly List<(SpriteRenderer r, Material m)> sprites = new();
        private readonly List<Renderer> hidden = new();

        public bool Playing { get; private set; }

        public IEnumerator Run(Camera cam)
        {
            if (Playing) yield break;
            Playing = true;
            EnsureMaterials();
            var bg = cam.backgroundColor;
            var timeScale = Time.timeScale;
            Time.timeScale = 0f; // the impact frame is a freeze

            sprites.Clear();
            hidden.Clear();
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r is SpriteRenderer sr) sprites.Add((sr, sr.sharedMaterial));
                else { r.enabled = false; hidden.Add(r); }
            }

            Apply(inkMat);
            cam.backgroundColor = paper;
            yield return new WaitForSecondsRealtime(frameTime);
            if (invertSecond)
            {
                Apply(paperMat);
                cam.backgroundColor = ink;
                yield return new WaitForSecondsRealtime(frameTime);
            }

            foreach (var (r, m) in sprites) if (r != null) r.sharedMaterial = m;
            foreach (var r in hidden) if (r != null) r.enabled = true;
            cam.backgroundColor = bg;
            Time.timeScale = timeScale;
            Playing = false;
        }

        public IEnumerator RunUI(Camera cam, Canvas canvas, ICollection<Graphic> subjects)
        {
            if (Playing) yield break;
            Playing = true;
            EnsureMaterials();
            var bg = cam.backgroundColor;
            var timeScale = Time.timeScale;
            Time.timeScale = 0f;

            uiSubjects.Clear();
            uiHidden.Clear();
            hidden.Clear();
            foreach (var g in canvas.GetComponentsInChildren<Graphic>())
            {
                if (!g.enabled) continue;
                if (subjects.Contains(g)) uiSubjects.Add((g, g.material == g.defaultMaterial ? null : g.material));
                else { g.enabled = false; uiHidden.Add(g); }
            }
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (r.enabled && r.gameObject.activeInHierarchy) { r.enabled = false; hidden.Add(r); }

            foreach (var (g, _) in uiSubjects) g.material = uiInkMat;
            cam.backgroundColor = paper;
            yield return new WaitForSecondsRealtime(frameTime);
            if (invertSecond)
            {
                foreach (var (g, _) in uiSubjects) g.material = uiPaperMat;
                cam.backgroundColor = ink;
                yield return new WaitForSecondsRealtime(frameTime);
            }

            foreach (var (g, m) in uiSubjects) if (g != null) g.material = m;
            foreach (var b in uiHidden) if (b != null) b.enabled = true;
            foreach (var r in hidden) if (r != null) r.enabled = true;
            cam.backgroundColor = bg;
            Time.timeScale = timeScale;
            Playing = false;
        }

        private void Apply(Material m)
        {
            foreach (var (r, _) in sprites) if (r != null) r.sharedMaterial = m;
        }

        private void EnsureMaterials()
        {
            if (inkMat != null) return;
            var shader = Shader.Find("CasualGame/Silhouette");
            inkMat = new Material(shader);
            inkMat.SetColor("_SilhouetteColor", ink);
            paperMat = new Material(shader);
            paperMat.SetColor("_SilhouetteColor", paper);
            var uiShader = Shader.Find("CasualGame/UISilhouette");
            uiInkMat = new Material(uiShader);
            uiInkMat.SetColor("_SilhouetteColor", ink);
            uiPaperMat = new Material(uiShader);
            uiPaperMat.SetColor("_SilhouetteColor", paper);
        }

        private void OnDestroy()
        {
            if (inkMat != null) Destroy(inkMat);
            if (paperMat != null) Destroy(paperMat);
            if (uiInkMat != null) Destroy(uiInkMat);
            if (uiPaperMat != null) Destroy(uiPaperMat);
        }
    }
}
