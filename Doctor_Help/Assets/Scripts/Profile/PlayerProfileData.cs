using System;
using System.Collections.Generic;

namespace DoctorHelp
{
    [Serializable]
    public sealed class MedicineStock
    {
        public string medicineId;
        public int quantity;
    }

    // Plain JSON data. Money is signed because debt is part of the game design.
    [Serializable]
    public sealed class PlayerProfileData
    {
        public int schemaVersion = 1;
        public int money;
        public int currentDay = 1;
        public int patientsKilled;
        public int patientsSaved;
        public List<MedicineStock> medicines = new List<MedicineStock>();

        public PlayerProfileData Copy()
        {
            var copy = new PlayerProfileData
            {
                schemaVersion = schemaVersion, money = money, currentDay = currentDay,
                patientsKilled = patientsKilled, patientsSaved = patientsSaved
            };
            foreach (var stock in medicines)
                copy.medicines.Add(new MedicineStock { medicineId = stock.medicineId, quantity = stock.quantity });
            return copy;
        }

        public bool IsValid()
        {
            if (schemaVersion != 1 || currentDay < 1 || patientsKilled < 0 ||
                patientsSaved < 0 || medicines == null) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var stock in medicines)
                if (stock == null || string.IsNullOrWhiteSpace(stock.medicineId) ||
                    stock.quantity < 0 || !ids.Add(stock.medicineId)) return false;
            return true;
        }
    }
}
