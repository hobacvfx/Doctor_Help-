using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    public enum SicknessTreatment { RequiredPills = 0, ReportToPolice = 1, [InspectorName("Remove Flies")] RemoveGhosts = 2 }

    [Serializable]
    public sealed class SicknessDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 4)] private string referenceDescription;
        [SerializeField] private SicknessTreatment treatment;
        [SerializeField] private List<string> symptomIds = new List<string>();
        [SerializeField] private List<string> pillIds = new List<string>();
        [SerializeField, Min(.1f)] private float ghostSpeedDiff1 = .6f;
        [SerializeField, Min(.1f)] private float ghostSpeedDiff2 = .85f;
        [SerializeField, Min(.1f)] private float ghostSpeedDiff3 = 1.1f;
        [SerializeField, Min(.1f)] private float ghostSpeedDiff4 = 1.45f;
        public string Id => id;
        public string DisplayName => displayName;
        public string ReferenceDescription => referenceDescription;
        public SicknessTreatment Treatment => treatment;
        public IReadOnlyList<string> SymptomIds => symptomIds.AsReadOnly();
        public IReadOnlyList<string> PillIds => pillIds.AsReadOnly();
        public float GhostSpeed(int difficulty) => Mathf.Max(.1f, difficulty switch
        {
            1 => ghostSpeedDiff1,
            2 => ghostSpeedDiff2,
            3 => ghostSpeedDiff3,
            _ => ghostSpeedDiff4
        });
        internal void EnsureUniqueId(HashSet<string> used)
        {
            if (!string.IsNullOrWhiteSpace(id) && used.Add(id)) return;
            do { id = Guid.NewGuid().ToString("N"); } while (!used.Add(id));
        }
    }
}
