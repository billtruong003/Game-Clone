using System.Collections.Generic;
using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>All clips under Assets/_Game/Audio, filled by the editor (Tools/Casual Game/Project Setup). Looked up by file name.</summary>
    public class AudioLibrary : ScriptableObject
    {
        [SerializeField] private List<AudioClip> clips = new();
        private Dictionary<string, AudioClip> byName;

        public AudioClip Get(string clipName)
        {
            if (byName == null)
            {
                byName = new Dictionary<string, AudioClip>();
                foreach (var c in clips)
                    if (c != null) byName[c.name] = c;
            }
            return byName.TryGetValue(clipName, out var clip) ? clip : null;
        }

#if UNITY_EDITOR
        public void EditorSet(List<AudioClip> all)
        {
            clips = all;
            byName = null;
        }
#endif
    }
}
