using System;
using System.Collections.Generic;
using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// The clips one game plays, looked up by the name the code uses ("merge", "music_merge"). Built by the editor
    /// (ProjectSetup.BuildAudioLibrary) from the AudioMap plus any clip under Assets/_Game/Audio with that file name.
    /// </summary>
    public class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string name;
            public AudioClip clip;
            [Range(0f, 1.5f)] public float volume;
        }

        [SerializeField] private List<Entry> entries = new();
        [NonSerialized] private Dictionary<string, Entry> byName;

        public AudioClip Get(string clipName) => TryGet(clipName, out var e) ? e.clip : null;

        /// <summary>Every clip in the library (the boot preloads them).</summary>
        public IEnumerable<AudioClip> Clips
        {
            get { foreach (var e in entries) if (e.clip != null) yield return e.clip; }
        }

        /// <summary>Loudness trim for that name (packs are mastered differently); 1 when unknown.</summary>
        public float Volume(string clipName) => TryGet(clipName, out var e) && e.volume > 0f ? e.volume : 1f;

        private bool TryGet(string clipName, out Entry entry)
        {
            if (byName == null)
            {
                byName = new Dictionary<string, Entry>(entries.Count);
                foreach (var e in entries)
                    if (e.clip != null && !string.IsNullOrEmpty(e.name)) byName[e.name] = e;
            }
            return byName.TryGetValue(clipName, out entry);
        }

#if UNITY_EDITOR
        public void EditorSet(List<Entry> all)
        {
            entries = all;
            byName = null;
        }
#endif
    }
}
