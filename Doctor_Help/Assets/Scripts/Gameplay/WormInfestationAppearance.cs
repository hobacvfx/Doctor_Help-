using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PatientExaminationProfile))]
    public sealed class WormInfestationAppearance : MonoBehaviour
    {
        [SerializeField] private string wormSymptomId;
        [SerializeField] private Transform[] positions;
        [SerializeField] private GameObject[] wormPrefabs;
        [SerializeField, HideInInspector] private List<GameObject> spawnedWorms = new List<GameObject>();
        internal SpriteRenderer SpawnedWorm { get; private set; }
        internal string ClueSymptomId => wormSymptomId;

        public void Initialize(NpcDefinition npc, AllSickness medicalData)
        {
            if (npc == null || medicalData == null || medicalData.SymptomProfile == null ||
                string.IsNullOrEmpty(wormSymptomId) || medicalData.SymptomProfile.Find(wormSymptomId) == null)
                throw new InvalidOperationException("Parasite appearance requires a valid patient, medical catalogue and symptom mapping.");
            bool infected = false;
            if (npc.HasSickness)
            {
                var resolvedSymptoms = NpcMedicalConditions.Resolve(npc.SicknessIds, medicalData);
                foreach (var symptomId in resolvedSymptoms)
                {
                    if (medicalData.SymptomProfile.Find(symptomId) == null)
                        throw new InvalidOperationException("Sickness references a missing symptom: " + symptomId);
                    infected |= symptomId == wormSymptomId;
                }
            }
            if (infected)
            {
                if (positions == null || positions.Length == 0 || wormPrefabs == null || wormPrefabs.Length == 0 ||
                    Array.Exists(wormPrefabs, prefab => prefab == null))
                    throw new InvalidOperationException("Assign parasite prefabs and position anchors on the NPC prefab.");
                var unique = new HashSet<Transform>();
                foreach (var position in positions)
                    if (position == null || !position.IsChildOf(transform) || !unique.Add(position))
                        throw new InvalidOperationException("Parasite positions must be distinct child anchors of this patient.");
            }
            ClearSpawned();
            if (!infected) return;
            try
            {
                var position = positions[UnityEngine.Random.Range(0, positions.Length)];
                var prefab = wormPrefabs[UnityEngine.Random.Range(0, wormPrefabs.Length)];
                var worm = Instantiate(prefab, position, false);
                spawnedWorms.Add(worm);
                SpawnedWorm = worm.GetComponentInChildren<SpriteRenderer>();
                worm.transform.localPosition = Vector3.zero;
                // Preserve prefab rotation, scale, layers, materials and renderer settings.
            }
            catch { ClearSpawned(); throw; }
        }

        private void ClearSpawned()
        {
            SpawnedWorm = null;
            foreach (var worm in spawnedWorms)
            {
                if (worm == null) continue;
                worm.SetActive(false);
                if (Application.isPlaying) Destroy(worm);
                else DestroyImmediate(worm);
            }
            spawnedWorms.Clear();
        }
    }
}



