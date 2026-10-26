using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    [Serializable]
    public sealed class NamedDataEntry
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        public string Id => id;
        public string DisplayName => displayName;

        internal void EnsureUniqueId(HashSet<string> used)
        {
            if (!string.IsNullOrWhiteSpace(id) && used.Add(id)) return;
            do { id = Guid.NewGuid().ToString("N"); } while (!used.Add(id));
        }
    }
}
