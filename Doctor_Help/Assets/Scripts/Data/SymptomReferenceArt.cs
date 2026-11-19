using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    [CreateAssetMenu(fileName = "SymptomReferenceArt", menuName = "Doctor Help/Data/Symptom Reference Art")]
    public sealed class SymptomReferenceArt : ScriptableObject
    {
        [Serializable]
        public sealed class Illustration
        {
            [SerializeField] private string symptomId;
            [SerializeField] private Sprite sprite;
            public string SymptomId => symptomId;
            public Sprite Sprite => sprite;
        }

        [SerializeField] private SymptomProfile source;
        [SerializeField] private List<Illustration> illustrations = new List<Illustration>();
        public SymptomProfile Source => source;
        public IReadOnlyList<Illustration> Illustrations => illustrations;

        // Missing/empty art is intentional: the symptom is described in text only.
        public Sprite Find(string symptomId)
        {
            foreach (var item in illustrations)
                if (item != null && item.SymptomId == symptomId) return item.Sprite;
            return null;
        }

        public void ValidateFor(SymptomProfile expected)
        {
            if (source == null || source != expected)
                throw new InvalidOperationException("Casebook illustrations must reference the disease catalogue's SymptomProfile.");
            var ids = new HashSet<string>();
            foreach (var item in illustrations)
                if (item == null || source.Find(item.SymptomId) == null || !ids.Add(item.SymptomId))
                    throw new InvalidOperationException("Casebook art contains an unknown or duplicate symptom ID. Repair SymptomReferenceArt in the Inspector.");
        }
    }
}
