using UnityEngine;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    public sealed class PlayerHomeCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 offset = new Vector2(0f, 6.5f);
        [SerializeField, Min(0.01f)] private float smoothTime = 0.18f;

        private Vector2 velocity;

        private void OnEnable() => SnapToTarget();

        internal void SnapToTarget()
        {
            velocity = Vector2.zero;
            if (target != null)
                Place((Vector2)target.position + offset);
        }

        private void LateUpdate()
        {
            if (target != null)
                Place(Vector2.SmoothDamp(transform.position, (Vector2)target.position + offset,
                    ref velocity, smoothTime));
        }

        private void Place(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }
    }
}
