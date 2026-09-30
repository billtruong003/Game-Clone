using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>Slice order of Assets/_Game/Art/Faces/faces.png (drawn by Tools/art/faces.mjs).</summary>
    public enum FaceId { Stare, Smug, Meh, Grin, Blink, Shock, Panic, Cry, Dizzy }

    /// <summary>
    /// A character's face. Expressions are not animated frame by frame: the body moves (jelly, rotation, squash) and the
    /// face only switches slices of one Texture2DArray — idle deadpan with blinks and the odd change of mood, a held
    /// mood while something happens (falling, dragged, danger), or a short reaction.
    /// Renders through <see cref="FaceGraphic"/> on a canvas or <see cref="FaceQuad"/> in the world; both batch.
    /// </summary>
    public sealed class Face : MonoBehaviour
    {
        // idle moods: mostly the blank stare, sometimes smug / unimpressed / content
        private static readonly FaceId[] IdlePool = { FaceId.Stare, FaceId.Stare, FaceId.Stare, FaceId.Smug, FaceId.Meh, FaceId.Grin };
        private const float BlinkTime = 0.11f;

        private FaceGraphic graphic;
        private FaceQuad quad;
        private FaceId idle = FaceId.Stare;
        private FaceId mood;
        private bool hasMood, holdMood;
        private float moodUntil, nextBlink, blinkUntil, nextIdleSwap;
        private FaceId shown = (FaceId)(-1);

        public FaceId Showing => shown;
        public FaceId Idle => idle;

        public static Face AddUI(Transform parent, Vector2 size, Vector2 offset = default)
        {
            var rt = UIKit.Place(UIKit.Rect("Face", parent), new Vector2(0.5f, 0.5f), offset, size);
            var face = rt.gameObject.AddComponent<Face>();
            face.graphic = rt.gameObject.AddComponent<FaceGraphic>();
            face.graphic.raycastTarget = false;
            face.Restart();
            return face;
        }

        public static Face AddWorld(Transform parent, float size, int sortingOrder, string sortingLayer = null)
        {
            var go = new GameObject("Face");
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * size;
            var face = go.AddComponent<Face>();
            face.quad = FaceQuad.Add(go, sortingOrder, sortingLayer);
            face.Restart();
            return face;
        }

        /// <summary>Picks a random idle mood and desyncs the blink clock from its neighbours.</summary>
        public void Restart()
        {
            idle = IdlePool[Random.Range(0, IdlePool.Length)];
            hasMood = holdMood = false;
            nextBlink = Time.time + Random.Range(0.5f, 4f);
            nextIdleSwap = Time.time + Random.Range(6f, 14f);
            Refresh();
        }

        public void SetIdle(FaceId face)
        {
            idle = face;
            nextIdleSwap = Time.time + Random.Range(8f, 16f);
            Refresh();
        }

        /// <summary>Shows <paramref name="face"/> for <paramref name="seconds"/>; negative holds it until <see cref="ClearReaction"/>.</summary>
        public void React(FaceId face, float seconds)
        {
            mood = face;
            hasMood = true;
            holdMood = seconds < 0f;
            moodUntil = Time.time + seconds;
            Refresh();
        }

        /// <summary>Keeps <paramref name="face"/> up while called every frame (falling, being dragged); lapses shortly after.</summary>
        public void Keep(FaceId face, float linger = 0.15f)
        {
            if (hasMood && holdMood) return; // an explicit hold (game over, danger) wins
            if (!hasMood || mood != face || moodUntil < Time.time + linger) React(face, linger);
        }

        public void ClearReaction()
        {
            hasMood = holdMood = false;
            Refresh();
        }

        public void SetAlpha(float a)
        {
            if (graphic != null) graphic.color = new Color(1f, 1f, 1f, a);
            if (quad != null) quad.Alpha = a;
        }

        private void Update()
        {
            var now = Time.time;
            if (hasMood && !holdMood && now >= moodUntil) hasMood = false;
            if (!hasMood && now >= nextIdleSwap)
            {
                idle = IdlePool[Random.Range(0, IdlePool.Length)];
                nextIdleSwap = now + Random.Range(6f, 14f);
            }
            if (now >= nextBlink)
            {
                blinkUntil = now + BlinkTime;
                nextBlink = now + Random.Range(2.2f, 6f);
            }
            Refresh();
        }

        private void Refresh()
        {
            var f = hasMood ? mood : idle;
            // only calm faces blink; a double blink now and then reads as "…really?"
            if (Time.time < blinkUntil && (f == FaceId.Stare || f == FaceId.Meh || f == FaceId.Smug)) f = FaceId.Blink;
            if (f == shown) return;
            shown = f;
            if (graphic != null) graphic.Slice = (int)f;
            if (quad != null) quad.Slice = (int)f;
        }
    }
}
