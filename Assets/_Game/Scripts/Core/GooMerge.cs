using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// Draws two balls as one gooey body (GooMerge shader: smooth union of two circles) while they merge, so they melt
    /// together like water drops instead of overlapping. The caller hides the balls' own body sprites (faces stay on top)
    /// and calls Set() every frame with the current centers/radii; the quad resizes itself to cover both.
    /// </summary>
    public sealed class GooMerge : MonoBehaviour
    {
        private static readonly int A = Shader.PropertyToID("_A"), B = Shader.PropertyToID("_B");
        private static readonly int ColorA = Shader.PropertyToID("_ColorA"), ColorB = Shader.PropertyToID("_ColorB");
        private static readonly int K = Shader.PropertyToID("_K"), Flash = Shader.PropertyToID("_Flash");

        private Material material;

        public static GooMerge Create(Transform parent, int sortingOrder)
        {
            var go = new GameObject("GooMerge");
            go.transform.SetParent(parent, false);
            var goo = go.AddComponent<GooMerge>();
            // one quad and one shader lookup for every merge; each blob keeps its own material (merges overlap)
            if (quad == null)
            {
                quad = new Mesh { name = "GooQuad" };
                quad.SetVertices(new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(-0.5f, 0.5f), new Vector3(0.5f, 0.5f) });
                quad.SetTriangles(new[] { 0, 2, 1, 2, 3, 1 }, 0);
            }
            if (shader == null) shader = Shader.Find("CasualGame/GooMerge");
            go.AddComponent<MeshFilter>().sharedMesh = quad;
            var r = go.AddComponent<MeshRenderer>();
            goo.material = new Material(shader);
            r.sharedMaterial = goo.material;
            r.sortingOrder = sortingOrder;
            return goo;
        }

        /// <param name="blend">neck softness in world units (0 = two plain circles, ~radius = very gooey)</param>
        public void Set(Vector2 a, float ra, Color ca, Vector2 b, float rb, Color cb, float blend, float flash = 0f)
        {
            material.SetVector(A, new Vector4(a.x, a.y, ra, 0f));
            material.SetVector(B, new Vector4(b.x, b.y, rb, 0f));
            material.SetColor(ColorA, ca);
            material.SetColor(ColorB, cb);
            material.SetFloat(K, blend);
            material.SetFloat(Flash, flash);
            // cover both circles plus the neck
            var min = Vector2.Min(a - Vector2.one * ra, b - Vector2.one * rb) - Vector2.one * blend;
            var max = Vector2.Max(a + Vector2.one * ra, b + Vector2.one * rb) + Vector2.one * blend;
            transform.position = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, 0f);
            transform.localScale = new Vector3(max.x - min.x, max.y - min.y, 1f);
        }

        private static Mesh quad;
        private static Shader shader;

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
