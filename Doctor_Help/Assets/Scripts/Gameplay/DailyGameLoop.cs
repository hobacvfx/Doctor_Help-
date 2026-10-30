using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DoctorHelp
{
    // Composition root: builds services/states and forwards requests; UI remains scene-owned.
    public sealed class DailyGameLoop : MonoBehaviour
    {
        [SerializeField, Min(1)] private float operatingSeconds = 720;
        public ClinicClock Clock { get; } = new ClinicClock();
        private bool applicationPaused, applicationUnfocused, skipClockFrame;
        public DayStep? Step => machine?.CurrentStep;
        public bool IsTransitioning => machine != null && machine.IsTransitioning;
        public string SelectedPill => treatment?.SelectedMedicineId;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private DayFlowMachine machine;
        private UnityDaySceneService scenes;
        private TreatmentDecisionState treatment;
        private IDayFlowView retryView;

        private async void Start()
        {
            try
            {
                var profile = PlayerProfileService.Instance;
                scenes = new UnityDaySceneService();
                if (profile == null || !profile.IsLoaded)
                {
                    var home = await scenes.LoadAsync(GameScene.Home, lifetime.Token);
                    home.ShowFlowError("Không thể tải profile. Dữ liệu cũ được giữ nguyên. Kiểm tra Console và mở lại game.");
                    return;
                }
                // Explicit order; changing enum numeric values does not change the flow.
                var plan = new DayFlowPlan(DayStep.Preparation, DayStep.Examination,
                    DayStep.TreatmentDecision, DayStep.PatientDeparture, DayStep.DayResults);
                plan.RepeatWhile(DayStep.PatientDeparture, DayStep.Examination, () => !Clock.IsClosed);
                var patients = new PatientService(scenes, Resources.Load<NpcCatalog>("NpcCatalog"));
                treatment = new TreatmentDecisionState(scenes, profile, RequestAdvance);
                machine = new DayFlowMachine(plan,
                    new PreparationState(scenes, profile, OpenClinic),
                    new ExaminationState(scenes, profile, patients, RequestAdvance), treatment,
                    new PatientDepartureState(scenes, profile, patients),
                    new DayResultsState(scenes, profile, new DayCompletionService(profile), RequestAdvance));
                await RunAsync(() => machine.StartAsync(lifetime.Token));
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
            catch (Exception error) { Debug.LogException(error); }
        }

        private void Update()
        {
            Clock.Tick(skipClockFrame ? 0 : Time.unscaledDeltaTime, applicationPaused || applicationUnfocused);
            skipClockFrame = false;
        }
        private void OnApplicationPause(bool paused) { applicationPaused = paused; skipClockFrame = true; }
        private void OnApplicationFocus(bool focused) { applicationUnfocused = !focused; skipClockFrame = true; }
        private void OpenClinic()
        {
            if (machine == null || machine.IsTransitioning || machine.FailedStep.HasValue || Step != DayStep.Preparation) return;
            Clock.Open(operatingSeconds);
            RequestAdvance();
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
        private async Task RunAsync(Func<Task<bool>> operation)
        {
            DetachRetry();
            try
            {
                bool success = await operation();
                // Arrival/departure awaits finish before another transition is requested.
                // The plan decides whether to admit another patient only after walk-out completes.
                if (success && machine.CurrentStep == DayStep.PatientDeparture)
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
        private void OnDestroy()
        {
            lifetime.Cancel();
            DetachRetry();
            try { machine?.Stop(); }
            finally { lifetime.Dispose(); }
        }
    }
}


