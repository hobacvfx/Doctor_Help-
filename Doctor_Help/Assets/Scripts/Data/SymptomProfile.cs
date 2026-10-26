using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    [CreateAssetMenu(fileName = "SymptomProfile", menuName = "Doctor Help/Data/Symptom Profile")]
    public sealed class SymptomProfile : ScriptableObject
    {
        [SerializeField] private List<NamedDataEntry> symptoms = new List<NamedDataEntry>();
        public IReadOnlyList<NamedDataEntry> Symptoms => symptoms.AsReadOnly();
        public NamedDataEntry Find(string id) => symptoms.Find(entry => entry != null && entry.Id == id);
        private void OnValidate()
        {
            var used = new HashSet<string>();
            foreach (var entry in symptoms) entry?.EnsureUniqueId(used);
        }
    }
}
