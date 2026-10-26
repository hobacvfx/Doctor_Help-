using System;
using UnityEngine;

namespace DoctorHelp
{
    public sealed class PlayerProfileService : MonoBehaviour
    {
        public static PlayerProfileService Instance { get; private set; }
        public event Action Changed;
        public bool IsLoaded => session != null;
        public string SavePath => store.SavePath;
        public int Money => Data.money;
        public int CurrentDay => Data.currentDay;
        public int PatientsKilled => Data.patientsKilled;
        public int PatientsSaved => Data.patientsSaved;

        private PlayerProfileSession session;
        private PlayerProfileStore store;
        private PlayerProfileData Data => session?.WorkingProfile ??
            throw new InvalidOperationException("Player profile could not be loaded. Check the Console.");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            store = new PlayerProfileStore(Application.persistentDataPath);
            try
            {
                session = new PlayerProfileSession(store.Load(), store.Save);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Player profile initialization failed. Existing saves preserved. {ex}");
            }
        }

        // Changes to a snapshot never affect the working day.
        public PlayerProfileData GetSnapshot() => Data.Copy();

        public int GetMedicineQuantity(string medicineId)
        {
            ValidateId(medicineId);
            var stock = Data.medicines.Find(item => item.medicineId == medicineId);
            return stock == null ? 0 : stock.quantity;
        }

        public void AddMoney(int amount)
        {
            Data.money = checked(Data.money + amount);
            Commit();
        }

        public void AddMedicine(string medicineId, int quantity)
        {
            ValidateId(medicineId);
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            var stock = Data.medicines.Find(item => item.medicineId == medicineId);
            if (stock == null)
                Data.medicines.Add(new MedicineStock { medicineId = medicineId, quantity = quantity });
            else stock.quantity = checked(stock.quantity + quantity);
            Commit();
        }

        public bool TryConsumeMedicine(string medicineId, int quantity = 1)
        {
            ValidateId(medicineId);
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            var stock = Data.medicines.Find(item => item.medicineId == medicineId);
            if (stock == null || stock.quantity < quantity) return false;
            stock.quantity -= quantity;
            Commit();
            return true;
        }

        public void RecordPatientKilled() { Data.patientsKilled = checked(Data.patientsKilled + 1); Commit(); }
        public void RecordPatientSaved() { Data.patientsSaved = checked(Data.patientsSaved + 1); Commit(); }

        public bool CompleteDay()
        {
            if (!IsLoaded) return false;
            try { session.CompleteDay(); }
            catch (Exception ex) { Debug.LogError($"Player profile save failed: {ex}"); return false; }
            Changed?.Invoke();
            return true;
        }

        private void Commit() => Changed?.Invoke();
        private static void ValidateId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Medicine ID is required.", nameof(id));
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
