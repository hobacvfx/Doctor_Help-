using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class ClueFlight : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private RawImage snapshot;
        [SerializeField] private TMP_Text caption;
        [SerializeField, Min(.1f)] private float duration = .9f;
        private RectTransform rect, parent;
        private Vector2 start, control;
        private Func<Vector2> destination;
        private RenderTexture texture;
        private float elapsed;
        public void Begin(Vector2 screen, Func<Vector2> target, Sprite sprite, RenderTexture capture, string text, TMP_FontAsset font)
        {
            rect = (RectTransform)transform; parent = (RectTransform)rect.parent; destination = target;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out start);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, target(), null, out var end);
            control = (start + end) * .5f + Vector2.up * 95;
            texture = capture; icon.gameObject.SetActive(sprite != null); icon.sprite = sprite; icon.preserveAspect = true;
            snapshot.gameObject.SetActive(capture != null); snapshot.texture = capture;
            caption.gameObject.SetActive(!string.IsNullOrEmpty(text)); caption.text = text;
            if (font != null) caption.font = font;
            rect.anchoredPosition = start;
        }
        private void Update()
        {
            if (destination == null) return;
            elapsed += Time.unscaledDeltaTime; float t = Mathf.Clamp01(elapsed / duration);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, destination(), null, out var end);
            float u = t * t * (3 - 2 * t);
            rect.anchoredPosition = (1-u)*(1-u)*start + 2*(1-u)*u*control + u*u*end;
            rect.localScale = Vector3.one * Mathf.Lerp(1.15f, .32f, u);
            if (t >= 1) Destroy(gameObject);
        }
        private void OnDestroy() { if (texture != null) RenderTexture.ReleaseTemporary(texture); }
    }
}
