using CasualGame.Core;
using UnityEditor;
using UnityEngine;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// Imports Assets/_Game/Art/Faces/faces.png (3×3 flipbook written by Tools/art/make-faces.mjs) as a Texture2DArray,
    /// one slice per <see cref="FaceId"/>, and keeps the shared FaceArray material pointing at it. The shop's face
    /// packs (faces_&lt;pack&gt;.png, same layout) import the same way.
    /// </summary>
    public class FaceTextureImporter : AssetPostprocessor
    {
        public const string TexturePath = "Assets/_Game/Art/Faces/faces.png";
        public const string MaterialPath = "Assets/_Game/Art/Faces/FaceArray.mat";

        private void OnPreprocessTexture()
        {
            if (assetPath != TexturePath && !IsPack(assetPath)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default;
            ti.textureShape = TextureImporterShape.Texture2DArray;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.flipbookColumns = 3;
            s.flipbookRows = 3;
            ti.SetTextureSettings(s);
            ti.sRGBTexture = true;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Compressed;
            var android = ti.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.format = TextureImporterFormat.ASTC_6x6;
            android.maxTextureSize = 1024;
            ti.SetPlatformTextureSettings(android);
        }

        public static bool IsPack(string path) =>
            path.StartsWith("Assets/_Game/Art/Faces/faces_") && path.EndsWith(".png");

        /// <summary>The material every face renders with (created on first use).</summary>
        public static Material EnsureMaterial()
        {
            var array = AssetDatabase.LoadAssetAtPath<Texture2DArray>(TexturePath);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("CasualGame/FaceArray")) { name = "FaceArray" };
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }
            if (array != null && mat.GetTexture("_Faces") != array)
            {
                mat.SetTexture("_Faces", array);
                EditorUtility.SetDirty(mat);
                AssetDatabase.SaveAssetIfDirty(mat);
            }
            return mat;
        }
    }
}
