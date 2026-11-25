using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    // Constrains screen-space popups to their Safe Area while the desk remains world-space.
    [RequireComponent(typeof(RectTransform))]
    public sealed class PopupPanelFit : MonoBehaviour
    {
        [SerializeField] private Vector2 maximumSize = new Vector2(520, 740);
        [SerializeField, Min(0)] private float padding = 16;
        [Tooltip("Optional list content: fit the panel to visible rows, up to the Safe Area limit.")]
        [SerializeField] private RectTransform content;
        [SerializeField, Min(0)] private float contentPaddingHeight = 210;
        [SerializeField, Min(0)] private float minimumHeight = 420;
        [Tooltip("Optional fixed width/height ratio. Zero keeps the existing independent size limits.")]
        [SerializeField, Min(0)] private float aspectRatio;
        private void OnEnable() => Fit();
        private void LateUpdate() => Fit();
        private void Fit()
        {
            var rect = (RectTransform)transform;
            if (!(rect.parent is RectTransform parent)) return;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            float desiredHeight = content != null
                ? Mathf.Max(minimumHeight, LayoutUtility.GetPreferredHeight(content) + contentPaddingHeight)
                : maximumSize.y;
            var size = new Vector2(Mathf.Min(maximumSize.x, Mathf.Max(0, parent.rect.width - 2 * padding)),
                Mathf.Min(desiredHeight, Mathf.Min(maximumSize.y, Mathf.Max(0, parent.rect.height - 2 * padding))));
            if (aspectRatio > 0)
            {
                size.x = Mathf.Min(size.x, size.y * aspectRatio);
                size.y = size.x / aspectRatio;
            }
            if (rect.sizeDelta != size) rect.sizeDelta = size;
        }
    }
}
