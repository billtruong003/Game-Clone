using UnityEditor;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// Build size: the Epic Toon FX textures are drawn small on a phone, so on Android they are capped at 256 px and
    /// compressed ASTC 8×8 (the pack ships them at 512–1024, uncompressed-looking at our size).
    /// </summary>
    public class FxTextureImporter : AssetPostprocessor
    {
        private const string Folder = "Assets/References/Epic Toon FX/Textures/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var ti = (TextureImporter)assetImporter;
            var android = ti.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = 256;
            android.format = TextureImporterFormat.ASTC_8x8;
            ti.SetPlatformTextureSettings(android);
        }
    }
}
