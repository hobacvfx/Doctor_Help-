using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class NewspaperStarGraphic : RawImage
    {
        private float fill = 1;

        internal void Configure(Texture atlas, float fraction)
        {
            texture = atlas;
            fill = Mathf.Clamp01(fraction);
            gameObject.SetActive(fill > 0);
            SetVerticesDirty();
        }

        internal void ShowFrame(int frame, int frameCount) =>
            uvRect = new Rect(frame / (float)frameCount, 0, 1f / frameCount, 1);

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            var uv = uvRect;
            float right = Mathf.Lerp(rect.xMin, rect.xMax, fill);
            float u = Mathf.Lerp(uv.xMin, uv.xMax, fill);
            Color32 tint = color;
            mesh.AddVert(new Vector2(rect.xMin, rect.yMin), tint, new Vector2(uv.xMin, uv.yMin));
            mesh.AddVert(new Vector2(rect.xMin, rect.yMax), tint, new Vector2(uv.xMin, uv.yMax));
            mesh.AddVert(new Vector2(right, rect.yMax), tint, new Vector2(u, uv.yMax));
            mesh.AddVert(new Vector2(right, rect.yMin), tint, new Vector2(u, uv.yMin));
            mesh.AddTriangle(0, 1, 2);
            mesh.AddTriangle(2, 3, 0);
        }
    }
}
