using System.Collections;
using System.Collections.Generic;
using CasualGame.Core;
using UnityEngine;

namespace CasualGame.Sandbox
{
    /// <summary>
    /// Eye Merge jar with the GlassJar front glass: real Physics2D balls (like the game) drop in, wobble on every hit
    /// (JellyWobble) and make the glass glint when they hit a wall. Built on first use; Clear() empties it.
    /// </summary>
    public sealed class GlassJarDemo : MonoBehaviour
    {
        [SerializeField] private FxSandbox sandbox;

        [Header("Jar (world units, interior)")]
        [SerializeField] private Rect interior = new(-2.6f, -4.7f, 5.2f, 7.0f);
        [SerializeField] private float cornerRadius = 0.9f;
        [SerializeField] private string glassLayer = "Glass";

        [Header("Balls")]
        [SerializeField] private float baseRadius = 0.32f;
        [SerializeField] private float tierGrowth = 1.25f;
        [SerializeField] private int maxTier = 5;
        [SerializeField] private float gravityScale = 1.4f;
        [SerializeField, Range(0f, 1f)] private float bounciness = 0.18f;
        [SerializeField] private float fillCount = 10, fillInterval = 0.28f;

        private Transform root;
        private GlassJar glass;
        private readonly List<GameObject> balls = new();
        private PhysicsMaterial2D material;

        public void Drop()
        {
            Build();
            int tier = Random.Range(1, maxTier + 1);
            float r = baseRadius * Mathf.Pow(tierGrowth, tier - 1);
            float x = Random.Range(interior.xMin + r + 0.1f, interior.xMax - r - 0.1f);
            Spawn(tier, new Vector2(x, interior.yMax - r - 0.1f));
        }

        public void Fill() => StartCoroutine(FillRoutine());

        private IEnumerator FillRoutine()
        {
            for (int i = 0; i < fillCount; i++)
            {
                Drop();
                yield return new WaitForSeconds(fillInterval);
            }
        }

        public void Clear()
        {
            foreach (var b in balls) if (b != null) Destroy(b);
            balls.Clear();
        }

        public void Teardown()
        {
            Clear();
            if (root != null) Destroy(root.gameObject);
            root = null;
        }

        private void Build()
        {
            if (root != null) return;
            root = new GameObject("JarDemo").transform;
            material = new PhysicsMaterial2D("JarBall") { bounciness = bounciness, friction = 0.2f };

            var size = new Vector2(interior.width + 0.36f, interior.height + 0.36f);
            var center = (Vector3)interior.center;
            var back = SandboxArt.Sprite(root, "jar_back", UIKit.Hex("#3A3F72"), 1f, 1);
            back.drawMode = SpriteDrawMode.Sliced;
            back.transform.localScale = Vector3.one;
            back.size = size;
            back.transform.position = center;
            var line = SandboxArt.Sprite(root, "jar_line", Color.white, 1f, 1);
            line.drawMode = SpriteDrawMode.Sliced;
            line.transform.localScale = Vector3.one;
            line.size = size;
            line.transform.position = center;
            line.sortingLayerName = glassLayer; // outline above the glass

            glass = GlassJar.Create(root, interior, cornerRadius, glassLayer, 0);

            void Wall(Vector2 pos, Vector2 s)
            {
                var go = new GameObject(JarBall.WallName, typeof(BoxCollider2D));
                go.transform.SetParent(root, false);
                go.transform.position = pos;
                go.GetComponent<BoxCollider2D>().size = s;
            }
            Wall(new Vector2(interior.xMin - 0.5f, interior.center.y), new Vector2(1f, interior.height + 6f));
            Wall(new Vector2(interior.xMax + 0.5f, interior.center.y), new Vector2(1f, interior.height + 6f));
            Wall(new Vector2(interior.center.x, interior.yMin - 0.5f), new Vector2(interior.width + 2f, 1f));
        }

        private void Spawn(int tier, Vector2 pos)
        {
            float r = baseRadius * Mathf.Pow(tierGrowth, tier - 1);
            var go = new GameObject($"JarBall{tier}", typeof(Rigidbody2D), typeof(CircleCollider2D));
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var body = go.GetComponent<Rigidbody2D>();
            body.gravityScale = gravityScale;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.freezeRotation = true;
            var col = go.GetComponent<CircleCollider2D>();
            col.radius = r;
            col.sharedMaterial = material;

            var color = SandboxArt.Palette[(tier - 1) % SandboxArt.Palette.Length];
            var visual = SandboxArt.Character(go.transform, "circle", color, r / SandboxArt.FillRadius, "face_happy_0", pos);
            var wobble = visual.gameObject.AddComponent<JellyWobble>();
            go.AddComponent<JarBall>().Init(wobble, glass, visual, r);
            balls.Add(go);
        }
    }
}
