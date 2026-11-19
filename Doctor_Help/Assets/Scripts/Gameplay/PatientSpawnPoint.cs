using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    public sealed class PatientSpawnPoint : MonoBehaviour
    {
        [SerializeField] private NpcProfile npcProfile;
        private GameObject patient;
        private PatientExaminationProfile currentPatient;
        // Office is loaded once per working day. A fresh scene gets a fresh roster.
        private List<NpcDefinition>[] remainingPatients;
        private DifficultyProfile difficultySettings;
        private int difficultyProgress;
        private bool hasSpawnedPatient;
        private bool departureStarted, paused, unfocused, skipPatienceFrame;
        private static readonly int IdleState = Animator.StringToHash("Base Layer.NPC_Idle");
        private static readonly int DepartureState = Animator.StringToHash("Base Layer.A_Walk_Out");
        public NpcProfile NpcProfile => npcProfile;
        public PatientExaminationProfile CurrentPatient => patient != null ? currentPatient : null;
        public bool IsDeparting => departureStarted;
        // Office is reloaded each working day, so this counts today's admitted patients.
        public int ArrivedCount { get; private set; }
        public bool HasRemainingPatients => remainingPatients != null &&
            Array.Exists(remainingPatients, group => group.Count > 0);
        private void Awake() => unfocused = !Application.isFocused;

        private void Update()
        {
            if (!paused && !unfocused && !skipPatienceFrame && currentPatient != null &&
                FirstRunTutorial.Instance?.IsHauntedActive != true)
                currentPatient.AdvancePatience(Time.unscaledDeltaTime);
            skipPatienceFrame = false;
        }

        private void OnApplicationPause(bool value) { paused = value; skipPatienceFrame = true; }
        private void OnApplicationFocus(bool focused) { unfocused = !focused; skipPatienceFrame = true; }

        private Animator RequireAnimator()
        {
            var animator = patient != null ? patient.GetComponentInChildren<Animator>() : null;
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null ||
                !animator.HasState(0, IdleState) || !animator.HasState(0, DepartureState))
                throw new InvalidOperationException("Patient requires the arrival/idle/departure animation controller.");
            return animator;
        }

        public async Task WaitForArrivalAsync(CancellationToken token)
        {
            var animator = RequireAnimator();
            while (true)
            {
                token.ThrowIfCancellationRequested();
                if (animator == null || !animator.isActiveAndEnabled)
                    throw new InvalidOperationException("Patient animator was removed during arrival.");
                if (!animator.IsInTransition(0) && animator.GetCurrentAnimatorStateInfo(0).fullPathHash == IdleState)
                {
                    CurrentPatient.BeginPatience();
                    skipPatienceFrame = true;
                    patient.GetComponent<NpcDialogue>()?.BeginAfterArrival();
                    return;
                }
                await Awaitable.NextFrameAsync(token);
            }
        }

        public async Task DepartAsync(CancellationToken token)
        {
            // A successful departure is idempotent if a later transition needs retrying.
            if (patient == null) return;
            var animator = RequireAnimator();
            if (!departureStarted)
            {
                CurrentPatient.StopPatience();
                if (CurrentPatient.RockReaction != null)
                    await CurrentPatient.RockReaction.PlayAsync(token);
                patient.GetComponent<NpcDialogue>()?.CloseBox();
                animator.ResetTrigger("Leave");
                animator.SetTrigger("Leave");
                departureStarted = true;
            }
            while (true)
            {
                token.ThrowIfCancellationRequested();
                if (animator == null || !animator.isActiveAndEnabled)
                    throw new InvalidOperationException("Patient animator was removed during departure.");
                var state = animator.GetCurrentAnimatorStateInfo(0);
                if (!animator.IsInTransition(0) && state.fullPathHash == DepartureState && state.normalizedTime >= 1) break;
                await Awaitable.NextFrameAsync(token);
            }
            var departed = patient;
            patient = null;
            currentPatient = null;
            departureStarted = false;
            Destroy(departed);
            // Destroy is deferred: allow it to complete before another NPC can be instantiated.
            await Awaitable.NextFrameAsync(token);
        }

        public void EnsurePatient(NpcCatalog catalog, DifficultyProfile settings, int progress)
        {
            if (patient != null) return;
            if (remainingPatients == null)
            {
                remainingPatients = settings.GroupPatients(BuildRoster(catalog), progress);
                difficultySettings = settings;
                difficultyProgress = progress;
            }
            if (!HasRemainingPatients)
                throw new InvalidOperationException($"No eligible patients remain for Diff Progress {difficultyProgress}.");
            int preferred = !hasSpawnedPatient
                ? FirstRunTutorial.Instance?.ChooseFirstPatient(remainingPatients[0], npcProfile.AllSickness) ?? -1
                : -1;
            int groupIndex = preferred >= 0 ? 0 :
                difficultySettings.ChooseGroup(remainingPatients, difficultyProgress, UnityEngine.Random.value);
            if (groupIndex < 0)
                throw new InvalidOperationException($"No configured patient groups remain for Diff Progress {difficultyProgress}.");
            var group = remainingPatients[groupIndex];
            int index = preferred >= 0 ? preferred : UnityEngine.Random.Range(0, group.Count);
            var definition = group[index];
            var prefab = definition.Prefab;
            var template = prefab.GetComponent<PatientExaminationProfile>();
            if (template == null || template.Heart == null)
                throw new InvalidOperationException("NPC prefab is missing its examination profile or Heart anchor: " + prefab.name);
            departureStarted = false;
            var spawned = Instantiate(prefab, transform, false);
            try
            {
                var examination = spawned.GetComponent<PatientExaminationProfile>();
                examination.Initialize(npcProfile, definition);
                spawned.transform.localPosition = Vector3.zero;
                spawned.transform.localRotation = Quaternion.identity;
                spawned.name = prefab.name;
                patient = spawned;
                currentPatient = examination;
                group.RemoveAt(index);
                hasSpawnedPatient = true;
                ArrivedCount++;
            }
            catch
            {
                // A failed examination setup must not consume the slot on retry.
                Destroy(spawned);
                throw;
            }
        }

        private List<NpcDefinition> BuildRoster(NpcCatalog catalog)
        {
            if (catalog == null || catalog.prefabs == null || catalog.prefabs.Length == 0 ||
                Array.Exists(catalog.prefabs, prefab => prefab == null))
                throw new InvalidOperationException("NPC catalog is missing or contains invalid prefab references.");
            var result = new List<NpcDefinition>(catalog.prefabs.Length);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var prefab in catalog.prefabs)
            {
                var definition = npcProfile != null ? npcProfile.FindByPrefab(prefab) : null;
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
                    throw new InvalidOperationException("NPC catalog prefab has no stable NpcProfile identity: " + prefab.name);
                if (ids.Add(definition.Id)) result.Add(definition);
            }
            return result;
        }
    }
}




