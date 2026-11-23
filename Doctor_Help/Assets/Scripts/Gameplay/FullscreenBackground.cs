using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    // Cover the whole root Canvas, including outside the safe area, without stretching the texture.
    [RequireComponent(typeof(Image))]
    public sealed class FullscreenBackground : MonoBehaviour
    {
        private Image image;
        private RectTransform rect;
        private Vector2 previousSize;
        private void Awake() { image = GetComponent<Image>(); rect = (RectTransform)transform; }
        private void OnEnable() => previousSize = Vector2.zero;
        private void LateUpdate()
        {
            if (image == null || image.sprite == null || !(rect.parent is RectTransform parent)) return;
            Vector2 size = parent.rect.size;
            if (size == previousSize) return;
            previousSize = size;
            Vector2 source = image.sprite.rect.size;
            rect.sizeDelta = source * Mathf.Max(size.x / source.x, size.y / source.y);
        }
    }
}
