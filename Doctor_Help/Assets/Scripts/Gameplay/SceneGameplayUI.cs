using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class SceneGameplayUI : MonoBehaviour, IDayFlowView, IPatientMotionView
    {
        [SerializeField] private ExaminationToolSelection examinationTools;
        [SerializeField] private OfficeHaunting officeHaunting;
        internal OfficeHaunting OfficeHaunting => officeHaunting;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private Text statusText;
        [SerializeField] private Text clockText;
        [SerializeField] private ClinicClockPeriodIcon clockPeriodIcon;
        [SerializeField] private Text waitingForPatientText;
        [SerializeField, Min(0)] private float waitingForPatientFadeSeconds = 0.6f;
        [SerializeField] private Button endDayButton;
        private DailyGameLoop gameLoop;
        [SerializeField] private Text errorText;
        [SerializeField] private DeskActionButton openButton;
        [SerializeField] private HomeRoomInteractions homeRoom;
        [SerializeField] private DeskActionButton givePillsButton;
        [SerializeField] private SelectPillPopup pillPopup;
        [SerializeField] private PoliceReportPopup policePopup;
        [SerializeField] private ResultsVideoPresenter resultsVideo;
        [SerializeField] private SceneTransitionFade sceneFade;
        [SerializeField] private Headbar headbar;
        public event Action OpenRequested;
        public event Action TreatmentRequested;
        public event Action DecisionConfirmed;
        public event Action DeclineRequested;
        public event Action EndDayRequested;
        public event Action RetryRequested;
        public event Action CancelTreatmentRequested;
        public System.Collections.Generic.IReadOnlyList<string> SelectedMedicineIds => pillPopup.SelectedMedicineIds;
        public TreatmentDecisionKind DecisionKind { get; private set; }
        public PoliceReportReason SelectedReportReason => policePopup != null ? policePopup.SelectedReason : PoliceReportReason.None;
        public void ShowTreatmentError(string message)
        {
            if (DecisionKind == TreatmentDecisionKind.ReportToPolice) policePopup.ShowError(message);
            else pillPopup.ShowError(message);
        }
        public DeskActionButton OpenButton => openButton;
        public DeskActionButton GivePillsButton => givePillsButton;
        public SelectPillPopup PillPopup => pillPopup;
        public bool IsConfigured => safeArea != null && statusText != null && errorText != null && sceneFade != null && sceneFade.IsConfigured &&
            ((openButton != null && openButton.IsConfigured && resultsVideo != null && resultsVideo.IsConfigured &&
                homeRoom != null && homeRoom.IsConfigured) ||
             (givePillsButton != null && givePillsButton.IsConfigured && pillPopup != null && pillPopup.IsConfigured && clockText != null && clockPeriodIcon != null && waitingForPatientText != null &&
                endDayButton != null && endDayButton.targetGraphic != null && policePopup != null && policePopup.IsConfigured));
        private bool retryMode;
        private DeskActionButton retryLabel;
        private string originalRetryLabel;
        private Coroutine waitingForPatientFade;
        private string waitingForPatientLabel;
        private const string ClientLimitMessage = "Maximum client reached, increase rating to get more client.";

        private void Awake()
        {
            if (openButton != null) openButton.Clicked += OnOpen;
            if (givePillsButton != null) givePillsButton.Clicked += OnTreatment;
            if (pillPopup != null) pillPopup.GiveRequested += OnGive;
            if (pillPopup != null) pillPopup.CloseRequested += OnCancelTreatment;
            if (policePopup != null) { policePopup.ReportRequested += OnConfirmDecision; policePopup.CloseRequested += OnCancelTreatment; }
            if (examinationTools != null) examinationTools.ToolSelected += OnToolSelected;
            if (endDayButton != null) endDayButton.onClick.AddListener(OnEndDay);
            HideAll();
        }
        private void Update()
        {
            if (clockText != null && clockText.gameObject.activeSelf)
            {
                if (gameLoop == null) gameLoop = FindFirstObjectByType<DailyGameLoop>();
                if (gameLoop != null)
                {
                    clockText.text = gameLoop.Clock.DisplayTime;
                    if (clockPeriodIcon != null) clockPeriodIcon.SetMinute(gameLoop.Clock.MinuteOfDay);
                }
            }
            ScreenSafeArea.ApplyTo(safeArea);
        }
        private void OnOpen() { if (retryMode) RetryRequested?.Invoke(); else OpenRequested?.Invoke(); }
        private void OnTreatment()
        {
            if (retryMode) RetryRequested?.Invoke();
            else { DecisionKind = TreatmentDecisionKind.GiveMedicine; TreatmentRequested?.Invoke(); }
        }
        private void OnToolSelected(ExaminationToolKind kind)
        {
            if (kind != ExaminationToolKind.Telephone) return;
            DecisionKind = TreatmentDecisionKind.ReportToPolice;
            TreatmentRequested?.Invoke();
        }
        private void OnGive() => OnConfirmDecision();
        private void OnDecline() => DeclineRequested?.Invoke();
        private void OnEndDay()
        {
            if (EndDayRequested == null) return;
            endDayButton.interactable = false;
            EndDayRequested.Invoke();
        }
        private void OnConfirmDecision() => DecisionConfirmed?.Invoke();
        private void OnCancelTreatment() => CancelTreatmentRequested?.Invoke();
        public void HideAll()
        {
            if (this == null) return;
            if (headbar != null) headbar.CloseSettings();
            if (TryGetComponent<StoreInventoryUI>(out var inventory)) inventory.Close();
            if (examinationTools != null) examinationTools.SetAvailable(false);
            if (officeHaunting != null) officeHaunting.SetAvailable(false);
            retryMode = false;
            if (retryLabel != null) retryLabel.Label = originalRetryLabel;
            retryLabel = null;
            if (homeRoom != null) homeRoom.gameObject.SetActive(false);
            if (openButton != null) openButton.gameObject.SetActive(false);
            // The desk button stays in place while patient motion locks its action.
            if (givePillsButton != null)
            {
                givePillsButton.gameObject.SetActive(true);
                givePillsButton.Interactable = false;
            }
            if (pillPopup != null) pillPopup.gameObject.SetActive(false);
            if (policePopup != null) policePopup.gameObject.SetActive(false);
            if (resultsVideo != null) { resultsVideo.StopPlayback(); resultsVideo.gameObject.SetActive(false); }
            if (errorText != null) errorText.gameObject.SetActive(false);
            if (statusText != null) statusText.gameObject.SetActive(false);
            if (clockText != null) clockText.gameObject.SetActive(false);
            if (waitingForPatientFade != null)
            {
                StopCoroutine(waitingForPatientFade);
                waitingForPatientFade = null;
            }
            if (waitingForPatientText != null) waitingForPatientText.gameObject.SetActive(false);
            if (endDayButton != null) endDayButton.gameObject.SetActive(false);
        }
        public void ShowPreparation(int day)
        {
            if (openButton == null) throw new InvalidOperationException("Preparation requires a Player Home Open button.");
            HideAll(); openButton.gameObject.SetActive(true);
            openButton.Interactable = true;
            if (homeRoom != null) homeRoom.gameObject.SetActive(true);
        }
        public void ShowPreparationNavigationError(string message)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }
        public Task RevealSceneAsync(CancellationToken token) => sceneFade.RevealAsync(token);
        public Task FadeToBlackAsync(CancellationToken token) => sceneFade.CoverAsync(token);
        public void ShowExamination(int day)
        {
            DecisionKind = TreatmentDecisionKind.GiveMedicine;
            if (givePillsButton == null) throw new InvalidOperationException("Examination requires an Office treatment button.");
            HideAll(); ShowClock(); ShowEndDay(); givePillsButton.gameObject.SetActive(true);
            givePillsButton.Interactable = true;
            if (examinationTools != null) examinationTools.SetAvailable(true);
            if (officeHaunting != null) officeHaunting.SetAvailable(true);
        }
        public void ShowTreatment(int day)
        {
            if (DecisionKind == TreatmentDecisionKind.ReportToPolice)
            {
                if (policePopup == null) throw new InvalidOperationException("Police reporting requires a scene-owned popup.");
                HideAll(); ShowClock(); Status("Báo cảnh sát");
                policePopup.ResetSelection(); policePopup.gameObject.SetActive(true);
                return;
            }
            if (pillPopup == null) throw new InvalidOperationException("Treatment requires SelectPillPopup.");
            HideAll(); ShowClock();
            pillPopup.PrepareForPatient();
            pillPopup.gameObject.SetActive(true);
        }
        private void ShowClock()
        {
            if (clockText == null) throw new InvalidOperationException("Office requires a scene-owned clock label.");
            clockText.gameObject.SetActive(true);
        }
        private void ShowEndDay()
        {
            if (gameLoop == null) gameLoop = FindFirstObjectByType<DailyGameLoop>();
            endDayButton.gameObject.SetActive(true);
            endDayButton.interactable = gameLoop != null && !gameLoop.IsEndingDayEarly;
        }
        public void ShowPatientArrival(int day)
        {
            HideAll(); ShowClock(); ShowEndDay();
        }
        public void ShowPatientDeparture(int day)
        {
            HideAll(); ShowClock(); ShowEndDay();
        }
        public void ShowWaitingForPatient()
        {
            if (waitingForPatientLabel != null) waitingForPatientText.text = waitingForPatientLabel;
            ShowWaitingLabel();
        }
        public void ShowClientLimitReached(int secondsLeft)
        {
            // Reuses the waiting label so the notice keeps the Office's centered message style.
            waitingForPatientLabel ??= waitingForPatientText.text;
            waitingForPatientText.verticalOverflow = VerticalWrapMode.Overflow;
            waitingForPatientText.text = $"{ClientLimitMessage}\nDay end in {secondsLeft}";
            ShowWaitingLabel();
        }
        private void ShowWaitingLabel()
        {
            statusText.gameObject.SetActive(false);
            if (waitingForPatientText.gameObject.activeSelf) return;
            waitingForPatientText.gameObject.SetActive(true);
            var label = waitingForPatientText.canvasRenderer;
            float duration = waitingForPatientFadeSeconds;
            label.SetAlpha(duration > 0 ? 0 : 1);
            if (duration > 0) waitingForPatientFade = StartCoroutine(FadeWaitingLabel(label, duration));
        }
        private IEnumerator FadeWaitingLabel(CanvasRenderer label, float duration)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                yield return null;
                elapsed += Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
                label.SetAlpha(Mathf.Clamp01(elapsed / duration));
            }
            waitingForPatientFade = null;
        }

        public Task PresentResultsAsync(DayTreatmentResult result, bool afterCutscene, CancellationToken token)
        {
            if (resultsVideo == null) throw new InvalidOperationException("DayResults requires the Player Home video presenter.");
            HideAll(); resultsVideo.gameObject.SetActive(true);
            return resultsVideo.PlayAsync(result, afterCutscene, token);
        }
        public void ShowFlowError(string message)
        {
            if (sceneFade != null) sceneFade.Clear();
            HideAll(); errorText.text = message; errorText.gameObject.SetActive(true);
            if (RetryRequested == null) return;
            retryMode = true;
            var button = openButton != null ? openButton : givePillsButton;
            if (button == null) return;
            retryLabel = button;
            if (retryLabel != null) { originalRetryLabel = retryLabel.Label; retryLabel.Label = "Thử lại"; }
            button.gameObject.SetActive(true);
            button.Interactable = true;
        }
        private void Status(string step)
        {
            statusText.text = step;
            statusText.gameObject.SetActive(true);
        }
        private void OnDestroy()
        {
            if (openButton != null) openButton.Clicked -= OnOpen;
            if (givePillsButton != null) givePillsButton.Clicked -= OnTreatment;
            if (pillPopup != null) pillPopup.GiveRequested -= OnGive;
            if (pillPopup != null) pillPopup.CloseRequested -= OnCancelTreatment;
            if (policePopup != null) { policePopup.ReportRequested -= OnConfirmDecision; policePopup.CloseRequested -= OnCancelTreatment; }
            if (examinationTools != null) examinationTools.ToolSelected -= OnToolSelected;
            if (endDayButton != null) endDayButton.onClick.RemoveListener(OnEndDay);
        }
    }
}




