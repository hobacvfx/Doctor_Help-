using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PatientExaminationProfile))]
    public sealed class SymptomPrefabAppearance : MonoBehaviour
    {
        [Serializable]
        public sealed class Binding
        {
            public string symptomId;
            public GameObject prefab;
            public Transform position;
        }
        [SerializeField] private Binding[] bindings;
        [SerializeField, HideInInspector] private List<GameObject> spawned = new List<GameObject>();

        public void Initialize(NpcDefinition npc, AllSickness medicalData)
        {
            if (npc == null || medicalData == null || medicalData.SymptomProfile == null || bindings == null)
                throw new InvalidOperationException("Symptom prefabs require a patient, medical catalogue and mappings.");
            var mapped = new HashSet<string>();
            foreach (var binding in bindings)
                if (binding == null || string.IsNullOrEmpty(binding.symptomId) ||
                    medicalData.SymptomProfile.Find(binding.symptomId) == null || !mapped.Add(binding.symptomId) ||
                    binding.prefab == null || binding.position == null || !binding.position.IsChildOf(transform))
                    throw new InvalidOperationException("Each symptom mapping needs a unique valid symptom, prefab and child anchor.");
            var symptoms = new HashSet<string>();
            if (npc.HasSickness)
            {
                var sickness = medicalData.Find(npc.SicknessId);
                if (sickness == null) throw new InvalidOperationException("NPC references a missing sickness: " + npc.SicknessId);
                foreach (var symptomId in sickness.SymptomIds)
                {
                    if (medicalData.SymptomProfile.Find(symptomId) == null)
                        throw new InvalidOperationException("Sickness references a missing symptom: " + symptomId);
                    symptoms.Add(symptomId);
                }
            }
            ClearSpawned();
            try
            {
                foreach (var binding in bindings)
                {
                    if (!symptoms.Contains(binding.symptomId)) continue;
                    var instance = Instantiate(binding.prefab, binding.position, false);
                    spawned.Add(instance);
                    instance.transform.localPosition = Vector3.zero;
                }
            }
            catch { ClearSpawned(); throw; }
        }

        private void ClearSpawned()
        {
            foreach (var instance in spawned)
            {
                if (instance == null) continue;
                instance.SetActive(false);
                if (Application.isPlaying) Destroy(instance);
                else DestroyImmediate(instance);
            }
            spawned.Clear();
        }
    }
}

