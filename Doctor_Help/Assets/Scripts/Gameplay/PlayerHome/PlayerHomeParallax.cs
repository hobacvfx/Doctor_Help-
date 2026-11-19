using UnityEngine;
using UnityEngine.Serialization;

namespace DoctorHelp
{
    [DefaultExecutionOrder(50), DisallowMultipleComponent]
    public sealed class PlayerHomeParallax : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [FormerlySerializedAs("horizontalFollow")]
        [Tooltip("Horizontal offset per unit of camera movement. Positive shifts foreground against the camera, negative lets distant backgrounds follow it, and zero anchors the layer to the scene.")]
        [SerializeField, Range(-1f, 1f)] private float horizontalStrength = 0.16f;

        private float originX;
        private float cameraOriginX;

        private void Start()
        {
            originX = transform.position.x;
            if (cameraTransform != null)
                cameraOriginX = cameraTransform.position.x;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null) return;

            var position = transform.position;
            position.x = originX - (cameraTransform.position.x - cameraOriginX) * horizontalStrength;
            transform.position = position;
        }
    }
}
