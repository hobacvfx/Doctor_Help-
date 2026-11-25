using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class PoliceReportCard : MonoBehaviour
    {
        [SerializeField] private RectTransform face;
        [SerializeField] private GameObject selectedBorder;
        [SerializeField] private Shadow raisedShadow;
        [SerializeField] private Vector2 pressedOffset = new Vector2(0, -4);
        [SerializeField, Range(.8f, 1)] private float pressedScale = .98f;
        [SerializeField, Min(.01f)] private float pressSeconds = .12f;
        private Vector2 restPosition;
        private Vector3 restScale;
        private bool captured, selected;
        private float progress;

        public bool IsConfigured => face != null && selectedBorder != null && raisedShadow != null;

        public void SetSelected(bool value)
        {
            CapturePose();
            selected = value;
            selectedBorder.SetActive(value);
            if (!isActiveAndEnabled) { progress = value ? 1 : 0; ApplyPose(); }
        }

        private void CapturePose()
        {
            if (captured) return;
            restPosition = face.anchoredPosition;
            restScale = face.localScale;
            captured = true;
        }

        private void OnEnable()
        {
            CapturePose();
            progress = selected ? 1 : 0;
            selectedBorder.SetActive(selected);
            ApplyPose();
        }

        private void Update()
        {
            float target = selected ? 1 : 0;
            if (Mathf.Approximately(progress, target)) return;
            progress = Mathf.MoveTowards(progress, target, Time.unscaledDeltaTime / pressSeconds);
            ApplyPose();
        }

        private void ApplyPose()
        {
            float eased = Mathf.SmoothStep(0, 1, progress);
            face.anchoredPosition = restPosition + pressedOffset * eased;
            face.localScale = restScale * Mathf.Lerp(1, pressedScale, eased);
            raisedShadow.effectDistance = Vector2.Lerp(new Vector2(0, -3), new Vector2(0, -1), eased);
        }
    }
}
