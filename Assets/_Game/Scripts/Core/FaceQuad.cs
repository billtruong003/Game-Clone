using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>World-space face: a unit quad whose shared mesh (one per slice) is swapped to change expression.</summary>
    public sealed class FaceQuad : MonoBehaviour
    {
        private static Mesh[] meshes;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static MaterialPropertyBlock block;

        private MeshFilter filter;
        private MeshRenderer meshRenderer;
        private int slice = -1;
        private float alpha = 1f;

        public static FaceQuad Add(GameObject go, int sortingOrder, string sortingLayer)
        {
            var q = go.AddComponent<FaceQuad>();
            q.filter = go.AddComponent<MeshFilter>();
            q.meshRenderer = go.AddComponent<MeshRenderer>();
            q.meshRenderer.sharedMaterial = ArtLibrary.Instance.FaceMaterial;
            q.meshRenderer.sortingOrder = sortingOrder;
            if (!string.IsNullOrEmpty(sortingLayer)) q.meshRenderer.sortingLayerName = sortingLayer;
            q.meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            q.meshRenderer.receiveShadows = false;
            q.Slice = 0;
            return q;
        }

        public int Slice
        {
            get => slice;
            set
            {
                if (slice == value) return;
                slice = value;
                filter.sharedMesh = MeshFor(value);
            }
        }

        public float Alpha
        {
            get => alpha;
            set
            {
                if (Mathf.Approximately(alpha, value)) return;
                alpha = value;
                if (Mathf.Approximately(value, 1f)) { meshRenderer.SetPropertyBlock(null); return; } // back to batching
                block ??= new MaterialPropertyBlock();
                block.SetColor(ColorId, new Color(1f, 1f, 1f, value));
                meshRenderer.SetPropertyBlock(block);
            }
        }

        private static Mesh MeshFor(int s)
        {
            meshes ??= new Mesh[System.Enum.GetValues(typeof(FaceId)).Length];
            if (meshes[s] != null) return meshes[s];
            var m = new Mesh { name = "FaceQuad" + s };
            m.SetVertices(new[] { new Vector3(-0.5f, -0.5f), new Vector3(-0.5f, 0.5f), new Vector3(0.5f, 0.5f), new Vector3(0.5f, -0.5f) });
            m.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) });
            m.SetUVs(1, new[] { new Vector2(s, 0), new Vector2(s, 0), new Vector2(s, 0), new Vector2(s, 0) });
            m.SetColors(new[] { Color.white, Color.white, Color.white, Color.white });
            m.SetTriangles(new[] { 0, 1, 2, 2, 3, 0 }, 0);
            m.UploadMeshData(true);
            return meshes[s] = m;
        }
    }
}
