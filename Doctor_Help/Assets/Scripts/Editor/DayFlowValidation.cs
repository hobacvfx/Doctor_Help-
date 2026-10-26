using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DoctorHelp.Editor
{
    // Pure C# checks: no scene loads, actual save files, or player profile mutations.
    public static class DayFlowValidation
    {
        public static int Run() => RunAsync().GetAwaiter().GetResult();

        private static async Task<int> RunAsync()
        {
            int count = 0;
            Action<bool, string> check = (ok, name) =>
            { if (!ok) throw new Exception("Day flow validation failed: " + name); count++; };
            bool rejected = false;
            try { new DayFlowPlan(DayStep.Examination, DayStep.DayResults); }
            catch (ArgumentException) { rejected = true; }
            check(rejected, "Preparation is mandatory at startup");
            rejected = false;
            try { new DayFlowPlan(DayStep.Preparation, DayStep.Examination); }
            catch (ArgumentException) { rejected = true; }
            check(rejected, "DayResults is mandatory at the end");
            rejected = false;
            try { new DayFlowPlan(DayStep.Preparation, DayStep.Preparation, DayStep.DayResults); }
            catch (ArgumentException) { rejected = true; }
            check(rejected, "Duplicate steps rejected");

            var plan = new DayFlowPlan(DayStep.Preparation, DayStep.Examination,
                DayStep.TreatmentDecision, DayStep.DayResults);
            rejected = false;
            try { new DayFlowMachine(plan, new TestState(DayStep.Preparation)); }
            catch (ArgumentException) { rejected = true; }
            check(rejected, "Unregistered states rejected");
            var prep = new TestState(DayStep.Preparation);
            var exam = new TestState(DayStep.Examination);
            var treatment = new TestState(DayStep.TreatmentDecision);
            var results = new TestState(DayStep.DayResults);
            var machine = new DayFlowMachine(plan, prep, exam, treatment, results);
            check(!await machine.AdvanceAsync(default), "Cannot advance before initialization");
            check(await machine.StartAsync(default) && machine.CurrentStep == DayStep.Preparation, "Initial step");
            check(!await machine.StartAsync(default), "Cannot initialize twice");
            exam.Gate = new TaskCompletionSource<bool>();
            var entering = machine.AdvanceAsync(default);
            check(machine.IsTransitioning && !await machine.AdvanceAsync(default), "Concurrent requests rejected");
            exam.Gate.SetResult(true);
            check(await entering && prep.Exits == 1 && machine.CurrentStep == DayStep.Examination, "Exit precedes successful entry");
            treatment.Fail = true;
            check(!await machine.AdvanceAsync(default) && machine.FailedStep == DayStep.TreatmentDecision &&
                !machine.IsTransitioning && treatment.Exits == 1, "Failed entry is cleaned and unlocks transition");
            check(!await machine.AdvanceAsync(default), "Fault requires explicit retry");
            treatment.Fail = false;
            check(await machine.RetryAsync(default) && machine.CurrentStep == DayStep.TreatmentDecision &&
                machine.LastError == null && exam.Exits == 1, "Retry does not exit the previous state twice");
            check(await machine.AdvanceAsync(default) && machine.CurrentStep == DayStep.DayResults, "Enter results");
            check(await machine.AdvanceAsync(default) && machine.CurrentStep == DayStep.Preparation, "Return to preparation");
            machine.Stop();
            check(!await machine.AdvanceAsync(default), "Stopped flow cannot restart");

            var reduced = new DayFlowMachine(new DayFlowPlan(DayStep.Preparation, DayStep.DayResults),
                new TestState(DayStep.Preparation), new TestState(DayStep.DayResults));
            await reduced.StartAsync(default);
            check(await reduced.AdvanceAsync(default) && reduced.CurrentStep == DayStep.DayResults,
                "Removing optional steps changes only the plan/registrations");
            var cancelled = new CancellationTokenSource();
            var waiting = new TestState(DayStep.Preparation) { Gate = new TaskCompletionSource<bool>() };
            var cancellationFlow = new DayFlowMachine(new DayFlowPlan(DayStep.Preparation, DayStep.DayResults),
                waiting, new TestState(DayStep.DayResults));
            var starting = cancellationFlow.StartAsync(cancelled.Token);
            cancelled.Cancel();
            waiting.Gate.SetResult(true);
            check(!await starting && waiting.Exits == 1 && !cancellationFlow.IsTransitioning,
                "Cancellation cleans partial entry");
            cancelled.Dispose();

            var scenes = new TestScenes();
            var profile = new TestProfile();
            var patients = new TestPatients();
            int requests = 0;
            Action advance = () => requests++;
            var treatmentState = new TreatmentDecisionState(scenes, profile, advance);
            var dayResults = new DayResultsState(scenes, profile, new DayCompletionService(profile), advance);
            var realStates = new DayFlowMachine(plan, new PreparationState(scenes, profile, advance),
                new ExaminationState(scenes, profile, patients, advance), treatmentState, dayResults);
            await realStates.StartAsync(default);
            scenes.Home.Open();
            check(requests == 1, "Preparation emits one request");
            await realStates.AdvanceAsync(default);
            scenes.Home.Open();
            check(requests == 1 && patients.Spawns == 1, "Preparation detaches and examination requests patient");
            await realStates.AdvanceAsync(default);
            scenes.Office.Give();
            check(requests == 1, "No treatment confirmation without a selected medicine");
            scenes.Office.SelectedMedicineId = "pill_a";
            scenes.Office.Give();
            check(requests == 2 && treatmentState.SelectedMedicineId == "pill_a", "Selected treatment emits one request");
            profile.FailSave = true;
            check(!await realStates.AdvanceAsync(default) && profile.CurrentDay == 1, "Failed save keeps current day");
            profile.FailSave = false;
            scenes.FailHome = true;
            check(!await realStates.RetryAsync(default) && profile.CurrentDay == 2 && profile.Saves == 1,
                "Checkpoint succeeds even if subsequent scene load fails");
            scenes.FailHome = false;
            check(await realStates.RetryAsync(default) && profile.Saves == 1 && profile.CurrentDay == 2,
                "Results retry does not save or advance the day again");
            scenes.Home.FinishResults();
            check(requests == 3, "Results completion emits advance request");
            await realStates.AdvanceAsync(default);
            scenes.Home.FinishResults();
            scenes.Home.Open();
            check(requests == 4 && profile.Saves == 1, "State re-entry has no duplicate listeners or morning save");
            realStates.Stop();
            scenes.Home.Open();
            check(requests == 4, "Stop detaches active UI listeners");
            return count;
        }

        private sealed class TestState : IDayFlowState
        {
            public DayStep Id { get; }
            public int Exits;
            public bool Fail;
            public TaskCompletionSource<bool> Gate;
            public TestState(DayStep id) => Id = id;
            public async Task EnterAsync(CancellationToken token)
            { if (Gate != null) await Gate.Task; if (Fail) throw new Exception("Simulated entry failure"); }
            public void Exit() => Exits++;
        }
        private sealed class TestProfile : IDayProfile
        {
            public int CurrentDay { get; private set; } = 1;
            public bool FailSave;
            public int Saves;
            public bool CompleteDay() { if (FailSave) return false; Saves++; CurrentDay++; return true; }
        }
        private sealed class TestPatients : IPatientService
        { public int Spawns; public void EnsurePatient() => Spawns++; }
        private sealed class TestScenes : IDaySceneService
        {
            public readonly TestView Home = new TestView();
            public readonly TestView Office = new TestView();
            public bool FailHome;
            public IDayFlowView CurrentView { get; private set; }
            public Task<IDayFlowView> LoadAsync(GameScene scene, CancellationToken token)
            {
                if (scene == GameScene.Home && FailHome) throw new Exception("Simulated scene failure");
                CurrentView = scene == GameScene.Home ? Home : Office;
                return Task.FromResult(CurrentView);
            }
        }
        private sealed class TestView : IDayFlowView
        {
            public event Action OpenRequested;
            public event Action TreatmentRequested { add { } remove { } }
            public event Action GiveRequested;
            public event Action ResultsCompleted;
            public event Action RetryRequested { add { } remove { } }
            public string SelectedMedicineId { get; set; }
            public void Open() => OpenRequested?.Invoke();
            public void Give() => GiveRequested?.Invoke();
            public void FinishResults() => ResultsCompleted?.Invoke();
            public void ShowPreparation(int day) { }
            public void ShowExamination(int day) { }
            public void ShowTreatment(int day) { SelectedMedicineId = null; }
            public void ShowResults() { }
            public void HideAll() { }
            public void ShowFlowError(string message) { }
        }
    }
}
