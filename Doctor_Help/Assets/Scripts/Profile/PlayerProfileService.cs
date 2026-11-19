using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    public sealed class PlayerProfileService : MonoBehaviour, IDayProfile, ISerializationCallbackReceiver
    {
        public static PlayerProfileService Instance { get; private set; }
        public event Action Changed;
        public bool IsLoaded => session != null;
        public string SavePath => store.SavePath;
        public int Money => Data.money;
        public float Rating => Data.rating;
        public int CurrentDay => Data.currentDay;
        public bool ThrowRock => IsLoaded && Data.throwRock;
        public bool Clean => IsLoaded && Data.clean;
        internal bool ShopsOpen => IsLoaded && CurrentDay >= 2;
        internal MainObjective CurrentMainObjective => Data.mainObjective;
        internal WalterQuest WalterContract => Data.walterQuest;
        private ClinicRankingsCatalog clinicRankings;
        public int DiffProgress => Data.diffProgress;
        public int DiffStreakDays => Data.diffStreakDays;
        public int DiffStreakIncorrectPatients => Data.diffStreakMistakeDay == 0 ? 0 : 1;
        internal DifficultyProfile DifficultySettings => difficultySettings != null
            ? difficultySettings : difficultySettings = DifficultyProfile.Load();
        public int PatientsKilled => Data.patientsKilled;
        public int PatientsSaved => Data.patientsSaved;
        public int GhostNumber => Data.ghostNumber;
        internal bool IsStreetRockThrowingActive => IsLoaded && Data.IsStreetRockThrowingActive;

        private PlayerProfileSession session;
        private PlayerProfileStore store;
        private DifficultyProfile difficultySettings;
        [SerializeField, HideInInspector] private TreatmentJournal journal;
        [SerializeField, HideInInspector] private PlayerProfileData workingDay;
        [SerializeField] private bool testNpcSmellyBleed;
        internal bool TestNpcSmellyBleed => testNpcSmellyBleed;
        [SerializeField] private bool hauntedTest;
        internal bool HauntedTest => hauntedTest;
        internal IReadOnlyList<ClinicDirtDay> ClinicDirtDays => Data.clinicDirtDays;
        public TreatmentJournal.Day GetTreatmentSnapshot() => journal?.GetSnapshot();
        private PlayerProfileData Data => session?.WorkingProfile ??
            throw new InvalidOperationException("Player profile could not be loaded. Check the Console.");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            var defaults = Resources.Load<PlayerStartingProfile>("PlayerStartingProfile");
            clinicRankings = defaults?.ClinicRankings;
            store = new PlayerProfileStore(ProfileDirectory(), defaults != null ? defaults.CreateProfile : null);
            journal = new TreatmentJournal();
            try
            {
                session = new PlayerProfileSession(store.Load(), store.Save, UpdateObjectives);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Player profile initialization failed. Existing saves preserved. {ex}");
            }
        }

        private void OnEnable()
        {
            if (Instance == null) Instance = this;
            if (session != null || workingDay == null) return;
            // Unity code reload in Play Mode preserves the working day, without saving it.
            var defaults = Resources.Load<PlayerStartingProfile>("PlayerStartingProfile");
            clinicRankings = defaults?.ClinicRankings;
            store = new PlayerProfileStore(ProfileDirectory(), defaults != null ? defaults.CreateProfile : null);
            try { store.Load(); session = new PlayerProfileSession(workingDay, store.Save, UpdateObjectives); }
            catch (Exception ex) { Debug.LogError("Cannot resume working profile after code reload: " + ex); }
        }

        private static string ProfileDirectory()
        {
#if UNITY_EDITOR
            if (TutorialPreviewSession.IsActive)
                return TutorialPreviewSession.SaveDirectory;
            if (UnityEditor.SessionState.GetBool("DoctorHelp.PlayerHomePreview", false))
                return System.IO.Path.GetFullPath("Library/PlayerHomePreview/Save");
#endif
            return Application.persistentDataPath;
        }
        public void OnBeforeSerialize() { if (session != null) workingDay = session.WorkingProfile.Copy(); }
        public void OnAfterDeserialize() { }

        // Changes to a snapshot never affect the working day.
        public PlayerProfileData GetSnapshot() => Data.Copy();

        internal ClinicDirtDay EnsureClinicDirtDay(int? visitorCount = null)
        {
            var profile = Data;
            var today = profile.clinicDirtDays.Find(entry => entry.day == profile.currentDay);
            if (today != null && !visitorCount.HasValue) return today;
            if (today == null)
            {
                today = new ClinicDirtDay { day = profile.currentDay };
                profile.clinicDirtDays.Add(today);
            }

            int count = visitorCount ?? (ClinicDemand.IndoorVisitors(profile.rating) + ClinicSignLevel);
            today.AddVisitors(count, () => UnityEngine.Random.Range(0, 100));
            return today;
        }

        internal float ClinicDirtPenalty()
        {
            EnsureClinicDirtDay();
            float total = 0f;
            foreach (var entry in ClinicDirtDays) total += entry.StarPenalty;
            return total;
        }

        public void SetMoney(int value) { Data.money = value; Commit(); }

        public void SetThrowRock(bool enabled)
        {
            if (!IsLoaded || Data.throwRock == enabled) return;
            Data.throwRock = enabled;
            NotifyChanged();
        }

        public void SetClean(bool enabled)
        {
            if (!IsLoaded || Data.clean == enabled) return;
            Data.clean = enabled;
            NotifyChanged();
        }

        public void SetRating(float value)
        {
            if (float.IsNaN(value))
                throw new ArgumentOutOfRangeException(nameof(value));

            float rating = Mathf.Clamp(value, 0f, PlayerProfileData.MaxRating);
            if (Data.rating == rating) return;
            Data.rating = rating;
            Commit();
        }

        public float GetRivalRating(string clinicId, float startingRating)
        {
            var entry = Data.rivalRatings.Find(rival => rival.clinicId == clinicId);
            return entry?.rating ?? startingRating;
        }

        public void SetRivalRating(string clinicId, float rating)
        {
            if (string.IsNullOrWhiteSpace(clinicId)) throw new ArgumentException("A clinic ID is required.", nameof(clinicId));
            if (!PlayerProfileData.IsRatingValid(rating)) throw new ArgumentOutOfRangeException(nameof(rating));
            var entry = Data.rivalRatings.Find(rival => rival.clinicId == clinicId);
            if (entry == null)
                Data.rivalRatings.Add(new RivalClinicRating { clinicId = clinicId, rating = rating });
            else
            {
                if (entry.rating == rating) return;
                entry.rating = rating;
            }
            Commit();
        }

        public bool TryGive(PatientExaminationProfile patient, PillProfile pills,
            System.Collections.Generic.IReadOnlyList<string> ids, out string error)
        {
            error = null;
            if (!IsLoaded || patient == null || patient.Definition == null || pills == null ||
                string.IsNullOrWhiteSpace(patient.ConsultationId) || ids == null)
            { error = "Patient or medicine catalogue unavailable."; return false; }
            if (patient.Haunting != null && !patient.HauntingCleared)
            { error = "Remove all ghosts before giving medicine."; return false; }
            if (journal.Contains(patient.ConsultationId))
            {
                if (journal.MatchesMedication(patient.ConsultationId, ids)) return true;
                error = "This consultation already has a different decision."; return false;
            }
            if (!session.TryGive(pills, ids, () => journal.Record(CurrentDay, patient, pills, ids), out error,
                patient.CanConfirmWithoutMedicine)) return false;
            patient.StopPatience();
            Commit();
            return true;
        }

        public bool TryReportToPolice(PatientExaminationProfile patient, PoliceReportReason reason, out string error)
        {
            error = null;
            if (!IsLoaded || patient == null || patient.Definition == null || string.IsNullOrWhiteSpace(patient.ConsultationId))
            { error = "Patient unavailable."; return false; }
            if (reason != PoliceReportReason.VampireInfected && reason != PoliceReportReason.ZombieInfected)
            { error = "Select Vampire infected or Zombie infected."; return false; }
            if (journal.Contains(patient.ConsultationId))
            {
                if (journal.MatchesPoliceReport(patient.ConsultationId, reason)) return true;
                error = "This consultation already has a different decision."; return false;
            }
            try { journal.RecordPoliceReport(CurrentDay, patient, reason); }
            catch (Exception ex) { error = "Cannot record police report. Retry: " + ex.Message; return false; }
            patient.StopPatience();
            Commit();
            return true;
        }

        public bool TryDecline(PatientExaminationProfile patient, out string error) =>
            TryRecordDeparture(patient, TreatmentDecisionKind.Decline, out error);

        internal bool TryRecordRockHit(PatientExaminationProfile patient, out string error) =>
            TryRecordDeparture(patient, TreatmentDecisionKind.HitPatientWithRock, out error);

        private bool TryRecordDeparture(PatientExaminationProfile patient, TreatmentDecisionKind decision, out string error)
        {
            error = null;
            if (!IsLoaded || patient == null || patient.Definition == null || string.IsNullOrWhiteSpace(patient.ConsultationId))
            { error = "Patient unavailable."; return false; }

            try { journal.RecordDecline(CurrentDay, patient, decision); }
            catch (Exception ex) { error = "Cannot record patient departure. Retry: " + ex.Message; return false; }
            patient.StopPatience();
            return true;
        }

        public void AddMoney(int amount)
        {
            Data.money = checked(Data.money + amount);
            Data.walterQuest.RecordEarnings(amount);
            Commit();
        }

        internal bool AcceptWalterQuest()
        {
            if (!IsLoaded || !Data.walterQuest.Accept(CurrentDay)) return false;
            Commit();
            return true;
        }

        internal bool ClaimWalterReward()
        {
            if (!IsLoaded || Data.walterQuest.state != WalterQuestState.ReadyToClaim ||
                Money > int.MaxValue - WalterQuest.Reward) return false;
            Data.money += WalterQuest.Reward;
            Data.walterQuest.state = WalterQuestState.Claimed;
            Commit();
            return true;
        }

        public void RecordPatientKilled() { Data.patientsKilled = checked(Data.patientsKilled + 1); Commit(); }
        public void RecordPatientSaved() { Data.patientsSaved = checked(Data.patientsSaved + 1); Commit(); }

        internal int ShopItemQuantity(string id) => Data.shopItems.Find(entry => entry.itemId == id)?.quantity ?? 0;
        internal string EquippedFurnitureId(string slot) => Data.equippedFurniture.Find(entry => entry.slotId == slot)?.itemId;
        internal int ClinicSignLevel => FurnitureLevel(ClinicDemand.SignSlot);
        internal float ReceptionRatingBonus => 0.5f * FurnitureLevel(ClinicDemand.ReceptionSlot);
        internal int ReceptionistLevel => FurnitureLevel(ClinicDemand.ReceptionistSlot);
        internal int ElevatorLevel => FurnitureLevel(ClinicDemand.ElevatorSlot);

        private ClinicShopItem[] shopDefinitions;
        internal IReadOnlyList<ClinicShopItem> ShopDefinitions =>
            shopDefinitions ??= Array.ConvertAll(Resources.LoadAll<GameObject>("ShopItems"),
                prefab => prefab.GetComponent<ClinicShopItem>());

        private int FurnitureLevel(string slot) => FurnitureLevel(Data, slot);

        private int FurnitureLevel(PlayerProfileData profile, string slot)
        {
            string equippedId = profile.equippedFurniture.Find(entry => entry.slotId == slot)?.itemId;
            int defaultLevel = 0;
            foreach (var item in ShopDefinitions)
            {
                if (item == null || !item.IsConfigured || item.FurnitureSlot != slot) continue;
                if (item.Id == equippedId) return item.FurnitureLevel;
                if (item.IsDefaultFurniture) defaultLevel = item.FurnitureLevel;
            }
            return defaultLevel;
        }

        internal bool IsUpgradeUnlocked(ClinicShopItem item) =>
            !item.IsEquippable || session.CanBuyUpgrade(item, ShopDefinitions);

        internal bool TryBuyShopItem(ClinicShopItem item, out string error)
        {
            error = IsLoaded ? "Shops open on day 2." : "The player profile is unavailable.";
            if (!ShopsOpen || !session.TryBuy(item, ShopDefinitions, out error)) return false;
            Commit();
            return true;
        }

        internal bool TryEquipFurniture(ClinicShopItem item)
        {
            if (session == null || !session.TryEquip(item)) return false;
            Commit();
            return true;
        }

        public bool CompleteDay(int incorrectPatients, int goldDelta, float? dailyRating = null,
            bool hadConsultations = true, int goldEarned = 0)
        {
            if (!IsLoaded) return false;
            try { session.CompleteDay(incorrectPatients, goldDelta, dailyRating, hadConsultations, DifficultySettings.SuccessStreak, goldEarned); }
            catch (Exception ex) { Debug.LogError($"Player profile save failed: {ex}"); return false; }
            // Future patient-result calculations must read history before this checkpoint.
            journal.Clear();
            NotifyChanged();
            return true;
        }

        internal bool TryReset()
        {
            if (store == null) return false;
            try { session = new PlayerProfileSession(store.Reset(), store.Save, UpdateObjectives); }
            catch (Exception error)
            {
                Debug.LogError($"Player profile reset failed: {error}");
                return false;
            }

            journal = new TreatmentJournal();
            workingDay = session.WorkingProfile.Copy();
            NotifyChanged();
            return true;
        }

        private void UpdateObjectives(PlayerProfileData profile) =>
            MainObjectives.Advance(profile, FurnitureLevel(profile, ClinicDemand.ReceptionistSlot), clinicRankings);

        private void Commit()
        {
            UpdateObjectives(Data);
            NotifyChanged();
        }
        private void NotifyChanged()
        {
            if (Changed == null) return;
            foreach (Action listener in Changed.GetInvocationList())
            {
                try { listener(); }
                catch (Exception error) { Debug.LogException(error); }
            }
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}


