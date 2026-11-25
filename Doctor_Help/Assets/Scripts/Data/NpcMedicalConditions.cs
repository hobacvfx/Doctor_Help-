using System;
using System.Collections.Generic;

namespace DoctorHelp
{
    // Stable design IDs. Display names/order can change without changing these rules.
    public static class NpcMedicalConditions
    {
        public const string Vampire = "e659156e77fa4a84b852a36f23090055";
        public const string Zombie = "b09d50f81fe14efab382c302f5b19ef0";
        public const string Haunted = "420888271cbf4de8947e7ef841c5e315";
        public const string BrainRot = "42ee7a9a38694d838b495736ea12cb20";
        public const string HauntedSymptom = "8439037532414cfe808eec0c66b75c5e";
        public const string Rapid = "99740cad57794442b282b9c1caddbbe0";
        public const string Slow = "ec557b40a6ed406fbb270e751a04cb18";
        public const string NoHeartbeat = "22eaad3867e2482cb6baaf07d3a04bd3";
        public const string HighTemperature = "ba01540c62484c209e3ec60d8200cd10";
        public const string LowTemperature = "81086fbd9e514e13b5674857847a7ead";

        public static HashSet<string> Resolve(IReadOnlyList<string> sicknessIds, AllSickness medicalData)
        {
            if (sicknessIds == null || medicalData == null || medicalData.SymptomProfile == null)
                throw new InvalidOperationException("NPC conditions require a disease list and medical catalogue.");
            var diseases = new HashSet<string>();
            var symptoms = new HashSet<string>();
            HashSet<string> prioritySymptoms = null;
            foreach (var id in sicknessIds)
            {
                if (string.IsNullOrWhiteSpace(id) || !diseases.Add(id))
                    throw new InvalidOperationException("Disease IDs must be non-empty and distinct.");
                var sickness = medicalData.Find(id);
                if (sickness == null) throw new InvalidOperationException("NPC references a missing sickness: " + id);
                var own = new HashSet<string>();
                foreach (var symptom in sickness.SymptomIds)
                {
                    if (medicalData.SymptomProfile.Find(symptom) == null)
                        throw new InvalidOperationException("Sickness references a missing symptom: " + symptom);
                    own.Add(symptom);
                    symptoms.Add(symptom);
                }
                if (id == Vampire || id == Zombie) prioritySymptoms = own;
            }
            if (diseases.Count > DifficultyProfile.MaxDifficulty)
                throw new InvalidOperationException("An NPC can have at most four diseases, including Dirty.");
            if (diseases.Contains(Vampire) && diseases.Contains(Zombie))
                throw new InvalidOperationException("Vampire Infection and Zombie Infection cannot coexist.");
            if (prioritySymptoms != null)
            {
                ApplyPriority(symptoms, prioritySymptoms, Rapid, Slow, NoHeartbeat);
                ApplyPriority(symptoms, prioritySymptoms, HighTemperature, LowTemperature);
            }
            if (symptoms.Contains(NoHeartbeat))
            {
                symptoms.Remove(Rapid);
                symptoms.Remove(Slow);
            }
            if (symptoms.Contains(Rapid) && symptoms.Contains(Slow))
                throw new InvalidOperationException("These diseases have conflicting rapid and slow heartbeats.");
            if (symptoms.Contains(HighTemperature) && symptoms.Contains(LowTemperature))
                throw new InvalidOperationException("These diseases have conflicting high and low temperatures.");
            return symptoms;
        }

        private static void ApplyPriority(HashSet<string> all, HashSet<string> priority, params string[] category)
        {
            bool overrides = false;
            foreach (var id in category) overrides |= priority.Contains(id);
            if (!overrides) return;
            foreach (var id in category)
                if (!priority.Contains(id)) all.Remove(id);
        }

        public static bool CanCombine(IReadOnlyList<string> sicknessIds, AllSickness medicalData, out string error)
        {
            try { Resolve(sicknessIds, medicalData); error = null; return true; }
            catch (InvalidOperationException ex) { error = ex.Message; return false; }
        }
    }
}
