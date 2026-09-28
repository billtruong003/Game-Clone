using UnityEngine;
using UnityEngine.UI;

namespace CasualGame.Core
{
    /// <summary>
    /// Frame-animated emoji face drawn on top of a body sprite. Works on a UI Image or a SpriteRenderer.
    /// Each face picks a random emoji and a random phase so a board full of them never animates in sync.
    /// <see cref="React"/> swaps to another emoji for a moment (laugh on merge, surprised on danger, …).
    /// </summary>
    public class EmojiFace : MonoBehaviour
    {
        private Image image;
        private SpriteRenderer spriteRenderer;
        private EmojiDef idle;
        private EmojiDef shown;
        private float clock;
        private float reactUntil = -1f;
        private bool holdReaction;

        public EmojiDef Idle => idle;

        private void Awake()
        {
            image = GetComponent<Image>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void SetEmoji(EmojiDef def)
        {
            idle = def;
            shown = def;
            clock = Random.value * 10f;
            reactUntil = -1f;
            holdReaction = false;
            Apply();
        }

        public void SetRandom() => SetEmoji(ArtLibrary.Instance.RandomEmoji());

        /// <summary>Shows another emoji for <paramref name="seconds"/>; a negative duration holds it until <see cref="ClearReaction"/>.</summary>
        public void React(string emojiName, float seconds)
        {
            var def = ArtLibrary.Instance.GetEmoji(emojiName);
            if (def == null) return;
            shown = def;
            clock = 0f;
            holdReaction = seconds < 0f;
            reactUntil = Time.time + seconds;
            Apply();
        }

        public void ClearReaction()
        {
            holdReaction = false;
            reactUntil = -1f;
            shown = idle;
        }

        private void Update()
        {
            if (shown == null) return;
            if (!holdReaction && reactUntil > 0f && Time.time >= reactUntil)
            {
                reactUntil = -1f;
                shown = idle;
            }
            clock += Time.deltaTime;
            Apply();
        }

        private void Apply()
        {
            if (shown == null || shown.frames == null || shown.frames.Length == 0) return;
            var seq = shown.sequence;
            int frame;
            if (seq != null && seq.Length > 0)
            {
                var step = (int)(clock * shown.fps) % seq.Length;
                frame = seq[step];
            }
            else
            {
                frame = (int)(clock * shown.fps) % shown.frames.Length;
            }
            var sprite = shown.frames[Mathf.Clamp(frame, 0, shown.frames.Length - 1)];
            if (image != null) image.sprite = sprite;
            else if (spriteRenderer != null) spriteRenderer.sprite = sprite;
        }
    }
}
