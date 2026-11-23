using System;
using System.Collections.Generic;

namespace DoctorHelp
{
    public static class TreatmentSlotRequirements
    {
        public static int Count(PatientExaminationProfile patient)
        {
            if (patient == null || patient.Definition == null || patient.Profile?.AllSickness == null)
                throw new InvalidOperationException("Receiving slots require an initialized patient.");
            var medical = patient.Profile.AllSickness;
            var medicines = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in patient.Definition.SicknessIds)
            {
                var disease = medical.Find(id);
                if (disease == null) throw new InvalidOperationException("Unknown patient disease: " + id);
                if (disease.Treatment != SicknessTreatment.RequiredPills) continue;
                if (disease.PillIds.Count == 0) throw new InvalidOperationException("Empty medicine recipe: " + disease.DisplayName);
                foreach (string medicine in disease.PillIds)
                {
                    if (medical.PillProfile.Find(medicine) == null)
                        throw new InvalidOperationException("Unknown medicine in patient recipe: " + medicine);
                    medicines.Add(medicine);
                }
            }
            if (medicines.Count == 0 && patient.CanConfirmWithoutMedicine) return 0;
            // Police-only/healthy cases still accept one attempted medicine treatment.
            return Math.Max(1, medicines.Count);
        }
    }
}
