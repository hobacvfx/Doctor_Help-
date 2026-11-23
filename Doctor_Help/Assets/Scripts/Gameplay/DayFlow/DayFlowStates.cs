using System;
using System.Threading;
using System.Threading.Tasks;

namespace DoctorHelp
{
    public enum GameScene { Office, Player_Home }

    public interface IDayFlowView
    {
        event Action OpenRequested;
        event Action TreatmentRequested;
        event Action DecisionConfirmed;
        event Action DeclineRequested;
        event Action EndDayRequested;
        event Action RetryRequested;
        event Action CancelTreatmentRequested;
        System.Collections.Generic.IReadOnlyList<string> SelectedMedicineIds { get; }
        TreatmentDecisionKind DecisionKind { get; }
        PoliceReportReason SelectedReportReason { get; }
        void ShowTreatmentError(string message);
        void ShowPreparation(int day);
        void ShowPreparationNavigationError(string message);
        Task RevealSceneAsync(CancellationToken token);
        Task FadeToBlackAsync(CancellationToken token);
        void ShowExamination(int day);
        void ShowTreatment(int day);
        // afterCutscene: the in-game end-of-day cutscene already played, so only the newspaper and the new-day line remain.
        Task PresentResultsAsync(DayTreatmentResult result, bool afterCutscene, CancellationToken token);
        void HideAll();
        void ShowFlowError(string message);
    }

    public interface IDaySceneService
    {
        IDayFlowView CurrentView { get; }
        INeighborhoodView CurrentNeighborhood { get; }
        Task<IDayFlowView> LoadAsync(GameScene scene, CancellationToken cancellation);
        Task<INeighborhoodView> LoadNeighborhoodAsync(string destination, CancellationToken cancellation);
        // Loads Endday_Cutscene and plays it to black. False when it could not play (the day still goes on).
        Task<bool> PlayDayEndCutsceneAsync(DayTreatmentResult result, CancellationToken cancellation);
    }

    public interface INeighborhoodView
    {
        event Action<string> TravelRequested;
    }

    public interface IDayProfile
    {
        int CurrentDay { get; }
        bool CompleteDay(int incorrectPatients, int goldDelta, float? dailyRating, bool hadConsultations, int goldEarned,
            int patientsExamined);
    }

    public interface IPatientService { void EnsurePatient(); }
    public interface IAnimatedPatientService : IPatientService
    {
        bool HasRemainingPatients { get; }
        int ArrivedToday { get; }
        Task WaitForArrivalAsync(CancellationToken token);
        Task DepartAsync(CancellationToken token);
    }

    public interface IPatientMotionView
    {
        void ShowPatientArrival(int day);
        void ShowPatientDeparture(int day);
        void ShowWaitingForPatient();
        void ShowClientLimitReached(int secondsLeft);
    }

    // One checkpoint per completed day, even when results entry is retried after a scene failure.
    public sealed class DayCompletionService
    {
        private readonly IDayProfile profile;
        private int? completedDay;
        private readonly Func<int, DayTreatmentResult> evaluate;
        private DayTreatmentResult completedResult;
        public DayTreatmentResult LatestResult => completedResult;
        public DayCompletionService(IDayProfile profile, Func<int, DayTreatmentResult> evaluate)
        { this.profile = profile; this.evaluate = evaluate; }
        public DayTreatmentResult Complete(int day)
        {
            if (completedDay == day) return completedResult;
            if (profile.CurrentDay != day) throw new InvalidOperationException("Unexpected day when creating checkpoint.");
            var result = evaluate(day); // History still exists until the checkpoint succeeds.
            if (!profile.CompleteDay(result.IncorrectPatients, result.NetGold, result.DailyRating,
                result.HasCompletedConsultations, result.TotalEarn, result.CorrectPatients + result.IncorrectPatients))
                throw new InvalidOperationException("Không thể lưu cuối ngày. Bấm Thử lại.");
            completedResult = result;
            completedDay = day;
            return completedResult;
        }
    }

    public sealed class PreparationState : IDayFlowState
    {
        public DayStep Id => DayStep.Preparation;
        private readonly IDaySceneService scenes;
        private readonly IDayProfile profile;
        private readonly Action advance;
        private IDayFlowView view;
        private INeighborhoodView neighborhood;
        private CancellationToken lifetime;
        public bool IsNavigating { get; private set; }

        public PreparationState(IDaySceneService scenes, IDayProfile profile, Action advance)
        {
            this.scenes = scenes;
            this.profile = profile;
            this.advance = advance;
        }

        public Task EnterAsync(CancellationToken token)
        {
            lifetime = token;
            return ShowNeighborhoodAsync(nameof(GameScene.Player_Home), token);
        }

        private async Task ShowNeighborhoodAsync(string destination, CancellationToken token)
        {
            var next = await scenes.LoadNeighborhoodAsync(destination, token);
            DetachPreparation();
            DetachNeighborhood();
            neighborhood = next;
            neighborhood.TravelRequested += Travel;
            view = scenes.CurrentView;
            if (view != null)
            {
                view.OpenRequested += OpenClinic;
                view.ShowPreparation(profile.CurrentDay);
                await view.RevealSceneAsync(token);
            }
        }

        private void OpenClinic()
        {
            if (!IsNavigating) advance();
        }

        private async void Travel(string destination) => await NavigateAsync(destination);

        private async Task NavigateAsync(string destination)
        {
            if (IsNavigating || lifetime.IsCancellationRequested) return;
            IsNavigating = true;
            try
            {
                await ShowNeighborhoodAsync(destination, lifetime);
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
            catch (Exception error)
            {
                UnityEngine.Debug.LogException(error);
                if (scenes.CurrentView == null && scenes.CurrentNeighborhood == null &&
                    !lifetime.IsCancellationRequested)
                {
                    // If the destination replaced the scene but could not bind its view, return to Preparation.
                    DetachPreparation();
                    DetachNeighborhood();
                    try { await ShowNeighborhoodAsync(nameof(GameScene.Player_Home), lifetime); }
                    catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
                    catch (Exception recoveryError) { UnityEngine.Debug.LogException(recoveryError); }
                }
                if (view != null && ReferenceEquals(view, scenes.CurrentView))
                    view.ShowPreparationNavigationError("Không thể chuyển cảnh. Hãy thử lại.");
            }
            finally { IsNavigating = false; }
        }

        private void DetachPreparation()
        {
            if (view == null) return;
            view.OpenRequested -= OpenClinic;
            if (view is not UnityEngine.Object component || component != null) view.HideAll();
            view = null;
        }

        private void DetachNeighborhood()
        {
            if (neighborhood == null) return;
            neighborhood.TravelRequested -= Travel;
            neighborhood = null;
        }

        public void Exit()
        {
            DetachPreparation();
            DetachNeighborhood();
        }
    }

    public sealed class ExaminationState : IDayFlowState
    {
        public DayStep Id => DayStep.Examination;
        private readonly IDaySceneService scenes;
        private readonly IDayProfile profile;
        private readonly IPatientService patients;
        private readonly Action advance;
        private readonly Action endDay;
        private readonly Action onExamStarted;
        private readonly Func<PatientExaminationProfile, string> recordRockHit;
        private IDayFlowView view;
        private OfficeHaunting haunting;
        internal bool RockHitRecorded { get; private set; }
        public ExaminationState(IDaySceneService scenes, IDayProfile profile, IPatientService patients,
            Action advance, Action endDay, Action onExamStarted = null,
            Func<PatientExaminationProfile, string> recordRockHit = null)
        {
            this.scenes = scenes; this.profile = profile; this.patients = patients;
            this.advance = advance; this.endDay = endDay; this.onExamStarted = onExamStarted;
            this.recordRockHit = recordRockHit;
        }
        public async Task EnterAsync(CancellationToken token)
        {
            view = await scenes.LoadAsync(GameScene.Office, token);
            RockHitRecorded = false;
            onExamStarted?.Invoke();
            view.EndDayRequested += endDay;
            if (view is IPatientMotionView motionView) motionView.ShowPatientArrival(profile.CurrentDay);
            patients.EnsurePatient();
            if (patients is IAnimatedPatientService animated) await animated.WaitForArrivalAsync(token);
            view.TreatmentRequested += advance;
            haunting = (view as SceneGameplayUI)?.OfficeHaunting;
            if (haunting != null) haunting.PatientStruck += OnPatientStruck;
            view.ShowExamination(profile.CurrentDay);
        }
        private void OnPatientStruck(PatientExaminationProfile patient)
        {
            string error = recordRockHit?.Invoke(patient);
            if (error != null)
            {
                UnityEngine.Object.Destroy(patient.RockReaction.gameObject);
                patient.RockReaction = null;
                view.ShowPreparationNavigationError(error);
                return;
            }
            RockHitRecorded = true;
            advance();
        }
        public void Exit()
        {
            if (haunting != null) haunting.PatientStruck -= OnPatientStruck;
            haunting = null;
            if (view == null) return;
            view.EndDayRequested -= endDay;
            view.TreatmentRequested -= advance;
            view.HideAll();
            view = null;
        }
    }

    public sealed class TreatmentDecisionState : IDayFlowState
    {
        public DayStep Id => DayStep.TreatmentDecision;
        private readonly IDaySceneService scenes;
        private readonly IDayProfile profile;
        private readonly Action advance;
        private readonly Action cancel;
        private readonly Func<System.Collections.Generic.IReadOnlyList<string>, string> confirm;
        private readonly Func<PoliceReportReason, string> report;
        private readonly Func<string> decline;
        private bool committed;
        private IDayFlowView view;
        public System.Collections.Generic.IReadOnlyList<string> SelectedMedicineIds { get; private set; } = Array.Empty<string>();
        public TreatmentDecisionState(IDaySceneService scenes, IDayProfile profile, Action advance,
            Action cancel, Func<System.Collections.Generic.IReadOnlyList<string>, string> confirm, Func<PoliceReportReason, string> report,
            Func<string> decline)
        { this.scenes = scenes; this.profile = profile; this.advance = advance; this.cancel = cancel; this.confirm = confirm; this.report = report; this.decline = decline; }
        public async Task EnterAsync(CancellationToken token)
        {
            SelectedMedicineIds = Array.Empty<string>();
            committed = false;
            view = await scenes.LoadAsync(GameScene.Office, token);
            view.DecisionConfirmed += ConfirmTreatment;
            view.DeclineRequested += ConfirmDecline;
            view.CancelTreatmentRequested += cancel;
            view.ShowTreatment(profile.CurrentDay);
        }
        private void ConfirmTreatment()
        {
            if (committed) return;
            if (view.DecisionKind == TreatmentDecisionKind.ReportToPolice)
            {
                if (view.SelectedReportReason == PoliceReportReason.None) return;
                string reportError = report(view.SelectedReportReason);
                if (reportError != null) { view.ShowTreatmentError(reportError); return; }
                committed = true;
                advance();
                return;
            }
            // Keep a snapshot of the confirmed selection as the popup closes.
            var ids = new System.Collections.Generic.List<string>(view.SelectedMedicineIds);
            string error = confirm(ids);
            if (error != null) { view.ShowTreatmentError(error); return; }
            committed = true;
            SelectedMedicineIds = ids.AsReadOnly();
            advance();
        }
        private void ConfirmDecline()
        {
            if (committed) return;
            string error = decline();
            if (error != null) { view.ShowTreatmentError(error); return; }
            committed = true;
            advance();
        }
        public void Exit()
        {
            if (view == null) return;
            view.DecisionConfirmed -= ConfirmTreatment;
            view.DeclineRequested -= ConfirmDecline;
            view.CancelTreatmentRequested -= cancel;
            view.HideAll();
            view = null;
        }
    }

    public sealed class PatientDepartureState : IDayFlowState
    {
        public DayStep Id => DayStep.PatientDeparture;
        private readonly IDaySceneService scenes;
        private readonly IDayProfile profile;
        private readonly IAnimatedPatientService patients;
        private readonly ClinicClock clock;
        private readonly Func<bool> acceptingPatients;
        private readonly Func<int> signLevel;
        private readonly Func<float> rating;
        private readonly Action clientLimitReached;
        private const int ClientLimitCountdownSeconds = 15;
        private readonly Action endDay;
        private IDayFlowView view;
        public PatientDepartureState(IDaySceneService scenes, IDayProfile profile, IAnimatedPatientService patients,
            Action endDay, ClinicClock clock, Func<bool> acceptingPatients, Func<int> signLevel, Func<float> rating,
            Action clientLimitReached)
        { this.scenes = scenes; this.profile = profile; this.patients = patients; this.endDay = endDay; this.clock = clock; this.acceptingPatients = acceptingPatients; this.signLevel = signLevel; this.rating = rating; this.clientLimitReached = clientLimitReached; }
        public async Task EnterAsync(CancellationToken token)
        {
            view = await scenes.LoadAsync(GameScene.Office, token);
            view.EndDayRequested += endDay;
            var motion = view as IPatientMotionView;
            motion?.ShowPatientDeparture(profile.CurrentDay);
            await patients.DepartAsync(token);
            if (!acceptingPatients()) return;

            // The rating caps today's clients; once reached, count down and close like a normal day end.
            if (patients.ArrivedToday >= ClinicDemand.DailyClientLimit(rating(), signLevel()))
            {
                clientLimitReached();
                for (int secondsLeft = ClientLimitCountdownSeconds; secondsLeft > 0; secondsLeft--)
                {
                    motion?.ShowClientLimitReached(secondsLeft);
                    float until = UnityEngine.Time.unscaledTime + 1f;
                    while (UnityEngine.Time.unscaledTime < until)
                        await UnityEngine.Awaitable.NextFrameAsync(token);
                }
                return;
            }

            double delay = patients.HasRemainingPatients
                ? ClinicDemand.NextPatientDelay(signLevel(), rating(), UnityEngine.Random.value, UnityEngine.Random.value)
                : double.PositiveInfinity;
            if (delay <= 0) return;

            motion?.ShowWaitingForPatient();
            double nextArrival = clock.ElapsedSeconds + delay;
            while (acceptingPatients() && clock.ElapsedSeconds < nextArrival)
                await UnityEngine.Awaitable.NextFrameAsync(token);
        }
        public void Exit()
        {
            if (view == null) return;
            view.EndDayRequested -= endDay;
            view.HideAll();
            view = null;
        }
    }

    public sealed class DayResultsState : IDayFlowState
    {
        public DayStep Id => DayStep.DayResults;
        private readonly IDaySceneService scenes;
        private readonly IDayProfile profile;
        private readonly DayCompletionService completion;
        private readonly IDayEndAdvertisement advertisement;
        private IDayFlowView view;
        private int? closingDay;
        // A retry after the cutscene (e.g. Home failed to load) does not replay it.
        private bool cutsceneShown;
        public DayResultsState(IDaySceneService scenes, IDayProfile profile, DayCompletionService completion,
            IDayEndAdvertisement advertisement = null)
        { this.scenes = scenes; this.profile = profile; this.completion = completion; this.advertisement = advertisement; }
        public async Task EnterAsync(CancellationToken token)
        {
            if (!closingDay.HasValue) closingDay = profile.CurrentDay;
            if (scenes.CurrentView != null) await scenes.CurrentView.FadeToBlackAsync(token);
            var result = completion.Complete(closingDay.Value);
            // Days with consultations show the in-game cutscene; a day without patients goes straight to Home.
            if (result.HasCompletedConsultations && !cutsceneShown)
                cutsceneShown = await scenes.PlayDayEndCutsceneAsync(result, token);
            await scenes.LoadNeighborhoodAsync(nameof(GameScene.Player_Home), token);
            view = scenes.CurrentView;
            await view.PresentResultsAsync(result, cutsceneShown, token);
            UnityEngine.Debug.Log($"[Test Ads] Day {result.Day}: presentation completed; ad service present={advertisement != null}.");
            if (advertisement != null) await advertisement.ShowAfterResultsAsync(result.Day, token);
            closingDay = null;
            cutsceneShown = false;
        }
        public void Exit()
        { if (view == null) return; view.HideAll(); view = null; }
    }
}

