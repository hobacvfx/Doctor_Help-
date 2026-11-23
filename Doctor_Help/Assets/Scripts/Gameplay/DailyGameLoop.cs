using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DoctorHelp
{
    // Composition root: builds services/states and forwards requests; UI remains scene-owned.
    public sealed class DailyGameLoop : MonoBehaviour
    {
        private const string OpeningCompletedKey = "DoctorHelp.OpeningComic.Completed";
        [SerializeField, Min(1)] private float operatingSeconds = ClinicClock.DefaultOperatingSeconds;
        public ClinicClock Clock { get; } = new ClinicClock();
        private bool applicationPaused, applicationUnfocused, skipClockFrame;
        public DayStep? Step => machine?.CurrentStep;
        public bool IsTransitioning => (machine != null && machine.IsTransitioning) ||
            (preparation != null && preparation.IsNavigating);
        internal bool SkipOnboarding { get; set; }
        public System.Collections.Generic.IReadOnlyList<string> SelectedPills => treatment?.SelectedMedicineIds;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private DayFlowMachine machine;
        private UnityDaySceneService scenes;
        private PreparationState preparation;
        private TreatmentDecisionState treatment;
        private DayCompletionService completion;
        public DayTreatmentResult LatestDayResult => completion?.LatestResult;
        private IDayFlowView retryView;
        private bool initialized, resetting;
        internal bool IsEndingDayEarly { get; private set; }
        // Set when today's rating-based client limit is reached; the day then ends after a countdown.
        private bool clientLimitReached;
        // Ghosts are counted when the clinic opens, before any patient is examined.
        private float ghostRatingPenalty;
        private bool AcceptingPatients => !IsEndingDayEarly && !clientLimitReached && !Clock.IsClosed;
        public bool CanResetProfile => initialized && !resetting && !IsTransitioning;

        private async void Start()
        {
            try
            {
                var profile = PlayerProfileService.Instance;
                scenes = new UnityDaySceneService();
                if (profile == null || !profile.IsLoaded)
                {
                    var home = await scenes.LoadAsync(GameScene.Player_Home, lifetime.Token);
                    home.ShowFlowError("Không thể tải profile. Dữ liệu cũ được giữ nguyên. Kiểm tra Console và mở lại game.");
                    return;
                }
                await PresentOpeningAsync();
                await StartFlowAsync(profile);
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
            catch (Exception error) { Debug.LogException(error); }
            finally { initialized = true; }
        }

        private async Task PresentOpeningAsync(bool reset = false)
        {
            bool preview = false;
#if UNITY_EDITOR
            if (SkipOnboarding) return;
            preview = TutorialPreviewSession.IsActive;
#endif
            if (reset && !preview)
            {
                PlayerPrefs.DeleteKey(OpeningCompletedKey);
                PlayerPrefs.Save();
            }
            var profile = PlayerProfileService.Instance;
            bool playComic = preview || PlayerPrefs.GetInt(OpeningCompletedKey) == 0;
            bool needsName = string.IsNullOrWhiteSpace(profile.ClinicName);
            if (!playComic && !needsName) return;

            var home = await scenes.LoadAsync(GameScene.Player_Home, lifetime.Token);
            if (playComic)
            {
                var comic = ((SceneGameplayUI)home).GetComponentInChildren<OpeningComicView>(true);
                if (comic == null || !comic.IsConfigured)
                    throw new InvalidOperationException("Player Home is missing its configured opening comic prefab.");
                await comic.PlayAsync(lifetime.Token);
                if (!preview)
                {
                    PlayerPrefs.SetInt(OpeningCompletedKey, 1);
                    PlayerPrefs.Save();
                }
            }
            if (needsName)
            {
                var naming = ((SceneGameplayUI)home).GetComponentInChildren<ClinicNamingView>(true);
                if (naming == null || !naming.IsConfigured)
                    throw new InvalidOperationException("Player Home is missing its configured clinic naming prefab.");
                await naming.PresentAsync(profile, lifetime.Token);
            }
        }

        private Task StartFlowAsync(PlayerProfileService profile)
        {
            IsEndingDayEarly = false;
            clientLimitReached = false;
            // Explicit order; changing enum numeric values does not change the flow.
            var plan = new DayFlowPlan(DayStep.Preparation, DayStep.Examination,
                DayStep.TreatmentDecision, DayStep.PatientDeparture, DayStep.DayResults);
            var patients = new PatientService(scenes, Resources.Load<NpcCatalog>("NpcCatalog"), profile);
            var examination = new ExaminationState(scenes, profile, patients, RequestAdvance, RequestEndDay,
                () => profile.EnsureClinicDirtDay(),
                patient => profile.TryRecordRockHit(patient, out var error) ? null : error);
            plan.SkipWhile(DayStep.Examination, DayStep.PatientDeparture, () => IsEndingDayEarly || examination.RockHitRecorded);
            plan.RepeatWhile(DayStep.PatientDeparture, DayStep.Examination, () => AcceptingPatients);
            plan.AllowReturn(DayStep.TreatmentDecision, DayStep.Examination);
            var pills = Resources.Load<PlayerStartingProfile>("PlayerStartingProfile")?.Pills;
            treatment = new TreatmentDecisionState(scenes, profile, RequestAdvance, RequestReturn,
                ids => profile.TryGive(patients.CurrentPatient, pills, ids, out var error) ? null : error,
                reason => profile.TryReportToPolice(patients.CurrentPatient, reason, out var error) ? null : error,
                () => profile.TryDecline(patients.CurrentPatient, out var error) ? null : error);
            preparation = new PreparationState(scenes, profile, OpenClinic);
            completion = new DayCompletionService(profile,
                day =>
                {
                    var result = TreatmentEvaluator.Evaluate(day, profile.GetTreatmentSnapshot(), patients.MedicalData,
                        profile.Money, profile.ClinicDirtPenalty(), profile.ReceptionRatingBonus,
                        profile.BusinessGoldMultiplier, ghostRatingPenalty,
                        (int)Math.Floor(profile.PlaySeconds / 60d * profile.GoldPerMinute));
                    result.ClientLimitReached = clientLimitReached;
                    return result;
                });
            machine = new DayFlowMachine(plan,
                preparation,
                examination, treatment,
                new PatientDepartureState(scenes, profile, patients, RequestEndDay, Clock, () => AcceptingPatients,
                    () => profile.ClinicSignLevel, () => profile.Rating, () => clientLimitReached = true),
                new DayResultsState(scenes, profile, completion,
                    GetComponent<DayEndInterstitialService>()));
            return RunAsync(() => machine.StartAsync(lifetime.Token));
        }

        public async Task<string> ResetProfileAsync()
        {
            if (!CanResetProfile) return "Please wait for the current action to finish.";
            var profile = PlayerProfileService.Instance;
            resetting = true;
            try
            {
                if (profile == null || !profile.TryReset()) return "Could not reset the player profile. Please try again.";
                DetachRetry();
                machine?.Stop();
                Clock.Stop();
                GetComponent<FirstRunTutorial>()?.RestartAfterProfileReset();
                await PresentOpeningAsync(reset: true);
                await StartFlowAsync(profile);
                return null;
            }
            finally { resetting = false; }
        }

        private void Update()
        {
            // Furniture gold per minute counts active play only; pause and background are excluded.
            if (!skipClockFrame && !applicationPaused && !applicationUnfocused)
                PlayerProfileService.Instance?.AddPlaySeconds(Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime));
            Clock.Tick(skipClockFrame ? 0 : Time.unscaledDeltaTime,
                applicationPaused || applicationUnfocused || FirstRunTutorial.Instance?.PausesClinicClock == true);
            skipClockFrame = false;
        }
        private void OnApplicationPause(bool paused) { applicationPaused = paused; skipClockFrame = true; }
        private void OnApplicationFocus(bool focused) { applicationUnfocused = !focused; skipClockFrame = true; }
        private void OpenClinic()
        {
            if (machine == null || machine.IsTransitioning || machine.FailedStep.HasValue || Step != DayStep.Preparation) return;
            IsEndingDayEarly = false;
            clientLimitReached = false;
            var profile = PlayerProfileService.Instance;
            ghostRatingPenalty = profile != null && profile.IsLoaded ? ClinicDemand.HomeGhostRatingPenalty(profile.GhostNumber) : 0f;
            if (profile != null && profile.IsLoaded) profile.BeginWorkingDay();
            GetComponent<FirstRunTutorial>()?.OnClinicOpened();
            Clock.Open(operatingSeconds);
            RequestAdvance();
        }

        private void RequestEndDay()
        {
            if (IsEndingDayEarly || machine == null || machine.FailedStep.HasValue || lifetime.IsCancellationRequested) return;
            IsEndingDayEarly = true;
            Clock.Stop();
            GetComponent<FirstRunTutorial>()?.OnClinicClosed();
            // If a patient is already walking, finish that motion before following the plan.
            if (!machine.IsTransitioning) RequestAdvance();
        }

        private async void RequestAdvance()
        {
            if (machine == null || machine.IsTransitioning || lifetime.IsCancellationRequested) return;
            await RunAsync(() => machine.AdvanceAsync(lifetime.Token));
        }
        private async void RequestRetry()
        {
            if (machine == null || machine.IsTransitioning || lifetime.IsCancellationRequested) return;
            await RunAsync(() => machine.RetryAsync(lifetime.Token));
        }
        private async void RequestReturn()
        {
            if (machine == null || machine.IsTransitioning || lifetime.IsCancellationRequested || Step != DayStep.TreatmentDecision) return;
            await RunAsync(() => machine.ReturnAsync(lifetime.Token));
        }
        private async Task RunAsync(Func<Task<bool>> operation)
        {
            DetachRetry();
            try
            {
                bool success = await operation();
                // Arrival/departure awaits finish before another transition is requested.
                // The plan decides whether to admit another patient only after walk-out completes.
                while (success && (Step == DayStep.PatientDeparture || Step == DayStep.DayResults ||
                    (Step == DayStep.Examination && IsEndingDayEarly)))
                    success = await machine.AdvanceAsync(lifetime.Token);
                if (success && (machine.CurrentStep == DayStep.DayResults || machine.CurrentStep == DayStep.Preparation))
                    Clock.Stop();
                if (lifetime.IsCancellationRequested || success || machine.LastError == null) return;
                Debug.LogException(machine.LastError);
                retryView = scenes.CurrentView;
                if (retryView == null) return;
                retryView.RetryRequested += RequestRetry;
                retryView.ShowFlowError("Không thể chuyển bước: " + machine.LastError.Message);
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
            catch (Exception error) { Debug.LogException(error); }
        }
        private void DetachRetry()
        {
            if (retryView != null) retryView.RetryRequested -= RequestRetry;
            retryView = null;
        }
        // Leaving Play Mode (or quitting) disables the scene fade mid-transition; that cancellation is expected,
        // so cancel the loop first and RunAsync treats it as cancellation instead of logging a flow error.
        private void OnEnable() => Application.quitting += CancelOnQuit;
        private void OnDisable() => Application.quitting -= CancelOnQuit;
        private void CancelOnQuit()
        {
            if (!lifetime.IsCancellationRequested) lifetime.Cancel();
        }
        private void OnDestroy()
        {
            lifetime.Cancel();
            DetachRetry();
            try { machine?.Stop(); }
            finally { lifetime.Dispose(); }
        }
    }
}


