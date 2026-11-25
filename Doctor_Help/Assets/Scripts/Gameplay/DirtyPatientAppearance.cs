using System;
using UnityEngine;

namespace DoctorHelp
{
    [DisallowMultipleComponent, RequireComponent(typeof(PatientExaminationProfile))]
    public sealed class DirtyPatientAppearance : MonoBehaviour
    {
        [SerializeField] private ParticleSystem smellyPrefab;
        [SerializeField] private Transform effectParent;
        [SerializeField] private Vector3 localOffset;
        private ParticleSystem instance;

        public bool IsConfigured => smellyPrefab != null && effectParent != null && effectParent.IsChildOf(transform);

        internal void Initialize(bool dirty)
        {
            if (instance != null)
            {
                instance.gameObject.SetActive(false);
                Destroy(instance.gameObject);
                instance = null;
            }
            if (!dirty) return;
            if (!IsConfigured)
                throw new InvalidOperationException("Dirty patient effects require a prefab and an animated child anchor.");

            instance = Instantiate(smellyPrefab, effectParent, false);
            instance.transform.localPosition = localOffset;
            // Existing particles must follow the same animated All/Outfit as the emitters.
            // Apply this to this instance only; the player's shared VFX asset stays authored as-is.
            foreach (var particles in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particles.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
            instance.Play(true);
        }
    }
}
