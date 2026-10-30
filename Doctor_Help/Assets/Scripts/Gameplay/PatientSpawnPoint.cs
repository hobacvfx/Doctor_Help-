using System;
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
        private bool departureStarted;
        private static readonly int IdleState = Animator.StringToHash("Base Layer.NPC_Idle");
        private static readonly int DepartureState = Animator.StringToHash("Base Layer.A_Walk_Out");
        public NpcProfile NpcProfile => npcProfile;
        public PatientExaminationProfile CurrentPatient => patient != null ? patient.GetComponent<PatientExaminationProfile>() : null;
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
                if (!animator.IsInTransition(0) && animator.GetCurrentAnimatorStateInfo(0).fullPathHash == IdleState) return;
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
            departureStarted = false;
            Destroy(departed);
            // Destroy is deferred: allow it to complete before another NPC can be instantiated.
            await Awaitable.NextFrameAsync(token);
        }

        public void EnsurePatient(NpcCatalog catalog)
        {
            if (patient != null) return;
            if (catalog == null || catalog.prefabs == null || catalog.prefabs.Length == 0 ||
                Array.Exists(catalog.prefabs, prefab => prefab == null))
                throw new InvalidOperationException("NPC catalog is missing or contains invalid prefab references.");
            var prefab = catalog.prefabs[UnityEngine.Random.Range(0, catalog.prefabs.Length)];
            var definition = npcProfile != null ? npcProfile.FindByPrefab(prefab) : null;
            if (definition == null)
                throw new InvalidOperationException("Spawned prefab is not configured in the assigned NpcProfile: " + prefab.name);
            var template = prefab.GetComponent<PatientExaminationProfile>();
            if (template == null || template.Heart == null)
                throw new InvalidOperationException("NPC prefab is missing its examination profile or Heart anchor: " + prefab.name);
            departureStarted = false;
            patient = Instantiate(prefab, transform, false);
            patient.GetComponent<PatientExaminationProfile>().Initialize(npcProfile, definition);
            patient.transform.localPosition = Vector3.zero;
            patient.transform.localRotation = Quaternion.identity;
            patient.name = prefab.name;
        }
    }
}


