using System;
using System.Collections.Generic;
using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// Which clip plays for each sound name the games use. Clips can live anywhere (the licensed packs keep their own
    /// folders and are not in the repo; this asset only references them). A "&lt;music&gt;_intro" entry plays once before
    /// its loop. Edit it in the Inspector, then Tools/Casual Game/Rebuild Audio Library.
    /// </summary>
    [CreateAssetMenu(menuName = "Casual Game/Audio Map")]
    public class AudioMap : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("Name the code plays, e.g. merge, ui_click, music_merge, music_merge_intro")] public string name;
            public AudioClip clip;
            [Range(0f, 1.5f)] public float volume = 1f;
            [Tooltip("Why this clip, so the next person can judge a swap")] public string note;
        }

        public List<Entry> entries = new();
    }
}
