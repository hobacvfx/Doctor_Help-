using UnityEngine;

namespace DoctorHelp
{
    // Keeps the normal uGUI Button/EventSystem path, but rejects hits outside the authored collider.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class Collider2DButtonHitArea : MonoBehaviour, ICanvasRaycastFilter
    {
        [SerializeField] private Collider2D hitArea;
        private Matrix4x4 lastColliderMatrix;
        private bool colliderTransformSynced;
        public Collider2D HitArea => hitArea;

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (hitArea == null || !hitArea.enabled || !hitArea.gameObject.activeInHierarchy) return false;
            var rectangle = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(rectangle, screenPoint, eventCamera, out var worldPoint))
                return false;
            // CanvasScaler/layout can move the collider after the last physics step.
            var matrix = hitArea.transform.localToWorldMatrix;
            if (!colliderTransformSynced || matrix != lastColliderMatrix)
            {
                Physics2D.SyncTransforms();
                lastColliderMatrix = matrix;
                colliderTransformSynced = true;
            }
            return hitArea.OverlapPoint(worldPoint);
        }
        private void OnEnable() => colliderTransformSynced = false;
        private void Reset() => hitArea = GetComponent<Collider2D>();
    }
}
