using System;
using System.Threading;
using System.Threading.Tasks;

namespace DoctorHelp
{
    public enum GameScene { Home, Office }

    public interface IDayFlowView
    {
        event Action OpenRequested;
        event Action TreatmentRequested;
        event Action GiveRequested;
        event Action ResultsCompleted;
        event Action RetryRequested;
        string SelectedMedicineId { get; }
        void ShowPreparation(int day);
        void ShowExamination(int day);
        void ShowTreatment(int day);
        void ShowResults();
        void HideAll();
        void ShowFlowError(string message);
    }

    public interface IDaySceneService
    {
        IDayFlowView CurrentView { get; }
        Task<IDayFlowView> LoadAsync(GameScene scene, CancellationToken cancellation);
    }

    public interface IDayProfile
    {
        int CurrentDay { get; }
        bool CompleteDay();
    }

    public interface IPatientService { void EnsurePatient(); }
    public interface IAnimatedPatientService : IPatientService
    {
        Task WaitForArrivalAsync(CancellationToken token);
        Task DepartAsync(CancellationToken token);
    }

    public interface IPatientMotionView
    {
        void ShowPatientArrival(int day);
        void ShowPatientDeparture(int day);
    }

    // One checkpoint per completed day, even when results entry is retried after a scene failure.
    public sealed class DayCompletionService
    {
        private readonly IDayProfile profile;
        private int? completedDay;
        public DayCompletionService(IDayProfile profile) => this.profile = profile;
        public void Complete(int day)
        {
            if (completedDay == day) return;
            if (profile.CurrentDay != day) throw new InvalidOperationException("Unexpected day when creating checkpoint.");
            if (!profile.CompleteDay()) throw new InvalidOperationException("Không thể lưu cuối ngày. Bấm Thử lại.");
            completedDay = day;
        }
    }

    public sealed class PreparationState : IDayFlowState
    {
        public DayStep Id => DayStep.Preparation;
        private readonly IDaySceneService scenes;
        private readonly IDayProfile profile;
        private readonly Action advance;
        private IDayFlowView view;
        public PreparationState(IDaySceneService scenes, IDayProfile profile, Action advance)
        { this.scenes = scenes; this.profile = profile; this.advance = advance; }
        public async Task EnterAsync(CancellationToken token)
        {
            view = await scenes.LoadAsync(GameScene.Home, token);
            view.OpenRequested += advance;
            view.ShowPreparation(profile.CurrentDay);
        }
        public void Exit()
        { if (view == null) return; view.OpenRequested -= advance; view.HideAll(); view = null; }
    }

    public sealed class ExaminationState : IDayFlowState
    {
        public DayStep Id => DayStep.Examination;
        private readonly IDaySceneService scenes;
        private readonly IDayProfile profile;
        private readonly IPatientService patients;
        private readonly Action advance;
        private IDayFlowView view;
        public ExaminationState(IDaySceneService scenes, IDayProfile profile, IPatientService patients, Action advance)
        { this.scenes = scenes; this.profile = profile; this.patients = patients; this.advance = advance; }
        public async Task EnterAsync(CancellationToken token)
        {
            view = await scenes.LoadAsync(GameScene.Office, token);
            if (view is IPatientMotionView motionView) motionView.ShowPatientArrival(profile.CurrentDay);
            patients.EnsurePatient();
            if (patients is IAnimatedPatientService animated) await animated.WaitForArrivalAsync(token);
            view.TreatmentRequested += advance;
            view.ShowExamination(profile.CurrentDay);
        }
        public void Exit()
        { if (view == null) return; view.TreatmentRequested -= advance; view.HideAll(); view = null; }
    }

    public sealed class TreatmentDecisionState : IDayFlowState
    {
        public DayStep Id => DayStep.TreatmentDecision;
        private readonly IDaySceneService scenes;
        private readonly IDayProfile profile;
        private readonly Action advance;
        private IDayFlowView view;
        public string SelectedMedicineId { get; private set; }
        public TreatmentDecisionState(IDaySceneService scenes, IDayProfile profile, Action advance)
        { this.scenes = scenes; this.profile = profile; this.advance = advance; }
        public async Task EnterAsync(CancellationToken token)
        {
            SelectedMedicineId = null;
            view = await scenes.LoadAsync(GameScene.Office, token);
            view.GiveRequested += ConfirmTreatment;
            view.ShowTreatment(profile.CurrentDay);
        }
        private void ConfirmTreatment()
        {
            if (string.IsNullOrWhiteSpace(view.SelectedMedicineId)) return;
            SelectedMedicineId = view.SelectedMedicineId;
            advance();
        }
        public void Exit()
        { if (view == null) return; view.GiveRequested -= ConfirmTreatment; view.HideAll(); view = null; }
    }

    public sealed class PatientDepartureState : IDayFlowState
    {
        public DayStep Id => DayStep.PatientDeparture;
        private readonly IDaySceneService scenes;
        private readonly IDayProfile profile;
        private readonly IAnimatedPatientService patients;
        private IDayFlowView view;
        public PatientDepartureState(IDaySceneService scenes, IDayProfile profile, IAnimatedPatientService patients)
        { this.scenes = scenes; this.profile = profile; this.patients = patients; }
        public async Task EnterAsync(CancellationToken token)
        {
            view = await scenes.LoadAsync(GameScene.Office, token);
            if (view is IPatientMotionView departureView) departureView.ShowPatientDeparture(profile.CurrentDay);
            await patients.DepartAsync(token);
        }
        public void Exit()
        { view?.HideAll(); view = null; }
    }

    public sealed class DayResultsState : IDayFlowState
    {
        public DayStep Id => DayStep.DayResults;
        private readonly IDaySceneService scenes;
        private readonly IDayProfile profile;
        private readonly DayCompletionService completion;
        private readonly Action advance;
        private IDayFlowView view;
        private int? closingDay;
        public DayResultsState(IDaySceneService scenes, IDayProfile profile, DayCompletionService completion, Action advance)
        { this.scenes = scenes; this.profile = profile; this.completion = completion; this.advance = advance; }
        public async Task EnterAsync(CancellationToken token)
        {
            if (!closingDay.HasValue) closingDay = profile.CurrentDay;
            // Apply future nightly calculations before this checkpoint, once per closing day.
            completion.Complete(closingDay.Value);
            view = await scenes.LoadAsync(GameScene.Home, token);
            view.ResultsCompleted += FinishPresentation;
            view.ShowResults();
        }
        private void FinishPresentation() { closingDay = null; advance(); }
        public void Exit()
        { if (view == null) return; view.ResultsCompleted -= FinishPresentation; view.HideAll(); view = null; }
    }
}

