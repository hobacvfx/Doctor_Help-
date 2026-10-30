using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoctorHelp
{
    public sealed class UnityDaySceneService : IDaySceneService
    {
        public IDayFlowView CurrentView { get; private set; }
        public Scene CurrentScene { get; private set; }

        public async Task<IDayFlowView> LoadAsync(GameScene destination, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            string name = destination.ToString();
            var scene = SceneManager.GetActiveScene();
            if (scene.name != name)
            {
                if (!Application.CanStreamedLevelBeLoaded(name))
                    throw new InvalidOperationException("Scene is missing from the build list: " + name);
                var operation = SceneManager.LoadSceneAsync(name, LoadSceneMode.Single);
                if (operation == null) throw new InvalidOperationException("Cannot load scene: " + name);
                CurrentView = null;
                var completed = new TaskCompletionSource<bool>();
                operation.completed += _ => completed.TrySetResult(true);
                if (operation.isDone) completed.TrySetResult(true);
                // Unity loads cannot be cancelled. Wait for completion, then honor cancellation.
                await completed.Task;
                cancellation.ThrowIfCancellationRequested();
                scene = SceneManager.GetActiveScene();
            }
            CurrentScene = scene;
            SceneGameplayUI view = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var candidate in root.GetComponentsInChildren<SceneGameplayUI>(true))
                {
                    if (view != null) throw new InvalidOperationException("Multiple scene UI views in " + name);
                    view = candidate;
                }
            if (view == null || !view.IsConfigured)
                throw new InvalidOperationException("Scene UI is missing or has invalid Inspector references: " + name);
            CurrentView = view;
            return view;
        }
    }

    public sealed class PatientService : IAnimatedPatientService
    {
        private readonly UnityDaySceneService scenes;
        private readonly NpcCatalog catalog;
        public PatientService(UnityDaySceneService scenes, NpcCatalog catalog)
        { this.scenes = scenes; this.catalog = catalog; }
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
        public void EnsurePatient() => ResolveSpawnPoint().EnsurePatient(catalog);
        public Task WaitForArrivalAsync(CancellationToken token) => ResolveSpawnPoint().WaitForArrivalAsync(token);
        public Task DepartAsync(CancellationToken token) => ResolveSpawnPoint().DepartAsync(token);
    }
}


