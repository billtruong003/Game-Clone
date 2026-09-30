using System;
using UnityEditor;
using UnityEngine;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// Play-mode screenshots that work while the editor is in the background: pauses, steps N frames, then renders
    /// the main camera (with its camera-space canvases) into a 1080×1920 PNG. Used for automated visual checks.
    /// </summary>
    public static class CaptureTool
    {
        public static void StepAndCapture(int frames, string path, Action before = null)
        {
            if (!EditorApplication.isPlaying) return;
            EditorApplication.isPaused = true;
            before?.Invoke();
            int n = frames;
            EditorApplication.CallbackFunction step = null;
            step = () =>
            {
                if (n-- > 0) { EditorApplication.Step(); return; }
                EditorApplication.update -= step;
                Capture(path);
            };
            EditorApplication.update += step;
        }

        public static void Capture(string path)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var rt = new RenderTexture(1080, 1920, 24);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prev;
            RenderTexture.active = rt;
            var tex = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release();
        }
    }
}
