using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>G12: light = place / drop, medium = clear / merge / blocked, strong = big combo, legend, game over.</summary>
    public enum HapticLevel { Light, Medium, Strong }

    /// <summary>Persistent SFX/music player. SFX use a small round-robin pool so overlapping pops don't cut each other off.</summary>
    public class GameAudio : MonoBehaviour
    {
        private const int Voices = 10;

        private static GameAudio instance;
        private AudioSource[] voices;
        private AudioSource music, intro;
        private AudioLibrary library;
        private int next;

        public static void Play(string clipName, float pitch = 1f, float volume = 1f)
        {
            if (instance == null || !GameSettings.Sound) return;
            instance.PlayInternal(clipName, pitch, volume);
        }

        /// <summary>
        /// Starts the game's track: "&lt;name&gt;_intro" once (when the library has one), then "&lt;name&gt;" looping, joined
        /// sample-accurately with PlayScheduled so the seam never clicks. Nothing happens if the clip is missing.
        /// </summary>
        public static void PlayMusic(string clipName)
        {
            if (instance == null || instance.library == null) return;
            var loop = instance.library.Get(clipName);
            if (loop == null || instance.music.clip == loop) return;
            var intro = instance.library.Get(clipName + "_intro");
            var volume = MusicVolume * instance.library.Volume(clipName);
            instance.intro.Stop();
            instance.music.Stop();
            instance.music.clip = loop;
            instance.music.volume = volume;
            instance.intro.clip = intro;
            instance.intro.volume = volume;
            if (intro == null) instance.music.Play();
            else instance.ScheduleFrom(0f);
        }

        private const float MusicVolume = 0.5f;
        private float pausedIntroAt = -1f;

        // intro from `introTime`, the loop queued to start the moment the intro ends
        private void ScheduleFrom(float introTime)
        {
            var start = AudioSettings.dspTime + 0.05;
            intro.time = introTime;
            intro.PlayScheduled(start);
            music.PlayScheduled(start + (intro.clip.length - introTime));
        }

        /// <summary>Pauses the music while the game is paused (G13); SFX from the popup still play.</summary>
        public static void PauseMusic(bool paused)
        {
            if (instance == null || instance.music.clip == null) return;
            instance.SetPaused(paused);
        }

        public static void Haptic(HapticLevel level = HapticLevel.Medium)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!GameSettings.Vibration || vibratorFailed) return;
            long ms = level == HapticLevel.Light ? 10L : level == HapticLevel.Medium ? 22L : 45L;
            int amplitude = level == HapticLevel.Light ? 60 : level == HapticLevel.Medium ? 140 : 255;
            try
            {
                if (vibrator == null)
                {
                    // looked up once: every merge / clear calls this, and the JNI lookups are not free
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    using var version = new AndroidJavaClass("android.os.Build$VERSION");
                    if (version.GetStatic<int>("SDK_INT") >= 26) effects = new AndroidJavaClass("android.os.VibrationEffect");
                    if (vibrator == null) { vibratorFailed = true; return; }
                }
                if (effects != null)
                {
                    using var effect = effects.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude);
                    vibrator.Call("vibrate", effect);
                }
                else vibrator.Call("vibrate", ms); // Android 7.1: no strength control
            }
            catch { vibratorFailed = true; }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject vibrator;
        private static AndroidJavaClass effects;
        private static bool vibratorFailed;
#endif

        // A loop already scheduled behind a paused intro would start on its own and play over it, so a pause during
        // the intro stops both and remembers where the intro was; resuming schedules them again from there.
        private void SetPaused(bool paused)
        {
            if (paused)
            {
                if (intro.clip != null && intro.isPlaying)
                {
                    pausedIntroAt = intro.time;
                    intro.Stop();
                    music.Stop();
                }
                else music.Pause();
                return;
            }
            if (pausedIntroAt >= 0f)
            {
                ScheduleFrom(pausedIntroAt);
                pausedIntroAt = -1f;
            }
            else music.UnPause();
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
            intro = gameObject.AddComponent<AudioSource>();
            intro.loop = false;
            intro.playOnAwake = false;
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.volume = MusicVolume;
            GameSettings.Changed += ApplySettings;
            ApplySettings();
        }

        private void OnDestroy() => GameSettings.Changed -= ApplySettings;

        private void ApplySettings() => music.mute = intro.mute = !GameSettings.Music;

        private void PlayInternal(string clipName, float pitch, float volume)
        {
            var clip = library != null ? library.Get(clipName) : null;
            if (clip == null) return;
            var src = voices[next];
            next = (next + 1) % Voices;
            src.pitch = pitch * Random.Range(0.95f, 1.05f); // G13: the same pop never sounds exactly twice the same
            src.PlayOneShot(clip, volume * library.Volume(clipName));
        }
    }
}
