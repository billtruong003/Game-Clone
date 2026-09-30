using UnityEngine;
using UnityEngine.UI;

namespace CasualGame.Core
{
    /// <summary>UI quad that draws one slice of the face array (slice in UV1.x, so all faces on a canvas batch).</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FaceGraphic : MaskableGraphic
    {
        private int slice;

        public int Slice
        {
            get => slice;
            set
            {
                if (slice == value) return;
                slice = value;
                SetVerticesDirty();
            }
        }

        public override Material defaultMaterial => ArtLibrary.Instance != null && ArtLibrary.Instance.FaceMaterial != null
            ? ArtLibrary.Instance.FaceMaterial : base.defaultMaterial;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (canvas != null) canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            var c = (Color32)color;
            var s = new Vector4(slice, 0, 0, 0);
            vh.AddVert(new Vector3(r.xMin, r.yMin), c, new Vector4(0, 0), s, Vector3.back, Vector4.zero);
            vh.AddVert(new Vector3(r.xMin, r.yMax), c, new Vector4(0, 1), s, Vector3.back, Vector4.zero);
            vh.AddVert(new Vector3(r.xMax, r.yMax), c, new Vector4(1, 1), s, Vector3.back, Vector4.zero);
            vh.AddVert(new Vector3(r.xMax, r.yMin), c, new Vector4(1, 0), s, Vector3.back, Vector4.zero);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }
}
