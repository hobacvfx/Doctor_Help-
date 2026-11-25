using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    [CreateAssetMenu(menuName = "Doctor Help/Medical/Symptom Animation Variants")]
    public sealed class SymptomAnimationVariants : ScriptableObject
    {
        [SerializeField] private GameObject[] prefabs = Array.Empty<GameObject>();
        public int Count => prefabs == null ? 0 : prefabs.Length;

        public void Validate()
        {
            if (Count == 0)
                throw new InvalidOperationException(name + " needs at least one symptom prefab.");
            var unique = new HashSet<GameObject>();
            foreach (var prefab in prefabs)
                if (prefab == null || !unique.Add(prefab))
                    throw new InvalidOperationException(name + " contains a missing or repeated symptom prefab.");
        }

        // Integer Random.Range gives every authored variant the same probability.
        public GameObject Pick()
        {
            Validate();
            return prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
        }
    }
}
