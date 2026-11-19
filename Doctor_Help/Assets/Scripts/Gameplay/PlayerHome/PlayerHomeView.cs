using UnityEngine;

namespace DoctorHelp
{
    [DefaultExecutionOrder(100), DisallowMultipleComponent, RequireComponent(typeof(DeskActionButton))]
    public sealed class PlayerHomeView : MonoBehaviour
    {
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private Vector2 safeAreaPadding = new Vector2(0.025f, 0.025f);
        [SerializeField] private RectTransform headbar;

        private SpriteRenderer visual;

        private void Awake() => visual = GetComponent<SpriteRenderer>();

        private void OnEnable()
        {
            PositionButton();
        }

        private void LateUpdate() => PositionButton();

        private void PositionButton()
        {
            if (sceneCamera == null || visual == null || sceneCamera.pixelWidth <= 0 || sceneCamera.pixelHeight <= 0)
                return;

            Rect safeArea = ScreenSafeArea.Current;
            var bounds = visual.bounds;
            Vector3 center = sceneCamera.WorldToScreenPoint(transform.position);
            Vector3 topLeft = sceneCamera.WorldToScreenPoint(
                new Vector3(bounds.min.x, bounds.max.y, bounds.center.z));
            float horizontalOffset = safeArea.xMin + safeArea.width * safeAreaPadding.x - topLeft.x;
            float top = safeArea.yMax - safeArea.height * safeAreaPadding.y;
            if (headbar != null && headbar.gameObject.activeInHierarchy)
            {
                Vector3 bottom = headbar.TransformPoint(new Vector3(0, headbar.rect.yMin));
                float barBottom = RectTransformUtility.WorldToScreenPoint(null, bottom).y;
                top = Mathf.Min(top, barBottom - safeArea.width * safeAreaPadding.x);
            }
            float verticalOffset = top - topLeft.y;
            if (Mathf.Abs(horizontalOffset) < .01f && Mathf.Abs(verticalOffset) < .01f) return;
            center.x += horizontalOffset;
            center.y += verticalOffset;
            transform.position = sceneCamera.ScreenToWorldPoint(center);
        }
    }
}
