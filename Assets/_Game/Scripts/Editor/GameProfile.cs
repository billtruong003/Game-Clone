using UnityEngine;

namespace CasualGame.EditorTools
{
    /// <summary>Everything that makes one shippable game out of this multi-game project. Edited in the Build Switcher.</summary>
    [CreateAssetMenu(menuName = "Casual Game/Game Profile")]
    public class GameProfile : ScriptableObject
    {
        public string gameId;
        public string productName;
        [Tooltip("Store package id, e.g. com.yourstudio.arrowout")] public string applicationId;
        public string version = "0.1.0";
        public int versionCode = 1;
        [Tooltip("Dev profile ships every game plus the Hub launcher")] public bool isDev;
        [Tooltip("Scene names under Assets/_Game/Scenes; the first one starts the game")] public string[] scenes;
        [Tooltip("Sprite sheets (Assets/_Game/Art/Sheets) this game uses")] public string[] artSheets;
        [Tooltip("Audio clip names this game uses (empty = all)")] public string[] audioClips;
        [Tooltip("Scripting define set while this game is active, e.g. CG_ARROWOUT")] public string defineSymbol;
        public Texture2D icon;
    }
}
