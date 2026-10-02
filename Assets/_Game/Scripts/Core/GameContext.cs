using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// Per-scene wiring of the game's own art, audio, FX libraries and release config. Each game scene references only what that game
    /// uses, so a single-game build pulls in only that game's sheets and clips (nothing lives in Resources).
    /// Runs before any other script in the scene. In the Boot scene it only sets the libraries: ads (and the consent
    /// form they may show) start with the game scene, after the boot has faded out.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameContext : MonoBehaviour
    {
        [SerializeField] private string gameId;
        [SerializeField] private ArtLibrary art;
        [SerializeField] private AudioLibrary audio;
        [SerializeField] private FxCatalog fx;
        [SerializeField] private GameConfig config;
        [SerializeField] private bool bootScene;

        public string GameId => gameId;

        private void Awake()
        {
            if (art != null) ArtLibrary.SetCurrent(art);
            if (audio != null) GameAudio.SetLibrary(audio);
            GameFx.SetCatalog(fx);
            GameConfig.SetCurrent(config);
            FacePacks.Apply(); // the worn face pack, on every face
            if (!bootScene) Ads.Init(config);
        }

#if UNITY_EDITOR
        public void EditorWire(string id, ArtLibrary artLibrary, AudioLibrary audioLibrary, FxCatalog fxCatalog, GameConfig gameConfig, bool boot = false)
        {
            gameId = id;
            art = artLibrary;
            audio = audioLibrary;
            fx = fxCatalog;
            config = gameConfig;
            bootScene = boot;
        }
#endif
    }
}
