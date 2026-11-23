using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    // Only discovered rich-text glyphs pulse. Other words and the row itself remain steady.
    [RequireComponent(typeof(Text))]
    public sealed class ClueTextPulse : BaseMeshEffect
    {
        [SerializeField] private Color discoveredColor = new Color32(220, 38, 38, 255);
        [SerializeField, Range(.1f, 1)] private float minimumAlpha = .67f;
        [SerializeField, Min(.1f)] private float period = 1.2f;
        private float nextRefresh;
        public Color DiscoveredColor => discoveredColor;
        private void LateUpdate()
        {
            if (!IsActive() || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .05f; graphic.SetVerticesDirty();
        }
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;
            Color32 marker = discoveredColor;
            float alpha = Mathf.Lerp(minimumAlpha, 1, .5f + .5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2 / period));
            UIVertex v = default;
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                if (v.color.r != marker.r || v.color.g != marker.g || v.color.b != marker.b) continue;
                v.color.a = (byte)Mathf.RoundToInt(v.color.a * alpha); vh.SetUIVertex(v, i);
            }
        }
    }
}
