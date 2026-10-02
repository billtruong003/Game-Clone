using System.IO;
using UnityEditor;
using UnityEngine;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// Captures for store screenshots and promo clips (Tools/promo, Remotion). Renders the main camera (UI included: the
    /// games use camera-space canvases) into a portrait render texture and writes a PNG under Tools/promo/public.
    /// Clips: set <see cref="Time.captureFramerate"/> = 30, then step the editor one frame at a time and call
    /// <see cref="Frame"/> after each step, so every frame is exactly 1/30 s of game time however slow the capture is.
    /// </summary>
    public static class PromoShots
    {
        public const string Root = "Tools/promo/public/";

        public static string Shot(string relativePath, int width = 1080, int height = 1920)
        {
            var cam = Camera.main;
            if (cam == null) return "no camera";
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var old = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = old;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            var path = Root + relativePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, tex.EncodeToJPG(92));
            Object.DestroyImmediate(tex);
            return path;
        }

        /// <summary>One clip frame: frames/&lt;clip&gt;/00042.jpg (720 x 1280 keeps a 30 s clip small and fast to write).</summary>
        public static string Frame(string clip, int index) => Shot($"frames/{clip}/{index:00000}.jpg", 720, 1280);

        /// <summary>Advances play mode n frames of exactly 1/30 s each (editor paused).</summary>
        public static void Step(int frames)
        {
            Time.captureFramerate = 30;
            EditorApplication.isPaused = true;
            for (int i = 0; i < frames; i++) EditorApplication.Step();
        }
    }
}
