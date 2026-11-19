using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PatientExaminationProfile))]
    public sealed class OrganAbnormalityAppearance : MonoBehaviour
    {
        [Serializable]
        public sealed class OrganSlot
        {
            public GameObject prefab;
            public Transform position;
        }
        [SerializeField] private string organSymptomId;
        [SerializeField] private OrganSlot[] organs;
        [SerializeField, HideInInspector] private GameObject spawnedOrgan;

        public void Initialize(NpcDefinition npc, AllSickness medicalData)
        {
            if (npc == null || medicalData == null || medicalData.SymptomProfile == null ||
                string.IsNullOrEmpty(organSymptomId) || medicalData.SymptomProfile.Find(organSymptomId) == null)
                throw new InvalidOperationException("Organ appearance requires a valid patient, medical catalogue and symptom mapping.");
            bool abnormal = false;
            if (npc.HasSickness)
            {
                var resolvedSymptoms = NpcMedicalConditions.Resolve(npc.SicknessIds, medicalData);
                foreach (var symptomId in resolvedSymptoms)
                {
                    if (medicalData.SymptomProfile.Find(symptomId) == null)
                        throw new InvalidOperationException("Sickness references a missing symptom: " + symptomId);
                    abnormal |= symptomId == organSymptomId;
                }
            }
            if (abnormal)
            {
                if (organs == null || organs.Length == 0)
                    throw new InvalidOperationException("Assign organ prefab/position pairs on the NPC prefab.");
                var options = new HashSet<(GameObject, Transform)>();
                foreach (var organ in organs)
                    if (organ == null || organ.prefab == null || organ.position == null ||
                        !organ.position.IsChildOf(transform) || !options.Add((organ.prefab, organ.position)))
                        throw new InvalidOperationException("Each organ option requires a unique prefab/child anchor pair on this patient.");
            }
            ClearSpawned();
            if (!abnormal) return;
            var selected = organs[UnityEngine.Random.Range(0, organs.Length)];
            spawnedOrgan = Instantiate(selected.prefab, selected.position, false);
            spawnedOrgan.transform.localPosition = Vector3.zero;
            // Keep authored prefab rotation, scale, material and sorting.
        }

        private void ClearSpawned()
        {
            if (spawnedOrgan == null) return;
            spawnedOrgan.SetActive(false);
            if (Application.isPlaying) Destroy(spawnedOrgan);
            else DestroyImmediate(spawnedOrgan);
            spawnedOrgan = null;
        }
    }
}


