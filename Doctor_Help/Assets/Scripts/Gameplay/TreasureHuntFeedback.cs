using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    [RequireComponent(typeof(CanvasRenderer)), DisallowMultipleComponent]
    public sealed class TreasureHuntFeedback : MaskableGraphic
    {
        [SerializeField] private Headbar headbar;
        [SerializeField] private Image coinPrefab;
        [SerializeField] private Color trackColor = new Color(.12f, .09f, .07f, .94f);
        [SerializeField] private Color fillColor = new Color(1f, .78f, .36f, 1f);
        private Vector2? ringCenter;
        private float progress;

        internal void ShowProgress(Vector2 lens, float fraction)
        {
            float scale = Screen.height / 1920f;
            float margin = 46f * scale;
            float sideways = 140f * scale;
            Rect safe = ScreenSafeArea.Current;
            lens.x += lens.x + sideways + margin <= safe.xMax ? sideways : -sideways;
            lens.x = Mathf.Clamp(lens.x, safe.xMin + margin, safe.xMax - margin);
            lens.y = Mathf.Clamp(lens.y, safe.yMin + margin, safe.yMax - margin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, lens, null, out var center);
            if (ringCenter == center && progress == fraction) return;
            ringCenter = center;
            progress = fraction;
            SetVerticesDirty();
        }

        internal void HideProgress()
        {
            if (!ringCenter.HasValue) return;
            ringCenter = null;
            SetVerticesDirty();
        }

        internal void FlyGold(Vector2 screenPosition, int amount)
        {
            if (headbar == null || coinPrefab == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, null, out var start);
            for (int i = 0; i < amount; i++)
            {
                var coin = Instantiate(coinPrefab, rectTransform, false).rectTransform;
                coin.localPosition = start;
                StartCoroutine(FlyCoin(coin, start, Random.insideUnitCircle * 30f, i * .035f));
            }
        }

        private IEnumerator FlyCoin(RectTransform coin, Vector2 start, Vector2 scatter, float delay)
        {
            const float duration = .55f;
            for (float elapsed = -delay; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                var target = headbar.GoldTarget;
                var end = rectTransform.InverseTransformPoint(target.TransformPoint(target.rect.center));
                coin.localPosition = Vector2.Lerp(start, end, t * t) + scatter * Mathf.Sin(Mathf.PI * t);
                yield return null;
            }
            Destroy(coin.gameObject);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (!ringCenter.HasValue) return;
            var wholeCanvas = (RectTransform)canvas.rootCanvas.transform;
            float scale = wholeCanvas.rect.height / 1920f;
            float radius = 38f * scale;
            float thickness = 9f * scale;
            TutorialOverlay.AddRing(mesh, ringCenter.Value, radius, thickness, 1f, trackColor);
            if (progress > 0)
                TutorialOverlay.AddRing(mesh, ringCenter.Value, radius - thickness * .12f,
                    thickness * .76f, progress, fillColor);
        }
    }
}
