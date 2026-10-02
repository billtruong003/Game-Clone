using System;
using UnityEngine;

namespace CasualGame.ArrowOut
{
    /// <summary>
    /// Drives one arrow of an interactive theme (Assets/_Game/Skins/Arrows): Train lamps show if the way is clear and
    /// the cars bunch up on a bump; Ants fidget when blocked and march off; Tape and Zipper peel / unzip instead of
    /// sliding out (ArrowBoardView runs that, through <see cref="Progress"/>).
    /// </summary>
    public sealed class ArrowLive : MonoBehaviour
    {
        public ArrowSkins.LiveKind Kind;
        public Material Mat;
        public Func<bool> IsFree;
        public RectTransform Roll;      // tape: the roll that winds the tape up
        public bool Leaving;
        public float Progress;          // tape peel / zipper unzip, 0..1 while leaving

        private float free = 1f, bumpT = 99f, march;

        /// <summary>A blocked tap: the train's cars bunch up, ants fidget, tape half-peels and sticks back, the zip jams.</summary>
        public void Bump() => bumpT = 0f;

        public float BumpAmount => bumpT < 0.5f ? Mathf.Sin(bumpT / 0.5f * Mathf.PI) : 0f;

        private void OnDestroy()
        {
            if (Mat != null) Destroy(Mat);
        }

        private void Update()
        {
            if (Mat == null) return;
            var dt = Time.deltaTime;
            bumpT += dt;
            free = Mathf.MoveTowards(free, Leaving || IsFree == null || IsFree() ? 1f : 0f, dt * 4f);
            Mat.SetFloat("_Free", free);
            var bump = BumpAmount;
            switch (Kind)
            {
                case ArrowSkins.LiveKind.Train:
                    Mat.SetFloat("_Bump", bump);
                    Mat.SetFloat("_Go", Leaving ? 1f : 0f);
                    break;
                case ArrowSkins.LiveKind.Ants:
                    march += dt * (Leaving ? 14f : 4f + 4f * (1f - free));
                    Mat.SetFloat("_March", march);
                    break;
                case ArrowSkins.LiveKind.Tape:
                    Mat.SetFloat("_Peel", Leaving ? Progress : 0.3f * bump);
                    break;
                case ArrowSkins.LiveKind.Zipper:
                    Mat.SetFloat("_Unzip", Leaving ? Progress : 0.06f * Mathf.Abs(Mathf.Sin(bumpT * 18f)) * bump);
                    break;
            }
        }
    }
}
