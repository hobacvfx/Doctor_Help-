using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    [CreateAssetMenu(fileName = "PillProfile", menuName = "Doctor Help/Data/Pill Profile")]
    public sealed class PillProfile : ScriptableObject
    {
        [SerializeField] private List<NamedDataEntry> pills = new List<NamedDataEntry>();
        public IReadOnlyList<NamedDataEntry> Pills => pills.AsReadOnly();
        public NamedDataEntry Find(string id) => pills.Find(entry => entry != null && entry.Id == id);
        private void OnValidate()
        {
            var used = new HashSet<string>();
            foreach (var entry in pills) entry?.EnsureUniqueId(used);
        }
    }
}
