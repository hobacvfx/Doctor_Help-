using UnityEngine;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    public sealed class MagnifierLens : MonoBehaviour
    {
        [SerializeField] private Transform center;
        [SerializeField, Min(.001f)] private float localRadius = 2.38f;
        public bool IsConfigured => center != null;
        internal Vector3 WorldCenter => center.position;

        internal bool Covers(Camera camera, Vector3 worldPoint)
        {
            if (!TryProject(camera, out var uv, out var radius)) return false;
            var point = camera.WorldToScreenPoint(worldPoint);
            var centerInPixels = new Vector2(uv.x * Screen.width, uv.y * Screen.height);
            float radiusInPixels = radius * Screen.width;
            return point.z > 0 && ((Vector2)point - centerInPixels).sqrMagnitude <= radiusInPixels * radiusInPixels;
        }

        // The existing cloth graph measures distance in isotropic screen-width units.
        // Project the authored lens geometry every frame; no phone-specific UV offset/size.
        public bool TryProject(Camera camera, out Vector2 uv, out float radius)
        {
            uv = default; radius = 0;
            if (center == null || camera == null || Screen.width <= 0 || Screen.height <= 0) return false;
            var point = camera.WorldToScreenPoint(center.position);
            var right = camera.WorldToScreenPoint(center.position + transform.TransformVector(Vector3.right * localRadius));
            var up = camera.WorldToScreenPoint(center.position + transform.TransformVector(Vector3.up * localRadius));
            if (point.z <= 0) return false;
            uv = new Vector2(point.x / Screen.width, point.y / Screen.height);
            radius = Mathf.Min(Vector2.Distance(point, right), Vector2.Distance(point, up)) / Screen.width;
            return radius > 0;
        }
    }
}
