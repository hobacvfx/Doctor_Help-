using System;
using System.Collections.Generic;
using UnityEditor;

namespace DoctorHelp.Editor
{
    // Authoring only: consultations never roll or replace profile diseases.
    public static class NpcSicknessAssignment
    {
        public static List<string> Create(NpcProfile profile, Random random, string first = null)
        {
            var medical = profile.AllSickness;
            if (medical == null || medical.Sicknesses.Count == 0)
                throw new InvalidOperationException("A medical catalogue with diseases is required.");
            if (first == null) first = medical.Sicknesses[random.Next(medical.Sicknesses.Count)].Id;
            NpcMedicalConditions.Resolve(new[] { first }, medical);
            var result = new List<string> { first };
            if (first != NpcMedicalConditions.Haunted && random.NextDouble() >= profile.SecondSicknessChance) return result;
            var compatible = new List<string>();
            foreach (var disease in medical.Sicknesses)
                if (disease != null && disease.Id != first &&
                    NpcMedicalConditions.CanCombine(new[] { first, disease.Id }, medical, out _))
                    compatible.Add(disease.Id);
            if (compatible.Count == 0)
                throw new InvalidOperationException("No compatible second disease exists for " + first);
            result.Add(compatible[random.Next(compatible.Count)]);
            return result;
        }

        public static List<string[]> FindCompatibleSets(AllSickness medical, int difficulty)
        {
            if (difficulty < 1 || difficulty > DifficultyProfile.MaxDifficulty)
                throw new ArgumentOutOfRangeException(nameof(difficulty));

            var result = new List<string[]>();
            var selected = new string[difficulty];
            var diseases = medical.Sicknesses;
            Collect(0, 0);
            return result;

            void Collect(int slot, int start)
            {
                if (slot == selected.Length)
                {
                    // A later disease can resolve symptom conflicts through the priority rules.
                    if (NpcMedicalConditions.CanCombine(selected, medical, out _))
                        result.Add((string[])selected.Clone());
                    return;
                }
                for (int i = start; i <= diseases.Count - (selected.Length - slot); i++)
                {
                    selected[slot] = diseases[i].Id;
                    Collect(slot + 1, i + 1);
                }
            }
        }

        public static void Write(SerializedProperty entry, IReadOnlyList<string> ids)
        {
            var list = entry.FindPropertyRelative("sicknessIds");
            list.arraySize = ids.Count;
            for (int i = 0; i < ids.Count; i++) list.GetArrayElementAtIndex(i).stringValue = ids[i];
        }
    }
}
