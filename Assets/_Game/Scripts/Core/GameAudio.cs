using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>Persistent SFX/music player. SFX use a small round-robin pool so overlapping pops don't cut each other off.</summary>
    public class GameAudio : MonoBehaviour
    {
        private const int Voices = 10;

        private static GameAudio instance;
        private AudioSource[] voices;
        private AudioSource music;
        private AudioLibrary library;
        private int next;

        public static void Play(string clipName, float pitch = 1f, float volume = 1f)
        {
            if (instance == null || !GameSettings.Sound) return;
            instance.PlayInternal(clipName, pitch, volume);
        }

        /// <summary>Starts a looping track if a clip with that name exists (drop Suno tracks into Assets/_Game/Audio/Music).</summary>
        public static void PlayMusic(string clipName)
        {
            if (instance == null) return;
            var clip = instance.library != null ? instance.library.Get(clipName) : null;
            if (clip == null || instance.music.clip == clip) return;
            instance.music.clip = clip;
            instance.music.Play();
        }

        public static void Haptic()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!GameSettings.Vibration) return;
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                vibrator?.Call("vibrate", 18L);
            }
            catch { /* no vibrator */ }
#endif
        }

        /// <summary>Called by the scene's GameContext with that game's clip list.</summary>
        public static void SetLibrary(AudioLibrary library)
        {
            if (instance != null) instance.library = library;
        }

        internal static void Create()
        {
            if (instance != null) return;
            var go = new GameObject("GameAudio");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GameAudio>();
        }

        private void Awake()
        {
#if UNITY_EDITOR
            library = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioLibrary>("Assets/_Game/Audio/Libraries/AudioLibrary_All.asset");
#endif
            voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.volume = 0.5f;
            GameSettings.Changed += ApplySettings;
            ApplySettings();
        }

        private void OnDestroy() => GameSettings.Changed -= ApplySettings;

        private void ApplySettings() => music.mute = !GameSettings.Music;

        private void PlayInternal(string clipName, float pitch, float volume)
        {
            var clip = library != null ? library.Get(clipName) : null;
            if (clip == null) return;
            var src = voices[next];
            next = (next + 1) % Voices;
            src.pitch = pitch;
            src.PlayOneShot(clip, volume);
        }
    }
}
