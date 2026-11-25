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
        private bool tutorialFocus;
        private Vector2 focusStart, focusTarget;
        private float focusElapsed, focusDuration;
        internal bool HasTutorialFocus => tutorialFocus;
        internal bool TutorialFocusComplete => tutorialFocus && focusElapsed >= focusDuration;

        internal void BeginTutorialFocus(Vector2 position, float seconds)
        {
            focusStart = transform.position;
            focusTarget = position;
            focusElapsed = 0f;
            focusDuration = Mathf.Max(.01f, seconds);
            tutorialFocus = true;
            velocity = Vector2.zero;
        }

        internal void EndTutorialFocus()
        {
            tutorialFocus = false;
            velocity = Vector2.zero;
        }

        private void OnEnable() => SnapToTarget();

        internal void SnapToTarget()
        {
            EndTutorialFocus();
            if (target != null)
                Place((Vector2)target.position + offset);
        }

        private void LateUpdate()
        {
            if (tutorialFocus)
            {
                AdvanceTutorialFocus(Time.unscaledDeltaTime);
                return;
            }
            if (target != null)
                Place(Vector2.SmoothDamp(transform.position, (Vector2)target.position + offset,
                    ref velocity, smoothTime));
        }

        private void AdvanceTutorialFocus(float seconds)
        {
            focusElapsed = Mathf.Min(focusDuration, focusElapsed + seconds);
            Place(Vector2.Lerp(focusStart, focusTarget, Mathf.SmoothStep(0f, 1f, focusElapsed / focusDuration)));
        }

        private void OnDisable() => EndTutorialFocus();

        private void Place(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }
    }
}
