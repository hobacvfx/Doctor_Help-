using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    // Every basement plant kind, in shop order: Assets/Resources/BasementPlantCatalog.asset.
    // Only playable profiles are sold; an owned plant whose profile disappears stays saved but inactive.
    [CreateAssetMenu(menuName = "Doctor Help/Basement Plant Catalog", fileName = "BasementPlantCatalog")]
    public sealed class BasementPlantCatalog : ScriptableObject
    {
        internal const string ResourcePath = "BasementPlantCatalog";

        [SerializeField] private BasementPlantProfile[] plants = new BasementPlantProfile[0];

        private readonly List<BasementPlantProfile> playable = new List<BasementPlantProfile>();
        private bool built;

        public IReadOnlyList<BasementPlantProfile> Playable
        {
            get
            {
                if (!built) Build();
                return playable;
            }
        }

        public BasementPlantProfile Find(string plantId)
        {
            if (string.IsNullOrEmpty(plantId)) return null;
            foreach (var plant in Playable)
                if (plant.PlantId == plantId) return plant;
            return null;
        }

        private void Build()
        {
            built = true;
            playable.Clear();
            var ids = new HashSet<string>();
            foreach (var plant in plants)
            {
                if (plant == null) continue;
                if (!plant.IsValid(out string error)) { Debug.LogWarning($"Basement plant '{plant.name}' skipped: {error}", plant); continue; }
                if (!plant.IsPlayable)
                {
                    Debug.LogWarning($"Basement plant '{plant.name}' skipped: attack type {plant.Attack} has no gameplay.", plant);
                    continue;
                }
                if (!ids.Add(plant.PlantId)) { Debug.LogWarning($"Duplicate basement plant ID '{plant.PlantId}' skipped.", plant); continue; }
                playable.Add(plant);
            }
        }

        private void OnValidate() => built = false;

        internal static BasementPlantCatalog Load()
        {
            var catalog = Resources.Load<BasementPlantCatalog>(ResourcePath);
            if (catalog != null) return catalog;
            Debug.LogWarning("Resources/BasementPlantCatalog is missing; no basement plants are available.");
            return CreateInstance<BasementPlantCatalog>();
        }
    }
}
