using UnityEditor;
using UnityEngine;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// The boot credit layers (Tools/art/make-boot.mjs) must keep their full 512 × 512 frame: the head, the sunglasses
    /// and the lens mask are stacked in the same rect, so a sprite trimmed to its content would no longer line up.
    /// </summary>
    public class BootArtImporter : AssetPostprocessor
    {
        private const string Folder = "Assets/_Game/Art/Boot/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.npotScale = TextureImporterNPOTScale.None;
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(settings);
        }
    }
}
