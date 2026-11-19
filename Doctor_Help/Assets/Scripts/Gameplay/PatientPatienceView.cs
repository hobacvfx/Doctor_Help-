using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class PatientPatienceView : MonoBehaviour
    {
        [SerializeField] private RectTransform widget;
        [SerializeField] private CanvasGroup visibility;
        [SerializeField] private Image fill;
        [SerializeField] private Image moodIcon;
        [SerializeField] private Sprite happyFace, neutralFace, angryFace;
        [SerializeField] private Text amount;
        [SerializeField] private PatientSpawnPoint patients;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RectTransform endDayControl;
        [SerializeField] private float gap = 24f;
        [SerializeField] private Color happyColor = new Color(.38f, .59f, .43f);
        [SerializeField] private Color neutralColor = new Color(.85f, .65f, .27f);
        [SerializeField] private Color angryColor = new Color(.78f, .29f, .23f);

        private Canvas canvas;
        private RectTransform parentRect;
        private PatientExaminationProfile shownPatient;
        private Vector3 headOffset;
        private int shownAmount = -1;
        private int shownStars;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>().rootCanvas;
            parentRect = (RectTransform)widget.parent;
            visibility.alpha = 0;
        }

        private void LateUpdate()
        {
            var patient = patients.CurrentPatient;
            if (patient == null || !patient.HasArrived || patients.IsDeparting)
            {
                visibility.alpha = 0;
                shownPatient = null;
                return;
            }

            if (shownPatient != patient)
            {
                shownPatient = patient;
                shownAmount = -1;
                shownStars = 0;
                // Sample the patient's idle silhouette once, before the dialogue opens.
                var heart = patient.Heart.position;
                float top = heart.y;
                foreach (var sprite in patient.GetComponentsInChildren<SpriteRenderer>())
                    if (sprite.enabled && sprite.sprite != null) top = Mathf.Max(top, sprite.bounds.max.y);
                headOffset = patient.transform.InverseTransformPoint(new Vector3(heart.x, top, heart.z));
            }

            var screen = worldCamera.WorldToScreenPoint(patient.transform.TransformPoint(headOffset));
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (screen.z <= 0 || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screen, uiCamera, out var point))
            {
                visibility.alpha = 0;
                return;
            }
            // Keep the bar above the patient while leaving the top-right End Day control clear.
            var controlLeft = endDayControl.TransformPoint(new Vector3(endDayControl.rect.xMin, 0));
            float maxX = parentRect.InverseTransformPoint(controlLeft).x - widget.rect.xMax - 8f;
            widget.localPosition = new Vector3(Mathf.Min(point.x, maxX), point.y + gap);
            visibility.alpha = 1;
            float patience = patient.Patience;
            fill.fillAmount = patience / 100f;
            int stars = patient.PatienceStars;
            if (shownStars != stars)
            {
                shownStars = stars;
                (fill.color, moodIcon.sprite) = stars switch
                {
                    5 => (happyColor, happyFace),
                    >= 3 => (neutralColor, neutralFace),
                    _ => (angryColor, angryFace)
                };
            }
            int rounded = patience > 0f ? Mathf.Max(1, Mathf.FloorToInt(patience)) : 0;
            if (shownAmount != rounded)
            {
                shownAmount = rounded;
                amount.text = rounded.ToString();
            }
        }
    }
}
