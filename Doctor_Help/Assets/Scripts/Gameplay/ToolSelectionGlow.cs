using UnityEngine;

namespace DoctorHelp
{
    // A padded quad lets the glow extend beyond even a tightly trimmed sprite mesh.
    public sealed class ToolSelectionGlow : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer source;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer glowRenderer;
        [SerializeField, Range(1, 24)] private float thickness = 5;
        [SerializeField, Range(0, 3)] private float brightness = 1;
        [SerializeField, Range(0, 0.5f)] private float pulseAmount = 0.12f;
        [SerializeField, Min(0.1f)] private float pulsePeriod = 2.5f;
        private Mesh mesh;
        private MaterialPropertyBlock properties;
        private bool selected;
        private Sprite builtSprite;
        private float builtThickness;

        public void SetSelected(bool value)
        {
            selected = value;
            if (glowRenderer != null) glowRenderer.enabled = value;
        }
        private void Awake()
        {
            properties = new MaterialPropertyBlock();
            mesh = new Mesh { name = "Tool glow quad" };
            meshFilter.sharedMesh = mesh;
            SetSelected(false);
        }
        private void LateUpdate()
        {
            if (!selected || source == null || source.sprite == null) return;
            properties ??= new MaterialPropertyBlock();
            var sprite = source.sprite;
            var texture = sprite.texture;
            Rect rect = sprite.textureRect;
            if (sprite != builtSprite || !Mathf.Approximately(thickness, builtThickness))
            {
                float padding = thickness + 2;
                // bounds can include transparent margins; textureRectOffset maps the trimmed
                // UV rectangle back to the correct local position relative to the sprite pivot.
                Vector2 min = (sprite.textureRectOffset - sprite.pivot - Vector2.one * padding) / sprite.pixelsPerUnit;
                Vector2 max = min + (rect.size + Vector2.one * (2 * padding)) / sprite.pixelsPerUnit;
                mesh.vertices = new[] { new Vector3(min.x, min.y), new Vector3(min.x, max.y),
                    new Vector3(max.x, max.y), new Vector3(max.x, min.y) };
                float u0 = (rect.xMin - padding) / texture.width, u1 = (rect.xMax + padding) / texture.width;
                float v0 = (rect.yMin - padding) / texture.height, v1 = (rect.yMax + padding) / texture.height;
                mesh.uv = new[] { new Vector2(u0, v0), new Vector2(u0, v1), new Vector2(u1, v1), new Vector2(u1, v0) };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                mesh.RecalculateBounds();
                builtSprite = sprite;
                builtThickness = thickness;
            }
            meshFilter.transform.localScale = new Vector3(source.flipX ? -1 : 1, source.flipY ? -1 : 1, 1);
            glowRenderer.sortingLayerID = source.sortingLayerID;
            glowRenderer.sortingOrder = source.sortingOrder + 1;
            properties.SetTexture("_MainTex", texture);
            properties.SetVector("_SpriteRect", new Vector4(rect.xMin / texture.width, rect.yMin / texture.height,
                rect.xMax / texture.width, rect.yMax / texture.height));
            properties.SetFloat("_Thickness", thickness);
            properties.SetFloat("_Brightness", brightness * (1 + pulseAmount * Mathf.Sin(Time.unscaledTime * 2 * Mathf.PI / Mathf.Max(0.1f, pulsePeriod))));
            glowRenderer.SetPropertyBlock(properties);
        }
        private void OnDisable() => SetSelected(false);
        private void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}
