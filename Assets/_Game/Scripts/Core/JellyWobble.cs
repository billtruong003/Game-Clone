using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// Jelly squash-and-wobble for a character's VISUAL transform (not the physics body): Impact() squashes it along the
    /// hit axis, then a damped spring wobbles it back. Volume is kept (the other axis bulges), and the side that hit
    /// stays put, so a ball landing on the floor squashes onto it instead of shrinking into the air.
    /// </summary>
    public sealed class JellyWobble : MonoBehaviour
    {
        [SerializeField, Tooltip("Wobble frequency (Hz).")] private float frequency = 3.6f;
        [SerializeField, Tooltip("How fast the wobble dies out (1/s).")] private float damping = 6.5f;
        [SerializeField, Tooltip("Squash at full strength (0.3 = 30% flatter).")] private float maxSquash = 0.28f;
        [SerializeField, Range(0f, 1f), Tooltip("How much the other axis bulges to keep the volume.")] private float bulge = 0.8f;

        private Vector3 restScale, restPos;
        private float amp, t, radius;
        private bool vertical, active;
        private int side;

        /// <summary>Squash along the hit direction. <paramref name="contactNormal"/> points from the contact into the ball
        /// (up for a floor hit); <paramref name="radius"/> = distance from the pivot to the contact (keeps that side in place).</summary>
        // The rest pose is taken once, when the visual is built. Re-reading it at impact time would bake in whatever
        // another animation is doing right then (a merge pop at 115 %), leaving the ball bigger than its collider for good.
        private void Awake()
        {
            restScale = transform.localScale;
            restPos = transform.localPosition;
        }

        public void Impact(float strength01, Vector2 contactNormal, float radius)
        {
            amp = Mathf.Clamp01(strength01) * maxSquash;
            t = 0f;
            vertical = Mathf.Abs(contactNormal.y) >= Mathf.Abs(contactNormal.x);
            side = vertical ? (contactNormal.y > 0f ? -1 : 1) : (contactNormal.x > 0f ? -1 : 1);
            this.radius = radius;
            active = true;
        }

        private void LateUpdate()
        {
            if (!active) return;
            t += Time.deltaTime;
            float env = amp * Mathf.Exp(-damping * t);
            float v = env * Mathf.Cos(2f * Mathf.PI * frequency * t);
            if (env < 0.002f)
            {
                transform.localScale = restScale;
                transform.localPosition = restPos;
                active = false;
                return;
            }
            float along = 1f - v, across = 1f + v * bulge;
            transform.localScale = vertical
                ? new Vector3(restScale.x * across, restScale.y * along, restScale.z)
                : new Vector3(restScale.x * along, restScale.y * across, restScale.z);
            // keep the contact side where it was: shift the center toward it by the squash amount
            var shift = radius * v * side;
            var offset = vertical ? new Vector3(0f, shift, 0f) : new Vector3(shift, 0f, 0f);
            // world axes: the parent may be a rolling ball, while this transform is kept world-aligned
            if (transform.parent != null) transform.position = transform.parent.TransformPoint(restPos) + offset;
            else transform.localPosition = restPos + offset;
        }

        public bool IsWobbling => active;
    }
}
