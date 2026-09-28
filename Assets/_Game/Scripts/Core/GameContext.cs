using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// Per-scene wiring of the game's own art and audio libraries. Each game scene references only what that game
    /// uses, so a single-game build pulls in only that game's sheets and clips (nothing lives in Resources).
    /// Runs before any other script in the scene.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameContext : MonoBehaviour
    {
        [SerializeField] private string gameId;
        [SerializeField] private ArtLibrary art;
        [SerializeField] private AudioLibrary audio;
        [SerializeField] private FxCatalog fx;

        public string GameId => gameId;

        private void Awake()
        {
            if (art != null) ArtLibrary.SetCurrent(art);
            if (audio != null) GameAudio.SetLibrary(audio);
            GameFx.SetCatalog(fx);
        }

#if UNITY_EDITOR
        public void EditorWire(string id, ArtLibrary artLibrary, AudioLibrary audioLibrary, FxCatalog fxCatalog)
        {
            gameId = id;
            art = artLibrary;
            audio = audioLibrary;
            fx = fxCatalog;
        }
#endif
    }
}
