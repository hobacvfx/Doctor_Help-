using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    public enum SicknessTreatment { RequiredPills = 0, ReportToPolice = 1 }

    [Serializable]
    public sealed class SicknessDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private SicknessTreatment treatment;
        [SerializeField] private List<string> symptomIds = new List<string>();
        [SerializeField] private List<string> pillIds = new List<string>();
        public string Id => id;
        public string DisplayName => displayName;
        public SicknessTreatment Treatment => treatment;
        public IReadOnlyList<string> SymptomIds => symptomIds.AsReadOnly();
        public IReadOnlyList<string> PillIds => pillIds.AsReadOnly();
        internal void EnsureUniqueId(HashSet<string> used)
        {
            if (!string.IsNullOrWhiteSpace(id) && used.Add(id)) return;
            do { id = Guid.NewGuid().ToString("N"); } while (!used.Add(id));
        }
    }
}
