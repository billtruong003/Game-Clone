using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>
    /// The front glass of the Merge jar (GlassJar shader): rim light, reflection streak, faint tint, refraction near the
    /// walls, and a glint band that sweeps across when Glint() is called (a ball hitting the glass) plus now and then
    /// on its own. Lives on the "Glass" sorting layer so it can refract everything drawn on "Default".
    /// </summary>
    public sealed class GlassJar : MonoBehaviour
    {
        private static readonly int JarId = Shader.PropertyToID("_Jar"), CornerId = Shader.PropertyToID("_Corner");
        private static readonly int GlintPosId = Shader.PropertyToID("_GlintPos"), GlintStrengthId = Shader.PropertyToID("_GlintStrength");

        [SerializeField, Tooltip("Seconds for one glint sweep across the jar.")] private float glintTime = 0.45f;
        [SerializeField, Tooltip("Idle glint every N seconds (0 = never).")] private float idleEvery = 5f;
        [SerializeField, Tooltip("Hits closer together than this don't restart the sweep.")] private float minGap = 0.25f;

        private Material material;
        private float glint = -1f, strength, idleTimer, lastGlint = -10f;

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
            jar.material.SetFloat(GlintPosId, -1f);
            return jar;
        }

        /// <summary>Sweep a glint across the glass; <paramref name="strength01"/> scales its brightness (e.g. impact speed).</summary>
        public void Glint(float strength01 = 1f)
        {
            if (Time.time - lastGlint < minGap && glint >= 0f) { strength = Mathf.Max(strength, Mathf.Clamp01(strength01)); return; }
            lastGlint = Time.time;
            glint = 0f;
            strength = Mathf.Clamp(strength01, 0.35f, 1f);
            idleTimer = 0f;
        }

        private void Update()
        {
            if (idleEvery > 0f && (idleTimer += Time.deltaTime) >= idleEvery) Glint(0.5f);
            if (glint < 0f) return;
            glint += Time.deltaTime / glintTime;
            if (glint > 1f)
            {
                glint = -1f;
                material.SetFloat(GlintPosId, -1f);
                return;
            }
            // band travels from beyond the left edge to beyond the right edge, easing out
            float t = 1f - (1f - glint) * (1f - glint);
            material.SetFloat(GlintPosId, Mathf.Lerp(-0.25f, 1.35f, t));
            material.SetFloat(GlintStrengthId, strength * Mathf.Sin(glint * Mathf.PI) * 0.9f + 0.1f * strength);
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
            var mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) Destroy(mf.sharedMesh);
        }
    }
}
