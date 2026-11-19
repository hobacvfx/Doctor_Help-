using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace DoctorHelp
{
    [DefaultExecutionOrder(-300)]
    internal sealed class FirstRunTutorial : MonoBehaviour
    {
        private const string HauntedCompletedKey = "DoctorHelp.HauntedTutorial.Completed";
        internal static FirstRunTutorial Instance { get; private set; }
        internal bool IsActive { get; private set; }
        internal bool IsHauntedActive => haunting != null;
        internal bool PausesClinicClock => IsActive || IsHauntedActive;
        private TutorialPlan plan;
        private TutorialSceneView view;
        private PlayerProfileService profile;
        private DailyGameLoop loop;
        private Component worldTarget;
        private EventSystem eventSystem;
        private bool previousNavigation;
        private string preferenceKey;
        private bool preview;
        private bool hauntedCompleted;
        private OfficeHaunting haunting;
        private int index;
        private bool stepPresented, hasPresented, paused, unfocused;
        private float unavailableSeconds;

        private void Start()
        {
            profile = PlayerProfileService.Instance;
            loop = GetComponent<DailyGameLoop>();
            plan = Resources.Load<TutorialPlan>("FirstRunTutorial");
#if UNITY_EDITOR
            preview = TutorialPreviewSession.IsActive || loop.SkipOnboarding;
#endif
            hauntedCompleted = !preview && PlayerPrefs.GetInt(HauntedCompletedKey) != 0;
            if (!loop.SkipOnboarding) preferenceKey = "DoctorHelp.FirstRunTutorial.Completed";
        }

        internal void OnClinicOpened()
        {
            if (preferenceKey == null || profile == null || !profile.IsLoaded || profile.CurrentDay != 1 || profile.HauntedTest) return;
            if (preview || PlayerPrefs.GetInt(preferenceKey) == 0) Begin();
        }

        internal void OnClinicClosed()
        {
            if (IsActive || IsHauntedActive) Stop();
        }

        internal void RestartAfterProfileReset()
        {
            Stop();
            hauntedCompleted = false;
            if (!preview)
            {
                if (preferenceKey != null) PlayerPrefs.DeleteKey(preferenceKey);
                PlayerPrefs.DeleteKey(HauntedCompletedKey);
                PlayerPrefs.Save();
            }
        }

        private void Begin()
        {
            if (plan == null || plan.Steps.Count == 0 || profile == null || !profile.IsLoaded) return;
            var pill = Resources.Load<PlayerStartingProfile>("PlayerStartingProfile")?.Pills?.Find(plan.MedicineId);
            if (pill == null || pill.Price < 0 || (pill.Price > 0 && profile.Money < pill.Price))
            {
                Debug.LogWarning("First-run tutorial requires an affordable medicine from PillProfile.");
                return;
            }

            Instance = this;
            IsActive = true;
            index = 0;
            worldTarget = null;
            stepPresented = hasPresented = false;
            unavailableSeconds = 0;
            SceneManager.sceneLoaded += OnSceneLoaded;
            FindView();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            view?.ResetHold();
            RestoreNavigation();
            FindView();
        }

        private void FindView() => view = FindFirstObjectByType<TutorialSceneView>();

        internal static bool AllowsWorld(Component target) =>
            Instance == null || (!Instance.IsHauntedActive &&
                (!Instance.IsActive || !Instance.hasPresented || Instance.worldTarget == target));

        internal bool OfferHauntedTutorial(OfficeHaunting source)
        {
            if (hauntedCompleted || IsHauntedActive) return true;
            if (IsActive) return false;
            FindView();
            if (view == null) return true;
            if (!view.ShowHauntedThrow(source)) return false;

            haunting = source;
            haunting.RockReleased += CompleteHauntedTutorial;
            Instance = this;
            DisableNavigation();
            return true;
        }

        private void CompleteHauntedTutorial()
        {
            hauntedCompleted = true;
            if (!preview)
            {
                PlayerPrefs.SetInt(HauntedCompletedKey, 1);
                PlayerPrefs.Save();
            }
            Stop();
        }

        internal int ChooseFirstPatient(IReadOnlyList<NpcDefinition> roster, AllSickness sicknesses)
        {
            if (!IsActive) return -1;
            var illness = sicknesses.Find(plan.SicknessId);
            if (illness != null && illness.Treatment == SicknessTreatment.RequiredPills &&
                illness.PillIds.Count == 1 && illness.PillIds[0] == plan.MedicineId)
            {
                for (int i = 0; i < roster.Count; i++)
                {
                    var ids = roster[i].SicknessIds;
                    if (ids.Count == 1 && ids[0] == illness.Id) return i;
                }
            }
            Stop("No patient in the roster matches the tutorial condition and prescription.");
            return -1;
        }

        private void Update()
        {
            if (!ReferenceEquals(haunting, null))
            {
                if (haunting == null || !haunting.CanTeachThrow || view == null) Stop();
                else view.ShowHauntedThrow(haunting);
                return;
            }
            if (!IsActive || paused || unfocused) return;
            var step = plan.Steps[index];
            if (view != null && stepPresented && (step.Action == TutorialAction.UseTool
                ? view.HoldComplete : view.IsComplete(step, loop, plan.MedicineId)))
            {
                if (++index == plan.Steps.Count)
                {
                    RememberCompletion();
                    Stop();
                    return;
                }
                step = plan.Steps[index];
                stepPresented = false;
                view.ResetHold();
                unavailableSeconds = 0;
            }

            if (view != null && view.TryGetTarget(step, loop, plan, !stepPresented, out var bounds, out var target))
            {
                worldTarget = target;
                stepPresented = hasPresented = true;
                unavailableSeconds = 0;
                DisableNavigation();
                view.Show(bounds, step, plan.MedicineId);
                return;
            }

            worldTarget = null;
            if (step.Action == TutorialAction.UseTool)
            {
                stepPresented = false;
                if (view != null) view.ResetHold();
            }
            if (hasPresented && view != null) view.BlockWhileWaiting();
            unavailableSeconds += Time.unscaledDeltaTime;
            if (unavailableSeconds >= plan.TargetTimeout)
                Stop($"Could not resolve tutorial step {step.Action}.");
        }

        private void DisableNavigation()
        {
            if (eventSystem != null || EventSystem.current == null) return;
            eventSystem = EventSystem.current;
            previousNavigation = eventSystem.sendNavigationEvents;
            eventSystem.sendNavigationEvents = false;
        }

        private void RestoreNavigation()
        {
            if (eventSystem != null) eventSystem.sendNavigationEvents = previousNavigation;
            eventSystem = null;
        }

        private void RememberCompletion()
        {
            if (preview) return;
            PlayerPrefs.SetInt(preferenceKey, 1);
            PlayerPrefs.Save();
        }

        private void Stop(string reason = null)
        {
            if (reason != null) Debug.LogWarning("First-run tutorial ended. " + reason);
            IsActive = false;
            if (haunting != null) haunting.RockReleased -= CompleteHauntedTutorial;
            haunting = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (view != null) view.Hide();
            RestoreNavigation();
            if (Instance == this) Instance = null;
        }

        private void OnDestroy() => Stop();
        private void OnApplicationPause(bool value)
        {
            paused = value;
            if (value && view != null) view.ResetHold();
        }
        private void OnApplicationFocus(bool focused)
        {
            unfocused = !focused;
            if (!focused && view != null) view.ResetHold();
        }
    }
}
