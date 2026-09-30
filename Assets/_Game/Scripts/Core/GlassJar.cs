using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// The front glass of the Merge jar (GlassJar shader, toon glass: translucent tint + hard diagonal lines).
    /// A ball hitting the wall nudges the lines sideways and lets them spring back. Lives on the "Glass" sorting
    /// layer, over the balls.
    /// </summary>
    public sealed class GlassJar : MonoBehaviour
    {
        private static readonly int JarId = Shader.PropertyToID("_Jar"), CornerId = Shader.PropertyToID("_Corner");
        private static readonly int ShiftId = Shader.PropertyToID("_Shift");

        [SerializeField, Tooltip("How far a hard hit slides the lines (in jar half-widths).")] private float maxShift = 0.25f;
        [SerializeField, Tooltip("Spring back speed.")] private float returnSpeed = 6f;

        private Material material;
        private float shift, velocity;

        public Material Material => material;

        /// <param name="interior">jar interior in world units</param>
        public static GlassJar Create(Transform parent, Rect interior, float cornerRadius, string sortingLayer, int order)
        {
            var go = new GameObject("GlassJar");
            go.transform.SetParent(parent, false);
            var jar = go.AddComponent<GlassJar>();
            var mesh = new Mesh { name = "GlassQuad" };
            mesh.SetVertices(new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(-0.5f, 0.5f), new Vector3(0.5f, 0.5f) });
            mesh.SetTriangles(new[] { 0, 2, 1, 2, 3, 1 }, 0);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            jar.material = new Material(Shader.Find("CasualGame/GlassJar"));
            r.sharedMaterial = jar.material;
            r.sortingLayerName = sortingLayer;
            r.sortingOrder = order;
            go.transform.position = new Vector3(interior.center.x, interior.center.y, 0f);
            go.transform.localScale = new Vector3(interior.width, interior.height, 1f);
            jar.material.SetVector(JarId, new Vector4(interior.xMin, interior.yMin, interior.xMax, interior.yMax));
            jar.material.SetFloat(CornerId, cornerRadius);
            return jar;
        }

        /// <summary>A ball hit the wall: kick the lines sideways; <paramref name="strength01"/> scales the kick.</summary>
        public void Glint(float strength01 = 1f) => velocity += Mathf.Clamp01(strength01) * maxShift * 12f * (Random.value < 0.5f ? -1f : 1f);

        private void Update()
        {
            if (Mathf.Abs(shift) < 1e-4f && Mathf.Abs(velocity) < 1e-3f) return;
            // damped spring back to 0
            var dt = Time.deltaTime;
            velocity += (-shift * returnSpeed * returnSpeed - velocity * returnSpeed * 0.9f) * dt;
            shift = Mathf.Clamp(shift + velocity * dt, -maxShift, maxShift);
            material.SetFloat(ShiftId, shift);
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
            var mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) Destroy(mf.sharedMesh);
        }
    }
}
