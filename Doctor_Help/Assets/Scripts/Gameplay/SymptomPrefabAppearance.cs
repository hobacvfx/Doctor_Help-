using System;
using System.Collections;
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
            [Tooltip("Optional shared set. Overrides the single prefab with equal-probability variants, chosen once per consultation.")]
            public SymptomAnimationVariants variants;
            public Transform position;
        }
        [SerializeField] private Binding[] bindings;
        [SerializeField, HideInInspector] private List<GameObject> spawned = new List<GameObject>();
        private const float SharedAnchorDisplaySeconds = 1f;
        private Coroutine sharedAnchorCycle;
        private readonly List<XrayClueTarget> clueTargets = new List<XrayClueTarget>();
        private readonly Dictionary<string, GameObject> consultationChoices = new Dictionary<string, GameObject>();
        private string choicesConsultationId;
        internal void AppendClueTargets(List<XrayClueTarget> output) => output.AddRange(clueTargets);

        public void Initialize(NpcDefinition npc, AllSickness medicalData)
        {
            if (npc == null || medicalData == null || medicalData.SymptomProfile == null || bindings == null)
                throw new InvalidOperationException("Symptom prefabs require a patient, medical catalogue and mappings.");
            var mapped = new HashSet<string>();
            foreach (var binding in bindings)
                if (binding == null || string.IsNullOrEmpty(binding.symptomId) ||
                    medicalData.SymptomProfile.Find(binding.symptomId) == null || !mapped.Add(binding.symptomId) ||
                    (binding.variants == null && binding.prefab == null) || binding.position == null || !binding.position.IsChildOf(transform))
                    throw new InvalidOperationException("Each symptom mapping needs a unique valid symptom, prefab and child anchor.");
            foreach (var binding in bindings)
                if (binding.variants != null) binding.variants.Validate();
            var symptoms = new HashSet<string>();
            if (npc.HasSickness)
            {
                var resolvedSymptoms = NpcMedicalConditions.Resolve(npc.SicknessIds, medicalData);
                foreach (var symptomId in resolvedSymptoms)
                {
                    if (medicalData.SymptomProfile.Find(symptomId) == null)
                        throw new InvalidOperationException("Sickness references a missing symptom: " + symptomId);
                    symptoms.Add(symptomId);
                }
            }
            ClearSpawned();
            var consultationId = GetComponent<PatientExaminationProfile>().ConsultationId;
            if (string.IsNullOrEmpty(consultationId) || choicesConsultationId != consultationId)
            {
                consultationChoices.Clear();
                choicesConsultationId = consultationId;
            }
            try
            {
                var byAnchor = new Dictionary<Transform, List<GameObject>>();
                foreach (var binding in bindings)
                {
                    if (!symptoms.Contains(binding.symptomId)) continue;
                    if (!consultationChoices.TryGetValue(binding.symptomId, out var selected))
                    {
                        selected = binding.variants != null ? binding.variants.Pick() : binding.prefab;
                        consultationChoices.Add(binding.symptomId, selected);
                    }
                    var instance = Instantiate(selected, binding.position, false);
                    spawned.Add(instance);
                    clueTargets.Add(new XrayClueTarget(binding.symptomId, instance));
                    instance.transform.localPosition = Vector3.zero;
                    if (!byAnchor.TryGetValue(binding.position, out var group))
                        byAnchor.Add(binding.position, group = new List<GameObject>());
                    if (group.Count > 0) instance.SetActive(false);
                    group.Add(instance);
                }
                var sharedAnchors = new List<List<GameObject>>();
                foreach (var group in byAnchor.Values)
                    if (group.Count > 1) sharedAnchors.Add(group);
                if (Application.isPlaying && sharedAnchors.Count > 0)
                    sharedAnchorCycle = StartCoroutine(AlternateSharedAnchors(sharedAnchors));
            }
            catch { ClearSpawned(); throw; }
        }

        private static IEnumerator AlternateSharedAnchors(List<List<GameObject>> groups)
        {
            var delay = new WaitForSeconds(SharedAnchorDisplaySeconds);
            var activeIndices = new int[groups.Count];
            while (true)
            {
                yield return delay;
                for (int i = 0; i < groups.Count; i++)
                {
                    var group = groups[i];
                    group[activeIndices[i]].SetActive(false);
                    activeIndices[i] = (activeIndices[i] + 1) % group.Count;
                    group[activeIndices[i]].SetActive(true);
                }
            }
        }

        private void ClearSpawned()
        {
            if (sharedAnchorCycle != null)
            {
                StopCoroutine(sharedAnchorCycle);
                sharedAnchorCycle = null;
            }
            foreach (var instance in spawned)
            {
                if (instance == null) continue;
                instance.SetActive(false);
                if (Application.isPlaying) Destroy(instance);
                else DestroyImmediate(instance);
            }
            spawned.Clear();
            clueTargets.Clear();
        }
    }
}


