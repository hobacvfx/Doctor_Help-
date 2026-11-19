using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OpeningComicPanel : MaskableGraphic
    {
        [SerializeField] private Texture2D page;
        [SerializeField] private Vector2 topLeft, topRight, bottomRight, bottomLeft;

        public override Texture mainTexture => page != null ? page : base.mainTexture;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect bounds = rectTransform.rect;
            AddCorner(mesh, bounds, topLeft);
            AddCorner(mesh, bounds, bottomLeft);
            AddCorner(mesh, bounds, bottomRight);
            AddCorner(mesh, bounds, topRight);
            mesh.AddTriangle(0, 1, 2);
            mesh.AddTriangle(0, 2, 3);
        }

        private void AddCorner(VertexHelper mesh, Rect bounds, Vector2 pagePoint)
        {
            var uv = new Vector2(pagePoint.x, 1 - pagePoint.y);
            mesh.AddVert(Rect.NormalizedToPoint(bounds, uv), color, uv);
        }
    }
}
