using UnityEditor;
using UnityEngine;

namespace CasualGame.EditorTools
{
    /// <summary>
    /// Import settings for the licensed audio packs, applied whenever they are (re)imported so every machine builds the
    /// same thing. SFX: mono ADPCM, decompressed on load (instant, tiny). Music: Vorbis kept compressed in memory, so
    /// PlayScheduled can join an intro and its loop without the start-up delay a streamed clip has.
    /// </summary>
    public class AudioImportRules : AssetPostprocessor
    {
        public const string SfxFolder = "Assets/Casual Game Sounds U6/";
        public const string MusicFolder = "Assets/Scenes/Season Cycle Casual Gaming Music Pack/";

        private void OnPreprocessAudio()
        {
            var importer = (AudioImporter)assetImporter;
            if (assetPath.StartsWith(SfxFolder))
            {
                importer.forceToMono = true;
                importer.defaultSampleSettings = new AudioImporterSampleSettings
                {
                    loadType = AudioClipLoadType.DecompressOnLoad,
                    compressionFormat = AudioCompressionFormat.ADPCM,
                    sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate,
                    preloadAudioData = true,
                };
            }
            else if (assetPath.StartsWith(MusicFolder))
            {
                importer.forceToMono = false;
                importer.loadInBackground = true;
                importer.defaultSampleSettings = new AudioImporterSampleSettings
                {
                    loadType = AudioClipLoadType.CompressedInMemory,
                    compressionFormat = AudioCompressionFormat.Vorbis,
                    quality = 0.45f,
                    sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate,
                    sampleRateOverride = 44100,
                    preloadAudioData = false,
                };
            }
        }

        [MenuItem("Tools/Casual Game/Reimport Audio Packs")]
        public static void ReimportPacks()
        {
            foreach (var folder in new[] { SfxFolder.TrimEnd('/'), MusicFolder.TrimEnd('/') })
                if (AssetDatabase.IsValidFolder(folder))
                    foreach (var g in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
                        AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(g), ImportAssetOptions.ForceUpdate);
        }
    }
}
