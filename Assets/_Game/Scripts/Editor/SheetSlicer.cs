using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CasualGame.Core;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// Automatic slicer for the generated sprite sheets: every Assets/_Game/Art/Sheets/X.png is sliced from its
    /// sidecar X.json (written by Tools/art/make-art.mjs). Sprite IDs are derived from sprite names, so rebuilding
    /// or replacing the art never breaks references in scenes and prefabs.
    /// </summary>
    public class SheetSlicer : AssetPostprocessor
    {
        public const string Folder = "Assets/_Game/Art/Sheets/";
        public const string LibraryPath = ArtLibrary.EditorAllPath;

        [Serializable] private class SheetJson { public string sheet; public int width; public int height; public float pixelsPerUnit = 100; public SpriteJson[] sprites; }
        [Serializable] private class SpriteJson { public string name; public int x; public int y; public int w; public int h; public int[] border; }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.spritePixelsPerUnit = 100;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = 4096;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            // flat art with hard edges survives ASTC 6×6 fine at half the size of the default 4×4
            var android = ti.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = 2048;
            android.format = TextureImporterFormat.ASTC_6x6;
            ti.SetPlatformTextureSettings(android);
            // Full Rect meshes: required by 9-sliced / tiled SpriteRenderers (jar, danger line) and cheap for flat art.
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(settings);
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            var sheets = imported
                .Where(p => p.StartsWith(Folder) && (p.EndsWith(".png") || p.EndsWith(".json")))
                .Select(p => Path.ChangeExtension(p, ".png"))
                .Distinct()
                .ToList();
            if (sheets.Count == 0) return;
            EditorApplication.delayCall += () =>
            {
                foreach (var sheet in sheets) Slice(sheet);
                RebuildLibrary();
            };
        }

        [MenuItem("Tools/Casual Game/Reslice All Sheets")]
        public static void SliceAll()
        {
            foreach (var png in Directory.GetFiles(Folder, "*.png")) Slice(png.Replace('\\', '/'));
            RebuildLibrary();
        }

        /// <returns>true when the rects changed and the texture was reimported.</returns>
        public static bool Slice(string pngPath)
        {
            var jsonPath = Path.ChangeExtension(pngPath, ".json");
            if (!File.Exists(jsonPath) || !(AssetImporter.GetAtPath(pngPath) is TextureImporter importer)) return false;
            var data = JsonUtility.FromJson<SheetJson>(File.ReadAllText(jsonPath));

            var rects = data.sprites.Select(s => new SpriteRect
            {
                name = s.name,
                spriteID = StableId($"{data.sheet}/{s.name}"),
                rect = new Rect(s.x, data.height - s.y - s.h, s.w, s.h), // JSON is top-left origin, Unity bottom-left
                border = s.border is { Length: 4 } b ? new Vector4(b[0], b[1], b[2], b[3]) : Vector4.zero,
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
            }).ToArray();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            if (SameRects(provider.GetSpriteRects(), rects)) return false;

            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
                ?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
            Debug.Log($"SheetSlicer: sliced {pngPath} into {rects.Length} sprites");
            return true;
        }

        private static bool SameRects(SpriteRect[] a, SpriteRect[] b)
        {
            if (a == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i].name != b[i].name || a[i].rect != b[i].rect || a[i].border != b[i].border || a[i].spriteID != b[i].spriteID) return false;
            return true;
        }

        private static GUID StableId(string key)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
            return new GUID(BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant());
        }

        [MenuItem("Tools/Casual Game/Rebuild Art Library")]
        public static void RebuildLibrary()
        {
            RebuildLibrary(LibraryPath, null);
            BuildSwitcher.RebuildGameLibraries();
        }

        /// <summary>Builds a library from the given sheets (null = every sheet).</summary>
        public static ArtLibrary RebuildLibrary(string libraryPath, string[] sheetNames)
        {
            var sprites = new List<Sprite>();
            foreach (var png in Directory.GetFiles(Folder, "*.png").Select(p => p.Replace('\\', '/')))
            {
                if (sheetNames != null && !sheetNames.Contains(Path.GetFileNameWithoutExtension(png))) continue;
                sprites.AddRange(AssetDatabase.LoadAllAssetsAtPath(png).OfType<Sprite>());
            }

            var library = AssetDatabase.LoadAssetAtPath<ArtLibrary>(libraryPath);
            if (library == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(libraryPath)!);
                library = ScriptableObject.CreateInstance<ArtLibrary>();
                AssetDatabase.CreateAsset(library, libraryPath);
            }
            library.EditorSet(sprites.OrderBy(s => s.name).ToList(), FaceTextureImporter.EnsureMaterial());
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssetIfDirty(library);
            return library;
        }
    }
}
