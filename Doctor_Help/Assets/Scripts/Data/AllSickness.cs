using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    [CreateAssetMenu(fileName = "AllSickness", menuName = "Doctor Help/Data/All Sickness")]
    public sealed class AllSickness : ScriptableObject
    {
        [SerializeField] private SymptomProfile symptomProfile;
        [SerializeField] private PillProfile pillProfile;
        [SerializeField] private List<SicknessDefinition> sicknesses = new List<SicknessDefinition>();
        public SymptomProfile SymptomProfile => symptomProfile;
        public PillProfile PillProfile => pillProfile;
        public IReadOnlyList<SicknessDefinition> Sicknesses => sicknesses.AsReadOnly();
        public SicknessDefinition Find(string id) => sicknesses.Find(entry => entry != null && entry.Id == id);

        // This is a data query, not an inventory transaction or a patient outcome.
        // Every listed pill is required once. Empty/invalid recipes never count as a cure.
        public bool HasRequiredPills(string sicknessId, IEnumerable<string> providedPillIds)
        {
            var sickness = Find(sicknessId);
            if (sickness == null || sickness.Treatment != SicknessTreatment.RequiredPills ||
                pillProfile == null || providedPillIds == null || sickness.PillIds.Count == 0)
                return false;
            var provided = new HashSet<string>(providedPillIds);
            var required = new HashSet<string>();
            foreach (var id in sickness.PillIds)
                if (string.IsNullOrWhiteSpace(id) || pillProfile.Find(id) == null ||
                    !required.Add(id) || !provided.Contains(id)) return false;
            return true;
        }

        private void OnValidate()
        {
            var used = new HashSet<string>();
            foreach (var entry in sicknesses) entry?.EnsureUniqueId(used);
        }
    }
}
