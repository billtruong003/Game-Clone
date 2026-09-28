using System.Collections.Generic;
using System.IO;
using CasualGame.Core;
using UnityEditor;
using UnityEngine;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// Generates the ToonParticle inputs (shape masks, tileable noise, ramp palettes), its materials, and the helper that
    /// wires a ParticleSystem to the shader (custom vertex streams + erosion curve in Custom Data).
    /// Masks store a field in alpha: 0.5 = nominal edge, 1 = core. Everything is procedural, no painted texture.
    /// </summary>
    public static class ToonFxBuilder
    {
        public const string Root = "Assets/_Game/Fx";
        private const string TexDir = Root + "/Textures";
        private const string MatDir = Root + "/Materials";
        private const string ShaderName = "CasualGame/ToonParticle";
        private const int MaskSize = 128;

        public static readonly Color Ink = new(0.118f, 0.133f, 0.251f, 1f);

        [MenuItem("Tools/Casual Game/Toon FX/Rebuild Textures + Materials")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(TexDir);
            Directory.CreateDirectory(MatDir);
            WriteMask("mask_puff", (x, y) => 1f - Len(x, y));
            // astroid |x|^½ + |y|^½ = R^½: pointed tips, concave sides (a classic 4-point sparkle)
            WriteMask("mask_star", (x, y) => 0.5f - (Mathf.Sqrt(Mathf.Abs(x)) + Mathf.Sqrt(Mathf.Abs(y)) - Mathf.Sqrt(0.86f)) * 1.3f);
            WriteMask("mask_ring", (x, y) => 0.5f - (Mathf.Abs(Len(x, y) - 0.66f) - 0.12f) * 2.5f);
            WriteMask("mask_drop", (x, y) => 0.5f - UnevenCapsule(x, y, new Vector2(0.42f, 0f), 0.34f, new Vector2(-0.78f, 0f), 0.06f) * 2f);
            WriteMask("mask_shard", (x, y) => 0.5f - RoundBox(x, y, 0.58f, 0.36f, 0.12f) * 2f);
            // flame tongue: round belly at the bottom, sharp tip at the top (+v)
            WriteMask("mask_flame", (x, y) => 0.5f - UnevenCapsule(x, y, new Vector2(0f, -0.38f), 0.4f, new Vector2(0f, 0.8f), 0.03f) * 2f);
            WriteGlow("glow_soft");
            WriteNoise("noise_toon", 128, 4);
            WriteRamp("ramp_toon");
            AssetDatabase.Refresh();
            ImportSettings();

            Mat("Toon_Puff", "mask_puff", noise: 0.35f, outline: 0.08f, shade: 0.08f, highlight: 0.85f);
            Mat("Toon_PuffRamp", "mask_puff", noise: 0.35f, outline: 0.08f, shade: 0.06f, highlight: 0f, ramp: true);
            Mat("Toon_Star", "mask_star", noise: 0f, outline: 0.07f, shade: 0.04f, highlight: 0f);
            // thin outline + low noise: an eroding ring must not leave loose navy arcs behind
            Mat("Toon_Ring", "mask_ring", noise: 0.15f, outline: 0.035f, shade: 0f, highlight: 0f);
            Mat("Toon_Drop", "mask_drop", noise: 0f, outline: 0.08f, shade: 0.05f, highlight: 0f);
            Mat("Toon_Shard", "mask_shard", noise: 0.1f, outline: 0.09f, shade: 0.07f, highlight: 0f);
            // noise scrolling down the tongue makes the edges lick upward without any flipbook
            Mat("Toon_Flame", "mask_flame", noise: 0.4f, outline: 0.06f, shade: 0f, highlight: 0f, scroll: new Vector2(0f, -1.6f));
            // smoke: no outline, soft, drawn semi-transparent (reference smoke is never outlined)
            Mat("Toon_Smoke", "mask_puff", noise: 0.35f, outline: 0f, shade: 0.07f, highlight: 0f);
            GlowMat("Soft_Glow", "glow_soft", 0.8f);
            AssetDatabase.SaveAssets();
            Debug.Log("ToonFx: textures + materials rebuilt in " + Root);
        }

        public static Material Material(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{name}.mat");

        /// <summary>Custom vertex streams + Custom1.x erosion curve (threshold offset over lifetime; 0 = nominal shape, ~0.7 = gone).</summary>
        public static void ConfigureStreams(ParticleSystem ps, AnimationCurve erode, float rampRow = 0f)
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
            {
                ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV,
                ParticleSystemVertexStream.Custom1XY, ParticleSystemVertexStream.StableRandomX,
            });
            var cd = ps.customData;
            cd.enabled = true;
            cd.SetMode(ParticleSystemCustomData.Custom1, ParticleSystemCustomDataMode.Vector);
            cd.SetVectorComponentCount(ParticleSystemCustomData.Custom1, 2);
            cd.SetVector(ParticleSystemCustomData.Custom1, 0, new ParticleSystem.MinMaxCurve(1f, erode));
            cd.SetVector(ParticleSystemCustomData.Custom1, 1, new ParticleSystem.MinMaxCurve(rampRow));
        }

        // ---------- generators ----------

        private static float Len(float x, float y) => Mathf.Sqrt(x * x + y * y);

        private static float RoundBox(float x, float y, float bx, float by, float r)
        {
            float qx = Mathf.Abs(x) - bx + r, qy = Mathf.Abs(y) - by + r;
            return Len(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        private static float UnevenCapsule(float x, float y, Vector2 a, float ra, Vector2 b, float rb)
        {
            var p = new Vector2(x, y);
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / ba.sqrMagnitude);
            return (pa - ba * h).magnitude - Mathf.Lerp(ra, rb, h);
        }

        private static void WriteMask(string name, System.Func<float, float, float> field)
        {
            var tex = new Texture2D(MaskSize, MaskSize, TextureFormat.RGBA32, false, true);
            var px = new Color32[MaskSize * MaskSize];
            for (int j = 0; j < MaskSize; j++)
            for (int i = 0; i < MaskSize; i++)
            {
                float x = (i + 0.5f) / MaskSize * 2f - 1f, y = (j + 0.5f) / MaskSize * 2f - 1f;
                // fade the field to 0 near the quad border so nothing is ever cut by the quad edge
                float border = Mathf.Clamp01((1f - Mathf.Max(Mathf.Abs(x), Mathf.Abs(y))) / 0.12f);
                float v = Mathf.Clamp01(field(x, y)) * border;
                px[j * MaskSize + i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(v * 255f));
            }
            tex.SetPixels32(px);
            File.WriteAllBytes($"{TexDir}/{name}.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>Plain radial falloff (not a field): smooth, wide, for additive halos.</summary>
        private static void WriteGlow(string name)
        {
            var tex = new Texture2D(MaskSize, MaskSize, TextureFormat.RGBA32, false, true);
            var px = new Color32[MaskSize * MaskSize];
            for (int j = 0; j < MaskSize; j++)
            for (int i = 0; i < MaskSize; i++)
            {
                float x = (i + 0.5f) / MaskSize * 2f - 1f, y = (j + 0.5f) / MaskSize * 2f - 1f;
                float v = Mathf.Clamp01(1f - Len(x, y));
                v = v * v * (3f - 2f * v);
                px[j * MaskSize + i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(v * v * 255f));
            }
            tex.SetPixels32(px);
            File.WriteAllBytes($"{TexDir}/{name}.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GlowMat(string name, string tex, float intensity)
        {
            var path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("CasualGame/SoftGlow"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = Shader.Find("CasualGame/SoftGlow");
            m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/{tex}.png"));
            m.SetFloat("_Intensity", intensity);
            EditorUtility.SetDirty(m);
        }

        /// <summary>Tileable value noise, 2 octaves only: low frequency keeps eroded edges smooth (vector look, no grain).</summary>
        private static void WriteNoise(string name, int size, int cells)
        {
            float Hash(int x, int y) { unchecked { uint h = (uint)(x * 374761393 + y * 668265263); h = (h ^ (h >> 13)) * 1274126177u; return (h ^ (h >> 16)) / 4294967295f; } }
            float Value(float x, float y, int period)
            {
                int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
                float fx = x - xi, fy = y - yi;
                fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
                int x0 = ((xi % period) + period) % period, y0 = ((yi % period) + period) % period;
                int x1 = (x0 + 1) % period, y1 = (y0 + 1) % period;
                return Mathf.Lerp(Mathf.Lerp(Hash(x0, y0), Hash(x1, y0), fx), Mathf.Lerp(Hash(x0, y1), Hash(x1, y1), fx), fy);
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            var px = new Color32[size * size];
            for (int j = 0; j < size; j++)
            for (int i = 0; i < size; i++)
            {
                float u = (float)i / size, v = (float)j / size;
                float n = Value(u * cells, v * cells, cells) * 0.68f + Value(u * cells * 2, v * cells * 2, cells * 2) * 0.32f;
                byte b = (byte)Mathf.RoundToInt(Mathf.Clamp01(n) * 255f);
                px[j * size + i] = new Color32(b, b, b, 255);
            }
            tex.SetPixels32(px);
            File.WriteAllBytes($"{TexDir}/{name}.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>8 rows of hard-banded palettes (point filtered): edge → core. Row v = (row + 0.5) / 8.</summary>
        private static void WriteRamp(string name)
        {
            Color H(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
            var rows = new[]
            {
                new[] { H("#C23B3F"), H("#FF9F1C"), H("#FFD23F"), Color.white },   // 0 fire
                new[] { H("#7B3FC4"), H("#F15BB5"), H("#FFB8E4"), Color.white },   // 1 pink
                new[] { H("#2E6DB4"), H("#4EA8DE"), H("#A8DCFF"), Color.white },   // 2 blue
                new[] { H("#1F9E6A"), H("#3DDC97"), H("#B5F5D6"), Color.white },   // 3 green
                new[] { H("#D98A00"), H("#FFD23F"), H("#FFF0A8"), Color.white },   // 4 gold
                new[] { H("#5B3A9E"), H("#9B5DE5"), H("#D6BCFA"), Color.white },   // 5 purple
                new[] { H("#C23B3F"), H("#FF5A5F"), H("#FFB3B5"), Color.white },   // 6 red
                new[] { H("#9AA0B8"), H("#E3E6F2"), Color.white, Color.white },     // 7 smoke
            };
            float[] stops = { 0.18f, 0.45f, 0.78f };
            const int w = 64, h = 8;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int r = 0; r < h; r++)
            for (int i = 0; i < w; i++)
            {
                float d = (i + 0.5f) / w;
                int band = d < stops[0] ? 0 : d < stops[1] ? 1 : d < stops[2] ? 2 : 3;
                tex.SetPixel(i, r, rows[r][band]);
            }
            File.WriteAllBytes($"{TexDir}/{name}.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void ImportSettings()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                var file = Path.GetFileNameWithoutExtension(path);
                ti.textureType = TextureImporterType.Default;
                ti.mipmapEnabled = false;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.alphaSource = TextureImporterAlphaSource.FromInput;
                ti.sRGBTexture = file.StartsWith("ramp");
                ti.wrapMode = file.StartsWith("noise") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                ti.filterMode = file.StartsWith("ramp") ? FilterMode.Point : FilterMode.Bilinear;
                ti.SaveAndReimport();
            }
        }

        private static void Mat(string name, string mask, float noise, float outline, float shade, float highlight, bool ramp = false, Vector2 scroll = default)
        {
            var path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find(ShaderName));
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = Shader.Find(ShaderName);
            m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/{mask}.png"));
            m.SetTexture("_NoiseTex", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/noise_toon.png"));
            m.SetTexture("_RampTex", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/ramp_toon.png"));
            m.SetFloat("_NoiseStrength", noise);
            m.SetFloat("_OutlineWidth", outline);
            m.SetFloat("_ShadeOffset", shade);
            m.SetFloat("_Highlight", highlight);
            m.SetVector("_NoiseScroll", new Vector4(scroll.x, scroll.y, 0f, 0f));
            m.SetColor("_OutlineColor", Ink);
            m.SetFloat("_UseRamp", ramp ? 1f : 0f);
            if (ramp) m.EnableKeyword("_RAMP"); else m.DisableKeyword("_RAMP");
            EditorUtility.SetDirty(m);
        }
    }
}
