using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    public sealed class PlayerProfileService : MonoBehaviour, IDayProfile, ISerializationCallbackReceiver
    {
        public static PlayerProfileService Instance { get; private set; }
        public event Action Changed;
        // Raised after the profile is reset to a new game, so systems with their own save files reset too.
        internal event Action ProfileReset;
        public bool IsLoaded => session != null;
        public string SavePath => store.SavePath;
        // True when a profile change is saved within a second (outside the working day).
        internal bool CanCommitImmediately => IsLoaded && !workingDayOpen;
        internal int BasementRepairsPaid => Data.basementRepairsPaid;
        // Owned basement plants. Read only: change them through the TryBuy/TryUpgrade methods below.
        internal IReadOnlyList<BasementPlantRecord> BasementPlants => Data.basementPlants;
        public int Money => Data.money;
        public int Water => Data.water;
        public int Electricity => Data.electricity;
        public float Dirt => Data.dirt;
        public float Rating => Data.rating;
        public int CurrentDay => Data.currentDay;
        public string ClinicName => Data.clinicName ?? "";

        internal bool TrySetClinicName(string input, out string error)
        {
            if (!ClinicNameRules.TryValidate(input, out var name, out error)) return false;
            if (!IsLoaded || workingDayOpen)
            {
                error = "Cannot name your clinic right now. Please try again.";
                return false;
            }
            var candidate = Data.Copy();
            candidate.clinicName = name;
            try
            {
                store.Flush();
                store.ConsumeWriteFailure();
                store.AutosaveNow(candidate);
                if (store.ConsumeWriteFailure())
                {
                    QueueSave(RetryDelay);
                    error = "Could not save your clinic name. Please try again.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Could not save clinic name: " + ex);
                error = "Could not save your clinic name. Please try again.";
                return false;
            }
            Data.clinicName = name;
            ClearPendingSave();
            NotifyChanged();
            return true;
        }
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
        internal int DartsLeft => IsLoaded ? Data.DartsLeft : 0;
        internal bool IsDartBalloonPopped(int reward) => IsLoaded && Data.IsDartBalloonPopped(reward);

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
        // True from Open until the day result is checkpointed; quitting then returns to the state at Open.
        [SerializeField, HideInInspector] private bool workingDayOpen; // Survives Play Mode code reload.
        // Changes are coalesced: the first change schedules one autosave SaveDelay seconds later.
        private bool saveQueued;
        private float saveDueTime;
        private bool playTimeUnsaved; // Play time rides along with other saves, and pause/quit.
        private const float SaveDelay = 1f;
        private const float RetryDelay = 5f;
        internal double PlaySeconds => Data.playSeconds;
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

        // Also used by saves kept beside the profile (basement), so previews stay isolated together.
        internal static string ProfileDirectory()
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
        public void SetWater(int value) { Data.water = Math.Max(0, value); Commit(); }
        public void SetElectricity(int value) { Data.electricity = Math.Max(0, value); Commit(); }
        public void SetDirt(float value) { Data.dirt = Mathf.Clamp(value, 0f, PlayerProfileData.MaxDirt); Commit(); }

        public void AddWater(int amount)
        {
            if (!IsLoaded || amount <= 0) return;
            Data.water = checked(Data.water + amount);
            Commit();
        }

        public void AddElectricity(int amount)
        {
            if (!IsLoaded || amount <= 0) return;
            Data.electricity = checked(Data.electricity + amount);
            Commit();
        }

        // Pays for a shower and removes its dirt in one save, so quitting mid-shower cannot lose either.
        internal bool TryShower(int waterCost, float dirtRemoved)
        {
            if (!IsLoaded || waterCost < 0 || dirtRemoved < 0f || Data.water < waterCost) return false;
            Data.water -= waterCost;
            Data.dirt = Mathf.Max(0f, Data.dirt - dirtRemoved);
            Commit();
            return true;
        }

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
            if (patient.Haunting != null && !patient.HauntingCleared && !patient.Definition.RequiresPoliceReport)
            { error = "Remove all ghosts before giving medicine."; return false; }
            if (journal.Contains(patient.ConsultationId))
            {
                if (journal.MatchesMedication(patient.ConsultationId, ids)) return true;
                error = "This consultation already has a different decision."; return false;
            }
            if (!session.TryGive(pills, ids, () => journal.Record(CurrentDay, patient, pills, ids), out error,
                patient.CanConfirmWithoutMedicine)) return false;
            patient.HasReceivedMedicine = true;
            patient.ClearStagedMedicines();
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

        internal int FortuneAdSpins => IsLoaded ? Data.fortuneAdSpins : 0;

        internal bool TryEarnFortuneAdSpin()
        {
            if (!IsLoaded || Data.fortuneAdSpins == int.MaxValue) return false;
            Data.fortuneAdSpins++;
            Commit();
            return true;
        }

        // Cost/credit consumption and payout are one mutation and one save. The animation only reveals
        // an already-settled result, so stopping the app midway cannot lose or claim that result twice.
        internal bool TrySpinFortune(FortuneWheelCatalog catalog, int index, bool adCredit, out string error)
        {
            error = null;
            if (!CanCommitImmediately || catalog == null || !catalog.IsConfigured || index < 0 ||
                index >= FortuneWheelCatalog.SectorCount)
            { error = "The fortune wheel is unavailable right now."; return false; }
            if (adCredit ? Data.fortuneAdSpins <= 0 : Data.money < catalog.SpinPrice)
            { error = adCredit ? "Watch a rewarded ad to spin." : "Not enough gold."; return false; }
            var reward = catalog.Rewards[index];
            int cost = adCredit ? 0 : catalog.SpinPrice;
            long money = (long)Data.money - cost + (reward.kind == CurrencyKind.Gold ? reward.amount : 0);
            long water = (long)Data.water + (reward.kind == CurrencyKind.Water ? reward.amount : 0);
            long electricity = (long)Data.electricity + (reward.kind == CurrencyKind.Electricity ? reward.amount : 0);
            if (money > int.MaxValue || water > int.MaxValue || electricity > int.MaxValue)
            { error = "Your balance cannot hold this reward."; return false; }
            if (adCredit) Data.fortuneAdSpins--;
            Data.money = (int)money;
            Data.water = (int)water;
            Data.electricity = (int)electricity;
            if (reward.kind == CurrencyKind.Gold) Data.walterQuest.RecordEarnings(reward.amount);
            Commit();
            return true;
        }

        internal bool TryEarnDartAdReward()
        {
            if (!IsLoaded || (Data.dartDay == Data.currentDay && Data.dartBonus > int.MaxValue - PlayerProfileData.DartsPerDay - 2)) return false;
            Data.AddRewardedDarts();
            Commit();
            return true;
        }
        internal bool TryUseDart()
        {
            if (!IsLoaded || Data.DartsLeft <= 0) return false;
            Data.UseDart();
            Commit();
            return true;
        }

        // Pops a dart balloon once per day; its gold counts as earnings like AddMoney.
        internal bool TryClaimDartBalloon(int reward, int gold, int water = 0, int electricity = 0)
        {
            if (!IsLoaded || reward < 0 || reward >= PlayerProfileData.DartBalloonCount || gold < 0 ||
                water < 0 || electricity < 0 || Data.IsDartBalloonPopped(reward) || Money > int.MaxValue - gold ||
                Data.water > int.MaxValue - water || Data.electricity > int.MaxValue - electricity) return false;
            Data.PopDartBalloon(reward);
            Data.water += water;
            Data.electricity += electricity;
            if (gold > 0)
            {
                Data.money += gold;
                Data.walterQuest.RecordEarnings(gold);
            }
            Commit();
            return true;
        }

        // Pays for one basement generator repair. Requires the full cost (no debt) and an immediately saved profile.
        internal bool TryPayBasementRepair(int cost)
        {
            if (!CanCommitImmediately || cost < 0 || Data.money < cost) return false;
            Data.money -= cost;
            Data.basementRepairsPaid = checked(Data.basementRepairsPaid + 1);
            Commit();
            return true;
        }

        internal BasementPlantRecord FindBasementPlant(int instanceId) =>
            IsLoaded ? Data.basementPlants.Find(plant => plant.instanceId == instanceId) : null;

        // Buys one Lv.1 basement plant with clean water: water and the new plant are written in one save, now.
        internal bool TryBuyBasementPlant(string profileId, int waterCost, out int instanceId)
        {
            instanceId = 0;
            if (!CanCommitImmediately || string.IsNullOrWhiteSpace(profileId) || waterCost < 0 ||
                Data.water < waterCost) return false;
            instanceId = Data.nextBasementPlantId;
            Data.nextBasementPlantId = checked(instanceId + 1);
            Data.water -= waterCost;
            Data.basementPlants.Add(new BasementPlantRecord { instanceId = instanceId, profileId = profileId, level = 1 });
            CommitNow();
            return true;
        }

        // Raises a plant from exactly fromLevel by one, paying clean water, in one immediate save.
        internal bool TryUpgradeBasementPlant(int instanceId, int fromLevel, int waterCost, int maxLevel)
        {
            var plant = FindBasementPlant(instanceId);
            if (!CanCommitImmediately || plant == null || plant.level != fromLevel || fromLevel >= maxLevel ||
                waterCost < 0 || Data.water < waterCost) return false;
            Data.water -= waterCost;
            plant.level = fromLevel + 1;
            CommitNow();
            return true;
        }

        // Save migration: a plant placed on a shelf before plants were sold becomes an owned Lv.1 plant, free.
        // Idempotent per shelf, so a migration interrupted before the basement saved never grants twice.
        internal int GrantLegacyBasementPlant(string slotId, string profileId)
        {
            if (!CanCommitImmediately || string.IsNullOrWhiteSpace(slotId) || string.IsNullOrWhiteSpace(profileId)) return 0;
            var existing = Data.basementPlants.Find(plant => plant.legacySlot == slotId);
            if (existing != null) return existing.instanceId;
            int id = Data.nextBasementPlantId;
            Data.nextBasementPlantId = checked(id + 1);
            Data.basementPlants.Add(new BasementPlantRecord
                { instanceId = id, profileId = profileId, level = 1, legacySlot = slotId });
            CommitNow();
            return id;
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
        internal int ReceptionTableLevel => FurnitureLevel(ClinicDemand.ReceptionSlot);
        internal int ReceptionistLevel => FurnitureLevel(ClinicDemand.ReceptionistSlot);
        internal int ElevatorLevel => FurnitureLevel(ClinicDemand.ElevatorSlot);
        internal float ReceptionRatingBonus => ClinicUpgrades.StarBonus(ReceptionTableLevel, ReceptionistLevel);
        internal float BusinessGoldMultiplier => ClinicUpgrades.GoldMultiplier(ReceptionTableLevel, ReceptionistLevel);
        internal float PatienceMultiplier => ClinicUpgrades.PatienceMultiplier(ElevatorLevel, ReceptionTableLevel, ReceptionistLevel);
        internal int GoldPerMinute => ClinicUpgrades.GoldPerMinute(ReceptionTableLevel, ReceptionistLevel);

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

        // Electricity used per day by the item in each furniture slot (the equipped one, else the slot default).
        internal int DailyElectricityUse
        {
            get
            {
                if (!IsLoaded) return 0;
                float total = 0f;
                var slots = new HashSet<string>();
                foreach (var item in ShopDefinitions)
                    if (item != null && item.IsConfigured && item.IsEquippable) slots.Add(item.FurnitureSlot);
                foreach (string slot in slots)
                {
                    string equippedId = EquippedFurnitureId(slot);
                    ClinicShopItem equipped = null, fallback = null;
                    foreach (var item in ShopDefinitions)
                    {
                        if (item == null || !item.IsConfigured || item.FurnitureSlot != slot) continue;
                        if (item.Id == equippedId) equipped = item;
                        else if (item.IsDefaultFurniture) fallback = item;
                    }
                    var active = equipped != null ? equipped : fallback;
                    if (active != null) total += active.ElectricityPerDay;
                }
                // Electricity is a whole-number currency: half points round up.
                return (int)Math.Round(total, MidpointRounding.AwayFromZero);
            }
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
            bool hadConsultations = true, int goldEarned = 0, int patientsExamined = 0)
        {
            if (!IsLoaded) return false;
            try
            {
                // Generator output is decided once per day by the basement and reused if this save is retried.
                var basement = BasementDefenseService.Instance;
                int produced = basement != null ? basement.ClaimDayProduction(CurrentDay) : 0;
                session.CompleteDay(incorrectPatients, goldDelta, dailyRating, hadConsultations,
                    DifficultySettings.SuccessStreak, goldEarned, patientsExamined, DailyElectricityUse, produced);
            }
            catch (Exception ex) { Debug.LogError($"Player profile save failed: {ex}"); return false; }
            // Future patient-result calculations must read history before this checkpoint.
            journal.Clear();
            workingDayOpen = false;
            ClearPendingSave(); // The checkpoint already holds every change.
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
            workingDayOpen = false;
            ClearPendingSave();
            workingDay = session.WorkingProfile.Copy();
            try { ProfileReset?.Invoke(); }
            catch (Exception error) { Debug.LogException(error); }
            NotifyChanged();
            return true;
        }

        private void UpdateObjectives(PlayerProfileData profile) =>
            MainObjectives.Advance(profile, FurnitureLevel(profile, ClinicDemand.ReceptionistSlot), clinicRankings);

        private void Commit()
        {
            UpdateObjectives(Data);
            QueueSave(SaveDelay);
            NotifyChanged();
        }

        // Basement transactions: written synchronously so the basement file (saved right after) never runs ahead.
        private void CommitNow()
        {
            UpdateObjectives(Data);
            ClearPendingSave();
            try { store.AutosaveNow(Data); }
            catch (Exception ex)
            {
                Debug.LogError($"Player profile save failed: {ex}");
                QueueSave(RetryDelay);
            }
            NotifyChanged();
        }

        // Called when the clinic opens: saves the state players return to if they quit mid-day.
        internal void BeginWorkingDay()
        {
            if (session != null && !workingDayOpen) Autosave();
            workingDayOpen = true;
        }

        internal void AddPlaySeconds(double seconds)
        {
            if (session == null || !(seconds > 0d)) return;
            Data.playSeconds += seconds;
            playTimeUnsaved = true;
        }

        private void QueueSave(float delay)
        {
            if (saveQueued) return;
            saveQueued = true;
            saveDueTime = Time.unscaledTime + delay;
        }

        private void Update()
        {
            if (store != null && store.ConsumeWriteFailure()) QueueSave(RetryDelay);
            if (saveQueued && !workingDayOpen && Time.unscaledTime >= saveDueTime) Autosave();
        }

        // Background write; the profile snapshot is serialized immediately.
        private void Autosave()
        {
            if (session == null) return;
            ClearPendingSave();
            try { store.Autosave(Data); }
            catch (Exception ex) { Debug.LogError($"Player profile save failed: {ex}"); }
        }

        private void ClearPendingSave()
        {
            saveQueued = false;
            playTimeUnsaved = false;
        }

        // Pause/quit: write synchronously, at most once (Pause and Focus both fire when backgrounding).
        private void FlushSaves()
        {
            if (store == null) return;
            try
            {
                if (session != null && !workingDayOpen && (saveQueued || playTimeUnsaved))
                {
                    ClearPendingSave();
                    store.AutosaveNow(Data);
                }
                else store.Flush();
            }
            catch (Exception ex) { Debug.LogError($"Player profile save failed: {ex}"); }
        }

        private void OnApplicationPause(bool paused) { if (paused) FlushSaves(); }
        private void OnApplicationFocus(bool focused) { if (!focused) FlushSaves(); }
        private void OnApplicationQuit() => FlushSaves();
        private void NotifyChanged()
        {
            if (Changed == null) return;
            foreach (Action listener in Changed.GetInvocationList())
            {
                try { listener(); }
                catch (Exception error) { Debug.LogException(error); }
            }
        }
        private void OnDestroy()
        {
            if (Instance != this) return;
            FlushSaves();
            Instance = null;
        }
    }
}
