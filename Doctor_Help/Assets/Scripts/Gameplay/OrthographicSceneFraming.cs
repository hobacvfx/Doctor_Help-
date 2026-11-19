using UnityEngine;

namespace DoctorHelp
{
    [DefaultExecutionOrder(-300), RequireComponent(typeof(Camera))]
    public sealed class OrthographicSceneFraming : MonoBehaviour
    {
        [SerializeField, Min(1)] private float minimumWidth = 6.4f;
        [SerializeField, Min(1)] private float minimumHeight = 10;
        [SerializeField] private SpriteRenderer backdrop;
        private Camera sceneCamera;
        private Vector3 backdropScale;
        private Bounds backdropBounds;
        private void Awake()
        {
            sceneCamera = GetComponent<Camera>();
            if (backdrop != null) { backdropScale = backdrop.transform.localScale; backdropBounds = backdrop.bounds; }
            Frame();
        }
        private void LateUpdate() => Frame();
        private void Frame()
        {
            if (sceneCamera == null || !sceneCamera.orthographic || sceneCamera.aspect <= 0) return;
            sceneCamera.orthographicSize = Mathf.Max(minimumHeight / 2, minimumWidth / (2 * sceneCamera.aspect));
            if (backdrop == null || backdropBounds.size.x <= 0 || backdropBounds.size.y <= 0) return;
            float height = 2 * sceneCamera.orthographicSize;
            var delta = sceneCamera.transform.position - backdropBounds.center;
            float factor = Mathf.Max(1, (height * sceneCamera.aspect + 2 * Mathf.Abs(delta.x)) / backdropBounds.size.x,
                (height + 2 * Mathf.Abs(delta.y)) / backdropBounds.size.y);
            backdrop.transform.localScale = backdropScale * factor;
        }
    }
}
