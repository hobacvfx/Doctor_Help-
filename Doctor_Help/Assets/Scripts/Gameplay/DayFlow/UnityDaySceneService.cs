using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoctorHelp
{
    public sealed class UnityDaySceneService : IDaySceneService
    {
        private TutorialPlan tutorialPlan;
        public IDayFlowView CurrentView { get; private set; }
        public INeighborhoodView CurrentNeighborhood { get; private set; }
        public Scene CurrentScene { get; private set; }

        public async Task<IDayFlowView> LoadAsync(GameScene destination, CancellationToken cancellation)
        {
            var scene = await LoadSceneAsync(destination.ToString(), cancellation);
            return CurrentView = BindGameplayView(scene);
        }

        public async Task<INeighborhoodView> LoadNeighborhoodAsync(string destination, CancellationToken cancellation)
        {
            string origin = SceneManager.GetActiveScene().name;
            var scene = await LoadSceneAsync(destination, cancellation);
            var view = FindSceneComponent<NeighborhoodView>(scene);
            if (view == null || !view.IsConfigured)
                throw new InvalidOperationException("Neighborhood scene has invalid navigation: " + scene.name);
            CurrentView = destination == nameof(GameScene.Player_Home) ? BindGameplayView(scene) : null;
            view.PlacePlayerAtEntrance(origin);
            if (PlayerProfileService.Instance is { IsLoaded: true })
                ConfigureFirstDayGuide(view, CurrentView as SceneGameplayUI);
            CurrentNeighborhood = view;
            return view;
        }

        private void ConfigureFirstDayGuide(NeighborhoodView view, SceneGameplayUI home)
        {
            if (tutorialPlan == null) tutorialPlan = Resources.Load<TutorialPlan>("FirstRunTutorial");
            var target = home != null ? home.OpenButton : view.DoorTo(nameof(GameScene.Player_Home)) ?? view.DoorTo("Street");
            view.ConfigureFirstDayGuide(tutorialPlan?.FirstDayGuide, target);
        }

        private static SceneGameplayUI BindGameplayView(Scene scene)
        {
            var view = FindSceneComponent<SceneGameplayUI>(scene);
            if (view == null || !view.IsConfigured)
                throw new InvalidOperationException("Scene UI is missing or has invalid Inspector references: " + scene.name);
            return view;
        }

        private async Task<Scene> LoadSceneAsync(string name, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var scene = SceneManager.GetActiveScene();
            if (scene.name != name)
            {
                if (!Application.CanStreamedLevelBeLoaded(name))
                    throw new InvalidOperationException("Scene is missing from the build list: " + name);
                var operation = SceneManager.LoadSceneAsync(name, LoadSceneMode.Single);
                if (operation == null) throw new InvalidOperationException("Cannot load scene: " + name);
                CurrentView = null;
                CurrentNeighborhood = null;
                var completed = new TaskCompletionSource<bool>();
                operation.completed += _ => completed.TrySetResult(true);
                if (operation.isDone) completed.TrySetResult(true);
                // Unity loads cannot be cancelled. Wait for completion, then honor cancellation.
                await completed.Task;
                cancellation.ThrowIfCancellationRequested();
                scene = SceneManager.GetActiveScene();
            }
            CurrentScene = scene;
            return scene;
        }

        private static T FindSceneComponent<T>(Scene scene) where T : Component
        {
            T result = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var candidate in root.GetComponentsInChildren<T>(true))
                {
                    if (result != null)
                        throw new InvalidOperationException($"Multiple {typeof(T).Name} components in {scene.name}.");
                    result = candidate;
                }
            return result;
        }
    }

    public sealed class PatientService : IAnimatedPatientService
    {
        private readonly UnityDaySceneService scenes;
        private readonly NpcCatalog catalog;
        private readonly PlayerProfileService profile;
        public PatientService(UnityDaySceneService scenes, NpcCatalog catalog, PlayerProfileService profile)
        { this.scenes = scenes; this.catalog = catalog; this.profile = profile; }
        private PatientSpawnPoint ResolveSpawnPoint()
        {
            PatientSpawnPoint spawnPoint = null;
            foreach (var root in scenes.CurrentScene.GetRootGameObjects())
                foreach (var candidate in root.GetComponentsInChildren<PatientSpawnPoint>(true))
                {
                    if (spawnPoint != null) throw new InvalidOperationException("Office has multiple PatientSpawnPoint components.");
                    spawnPoint = candidate;
                }
            if (spawnPoint == null) throw new InvalidOperationException("Office is missing PatientSpawnPoint on NPC_Position.");
            return spawnPoint;
        }
        public void EnsurePatient() => ResolveSpawnPoint().EnsurePatient(catalog, profile.DifficultySettings, profile.DiffProgress);
        public bool HasRemainingPatients => ResolveSpawnPoint().HasRemainingPatients;
        public int ArrivedToday => ResolveSpawnPoint().ArrivedCount;
        public PatientExaminationProfile CurrentPatient => ResolveSpawnPoint().CurrentPatient;
        public AllSickness MedicalData => ResolveSpawnPoint().NpcProfile.AllSickness;
        public Task WaitForArrivalAsync(CancellationToken token) => ResolveSpawnPoint().WaitForArrivalAsync(token);
        public Task DepartAsync(CancellationToken token) => ResolveSpawnPoint().DepartAsync(token);
    }
}


