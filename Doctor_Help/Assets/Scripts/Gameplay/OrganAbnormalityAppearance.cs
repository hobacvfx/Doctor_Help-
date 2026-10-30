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
                var sickness = medicalData.Find(npc.SicknessId);
                if (sickness == null) throw new InvalidOperationException("NPC references a missing sickness: " + npc.SicknessId);
                foreach (var symptomId in sickness.SymptomIds)
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
                var positions = new HashSet<Transform>();
                foreach (var organ in organs)
                    if (organ == null || organ.prefab == null || organ.position == null ||
                        !organ.position.IsChildOf(transform) || !positions.Add(organ.position))
                        throw new InvalidOperationException("Each organ requires a prefab and a distinct child anchor of this patient.");
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

