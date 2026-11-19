using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    [System.Serializable]
    public sealed class PillDefinition : NamedDataEntry
    {
        [SerializeField, Min(0)] private int price = 1;
        [SerializeField, TextArea(2, 4), Tooltip("Wrap disease names in <disease>...</disease> to highlight them in Give Medicines.")]
        private string description;
        [SerializeField] private Sprite sprite;
        public int Price => price;
        public string Description => description;
        public Sprite Sprite => sprite;
    }

    [CreateAssetMenu(fileName = "PillProfile", menuName = "Doctor Help/Data/Pill Profile")]
    public sealed class PillProfile : ScriptableObject
    {
        [SerializeField] private List<PillDefinition> pills = new List<PillDefinition>();
        public IReadOnlyList<PillDefinition> Pills => pills.AsReadOnly();
        public PillDefinition Find(string id) => pills.Find(entry => entry != null && entry.Id == id);
        internal bool TryGetCost(IReadOnlyList<string> ids, out long cost)
        {
            cost = 0;
            var unique = new HashSet<string>();
            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id) || !unique.Add(id)) return false;
                var pill = Find(id);
                if (pill == null || pill.Price < 0) return false;
                cost += pill.Price;
            }
            return true;
        }
        private void OnValidate()
        {
            var used = new HashSet<string>();
            foreach (var entry in pills) entry?.EnsureUniqueId(used);
        }
    }
}
