using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class SceneGameplayUI : MonoBehaviour, IDayFlowView, IPatientMotionView
    {
        [SerializeField] private ExaminationToolSelection examinationTools;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private Text statusText;
        [SerializeField] private Text clockText;
        private DailyGameLoop gameLoop;
        [SerializeField] private Text errorText;
        [SerializeField] private Button openButton;
        [SerializeField] private Button givePillsButton;
        [SerializeField] private SelectPillPopup pillPopup;
        [SerializeField] private GameObject resultsPopup;
        [SerializeField] private bool usePlaceholderResultsTimer = true;
        [SerializeField, Min(0)] private float placeholderResultsSeconds = 3f;
        public event Action OpenRequested;
        public event Action TreatmentRequested;
        public event Action GiveRequested;
        public event Action ResultsCompleted;
        public event Action RetryRequested;
        public Button OpenButton => openButton;
        public Button GivePillsButton => givePillsButton;
        public SelectPillPopup PillPopup => pillPopup;
        public string SelectedMedicineId => pillPopup != null ? pillPopup.SelectedMedicineId : null;
        public bool IsConfigured => safeArea != null && statusText != null && errorText != null &&
            ((openButton != null && resultsPopup != null) ||
             (givePillsButton != null && pillPopup != null && pillPopup.IsConfigured && clockText != null));
        private bool retryMode;
        private bool presentingResults;
        private Coroutine resultsRoutine;
        private Text retryLabel;
        private string originalRetryLabel;

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(OnOpen);
            if (givePillsButton != null) givePillsButton.onClick.AddListener(OnTreatment);
            if (pillPopup != null) pillPopup.GiveRequested += OnGive;
            HideAll();
        }
        private void Update()
        {
            if (clockText != null && clockText.gameObject.activeSelf)
            {
                if (gameLoop == null) gameLoop = FindFirstObjectByType<DailyGameLoop>();
                if (gameLoop != null) clockText.text = gameLoop.Clock.DisplayTime;
            }
            if (safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect area = Screen.safeArea;
            safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        }
        private void OnOpen() { if (retryMode) RetryRequested?.Invoke(); else OpenRequested?.Invoke(); }
        private void OnTreatment() { if (retryMode) RetryRequested?.Invoke(); else TreatmentRequested?.Invoke(); }
        private void OnGive() => GiveRequested?.Invoke();
        public void HideAll()
        {
            if (this == null) return;
            if (examinationTools != null) examinationTools.SetAvailable(false);
            if (resultsRoutine != null) StopCoroutine(resultsRoutine);
            resultsRoutine = null;
            presentingResults = false;
            retryMode = false;
            if (retryLabel != null) retryLabel.text = originalRetryLabel;
            retryLabel = null;
            if (openButton != null) openButton.gameObject.SetActive(false);
            if (givePillsButton != null) givePillsButton.gameObject.SetActive(false);
            if (pillPopup != null) pillPopup.gameObject.SetActive(false);
            if (resultsPopup != null) resultsPopup.SetActive(false);
            if (errorText != null) errorText.gameObject.SetActive(false);
            if (statusText != null) statusText.gameObject.SetActive(false);
            if (clockText != null) clockText.gameObject.SetActive(false);
        }
        public void ShowPreparation(int day)
        {
            if (openButton == null) throw new InvalidOperationException("Preparation requires a Home Open button.");
            HideAll(); Status(day, "Chuẩn bị mở cửa"); openButton.gameObject.SetActive(true);
        }
        public void ShowExamination(int day)
        {
            if (givePillsButton == null) throw new InvalidOperationException("Examination requires an Office treatment button.");
            HideAll(); ShowClock(); Status(day, "Khám bệnh"); givePillsButton.gameObject.SetActive(true);
            if (examinationTools != null) examinationTools.SetAvailable(true);
        }
        public void ShowTreatment(int day)
        {
            if (pillPopup == null) throw new InvalidOperationException("Treatment requires SelectPillPopup.");
            HideAll(); ShowClock(); Status(day, "Cho thuốc"); pillPopup.ResetSelection(); pillPopup.gameObject.SetActive(true);
        }
        private void ShowClock()
        {
            if (clockText == null) throw new InvalidOperationException("Office requires a scene-owned clock label.");
            clockText.gameObject.SetActive(true);
        }
        public void ShowPatientArrival(int day)
        {
            HideAll(); ShowClock(); Status(day, "Bệnh nhân bước vào");
        }
        public void ShowPatientDeparture(int day)
        {
            HideAll(); ShowClock(); Status(day, "Bệnh nhân ra về");
        }
        public void ShowResults()
        {
            if (resultsPopup == null) throw new InvalidOperationException("DayResults requires a results view.");
            HideAll(); presentingResults = true; resultsPopup.SetActive(true);
            if (usePlaceholderResultsTimer) resultsRoutine = StartCoroutine(PlaceholderResults());
        }
        private IEnumerator PlaceholderResults()
        {
            yield return new WaitForSecondsRealtime(placeholderResultsSeconds);
            resultsRoutine = null;
            CompleteResultsPresentation();
        }
        // Future cutscene/results UI calls this with the placeholder timer disabled in the Inspector.
        public void CompleteResultsPresentation()
        {
            if (!presentingResults) return;
            presentingResults = false;
            ResultsCompleted?.Invoke();
        }
        public void ShowFlowError(string message)
        {
            HideAll(); errorText.text = message; errorText.gameObject.SetActive(true);
            if (RetryRequested == null) return;
            retryMode = true;
            var button = openButton != null ? openButton : givePillsButton;
            if (button == null) return;
            retryLabel = button.GetComponentInChildren<Text>(true);
            if (retryLabel != null) { originalRetryLabel = retryLabel.text; retryLabel.text = "Thử lại"; }
            button.gameObject.SetActive(true);
        }
        private void Status(int day, string step)
        {
            statusText.text = $"Ngày {day} — {step}";
            statusText.gameObject.SetActive(true);
        }
        private void OnDestroy()
        {
            if (openButton != null) openButton.onClick.RemoveListener(OnOpen);
            if (givePillsButton != null) givePillsButton.onClick.RemoveListener(OnTreatment);
            if (pillPopup != null) pillPopup.GiveRequested -= OnGive;
        }
    }
}




