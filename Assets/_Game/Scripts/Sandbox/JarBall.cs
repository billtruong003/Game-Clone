using CasualGame.Core;
using UnityEngine;

namespace CasualGame.Sandbox
{
    /// <summary>A physics ball in the jar demo: every hit wobbles its visual; hitting the glass hard makes the glass glint.</summary>
    public sealed class JarBall : MonoBehaviour
    {
        public const string WallName = "JarWall";

        private JellyWobble wobble;
        private GlassJar glass;
        private Transform visual;
        private float radius, squintUntil;

        public void Init(JellyWobble wobble, GlassJar glass, Transform visual, float radius)
        {
            this.wobble = wobble;
            this.glass = glass;
            this.visual = visual;
            this.radius = radius;
        }

        private void OnCollisionEnter2D(Collision2D c)
        {
            float speed = c.relativeVelocity.magnitude;
            if (speed < 1f) return;
            var contact = c.GetContact(0).point;
            var normal = ((Vector2)transform.position - contact).normalized; // from the contact into this ball
            wobble.Impact(speed / 12f, normal, radius);
            if (c.collider.name == WallName && speed > 2f && glass != null) glass.Glint(speed / 10f);
            if (speed > 6f)
            {
                SandboxArt.SetFace(visual, "face_laugh_0");
                squintUntil = Time.time + 0.18f;
            }
        }

        private void Update()
        {
            if (squintUntil > 0f && Time.time > squintUntil)
            {
                squintUntil = 0f;
                SandboxArt.SetFace(visual, "face_happy_0");
            }
        }
    }
}
