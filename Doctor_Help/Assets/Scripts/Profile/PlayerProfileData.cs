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

    [Serializable]
    public sealed class ShopItemStock
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public sealed class FurnitureEquipment
    {
        public string slotId;
        public string itemId;
    }

    // A basement plant the player owns. Water spent and the plant are saved together in this profile, so a
    // purchase or upgrade can never be half applied. Where it stands (shelf or storage) is saved by the basement.
    [Serializable]
    public sealed class BasementPlantRecord
    {
        public int instanceId;
        public string profileId;
        public int level = 1;
        // Shelf of a plant placed before plants were bought (granted once, free, by the save migration).
        public string legacySlot = "";

        internal BasementPlantRecord Copy() => new BasementPlantRecord
            { instanceId = instanceId, profileId = profileId, level = level, legacySlot = legacySlot };
    }

    [Serializable]
    public sealed class ClinicDayRating
    {
        public int day;
        public float rating;
    }

    [Serializable]
    public sealed class RivalClinicRating
    {
        public string clinicId;
        public float rating;
    }

    // Plain JSON data. Money is signed because debt is part of the game design.
    [Serializable]
    public sealed class PlayerProfileData
    {
        public const float DefaultRating = 3.5f;
        public const float MaxRating = 5f;
        internal const int CurrentSchemaVersion = 2;
        internal const int RockThrowDeathThreshold = 4;
        private const int LegacyRockThrowDeathThreshold = 11;

        public int schemaVersion = CurrentSchemaVersion;
        public string clinicName = "";
        public int money = 500;
        public float rating = DefaultRating;
        public int currentDay = 1;
        [UnityEngine.SerializeField] internal MainObjective mainObjective;
        [UnityEngine.SerializeField] internal WalterQuest walterQuest;
        public int diffProgress = 1;
        public int diffStreakDays;
        public int diffStreakMistakeDay;
        public int patientsKilled;
        public int patientsSaved;
        public int ghostNumber;
        public int rockThrowDeaths;
        public int rockThrowCleanDays;
        public bool throwRock;
        public bool clean;
        // Active play time since the last day result; drives furniture gold per minute.
        public double playSeconds;
        // Dart minigame state belongs to dartDay; any other day starts with a full set of darts and balloons.
        public int dartDay;
        public int dartsUsed;
        public int dartBonus;
        public int dartBalloonsPopped; // Bit per balloon reward index.
        // Earned rewarded-ad spins survive app/scene interruptions until consumed.
        public int fortuneAdSpins;
        // Clean water and electricity behave like gold. Saves made before they existed start with the defaults.
        public int water = DefaultWater;
        public int electricity = DefaultElectricity;
        // Hidden player dirtiness, 0..MaxDirt in half points.
        public float dirt;
        // Basement generator repairs paid for with gold. Basement progress (separate file) records how many it
        // has applied; the two are reconciled on load so a repair is never free or paid without effect.
        public int basementRepairsPaid;
        // Owned basement plants and the next free instance ID (IDs are never reused).
        public List<BasementPlantRecord> basementPlants = new();
        public int nextBasementPlantId = 1;
        internal const int DefaultWater = 100;
        internal const int DefaultElectricity = 100;
        public const float MaxDirt = 10f;
        internal const float DirtPerPatient = .5f;
        internal bool IsStreetRockThrowingActive => rockThrowDeaths >= RockThrowDeathThreshold;
        internal const int DartsPerDay = 3;
        internal const int DartBalloonCount = 4;
        internal int DartsLeft => dartDay == currentDay ? DartsPerDay + dartBonus - dartsUsed : DartsPerDay;
        internal bool IsDartBalloonPopped(int reward) =>
            dartDay == currentDay && (dartBalloonsPopped & (1 << reward)) != 0;

        internal void AddRewardedDarts() { StartDartDay(); dartBonus += 2; }
        internal void UseDart()
        {
            StartDartDay();
            dartsUsed++;
        }

        internal void PopDartBalloon(int reward)
        {
            StartDartDay();
            dartBalloonsPopped |= 1 << reward;
        }

        private void StartDartDay()
        {
            if (dartDay == currentDay) return;
            dartDay = currentDay;
            dartsUsed = 0;
            dartBonus = 0;
            dartBalloonsPopped = 0;
        }
        public List<ClinicDayRating> dayRatings = new List<ClinicDayRating>();
        public List<RivalClinicRating> rivalRatings = new List<RivalClinicRating>();
        public List<ClinicDirtDay> clinicDirtDays = new();

        internal static bool IsRatingValid(float value) => value >= 0f && value <= MaxRating;
        // Retained for existing checkpoints. Consultations no longer use medicine stock.
        public List<MedicineStock> medicines = new List<MedicineStock>();
        public List<ShopItemStock> shopItems = new();
        public List<FurnitureEquipment> equippedFurniture = new();

        public PlayerProfileData Copy()
        {
            var copy = new PlayerProfileData
            {
                schemaVersion = schemaVersion, money = money, rating = rating, currentDay = currentDay,
                clinicName = clinicName,
                mainObjective = mainObjective,
                walterQuest = walterQuest,
                diffProgress = diffProgress, diffStreakDays = diffStreakDays, diffStreakMistakeDay = diffStreakMistakeDay,
                patientsKilled = patientsKilled, patientsSaved = patientsSaved, ghostNumber = ghostNumber,
                rockThrowDeaths = rockThrowDeaths, rockThrowCleanDays = rockThrowCleanDays,
                throwRock = throwRock, clean = clean, playSeconds = playSeconds,
                dartDay = dartDay, dartsUsed = dartsUsed, dartBonus = dartBonus, dartBalloonsPopped = dartBalloonsPopped,
                fortuneAdSpins = fortuneAdSpins,
                water = water, electricity = electricity, dirt = dirt, basementRepairsPaid = basementRepairsPaid,
                nextBasementPlantId = nextBasementPlantId
            };
            foreach (var plant in basementPlants)
                copy.basementPlants.Add(plant.Copy());
            foreach (var stock in medicines)
                copy.medicines.Add(new MedicineStock { medicineId = stock.medicineId, quantity = stock.quantity });
            foreach (var stock in shopItems)
                copy.shopItems.Add(new ShopItemStock { itemId = stock.itemId, quantity = stock.quantity });
            foreach (var entry in equippedFurniture)
                copy.equippedFurniture.Add(new FurnitureEquipment { slotId = entry.slotId, itemId = entry.itemId });
            foreach (var entry in dayRatings)
                copy.dayRatings.Add(new ClinicDayRating { day = entry.day, rating = entry.rating });
            foreach (var entry in rivalRatings)
                copy.rivalRatings.Add(new RivalClinicRating { clinicId = entry.clinicId, rating = entry.rating });
            foreach (var entry in clinicDirtDays)
                copy.clinicDirtDays.Add(entry.Copy());
            return copy;
        }

        public bool IsValid()
        {
            if (schemaVersion != CurrentSchemaVersion || currentDay < 1 || patientsKilled < 0 ||
                mainObjective < MainObjective.ClearFirstDay || mainObjective > MainObjective.Completed ||
                !walterQuest.IsValid(currentDay) ||
                diffProgress < 1 || diffProgress > DifficultyProfile.MaxDifficulty ||
                diffStreakDays < 0 || diffStreakDays >= currentDay || diffStreakMistakeDay < 0 ||
                (diffStreakMistakeDay != 0 && (diffStreakMistakeDay < currentDay - diffStreakDays ||
                    diffStreakMistakeDay >= currentDay)) ||
                patientsSaved < 0 || ghostNumber < 0 || medicines == null ||
                rockThrowDeaths < 0 || rockThrowDeaths > LegacyRockThrowDeathThreshold ||
                rockThrowCleanDays < 0 || rockThrowCleanDays > (IsStreetRockThrowingActive ? 1 : 0) ||
                dayRatings == null || rivalRatings == null || clinicDirtDays == null ||
                shopItems == null || equippedFurniture == null || !IsRatingValid(rating) ||
                !(playSeconds >= 0d) || double.IsInfinity(playSeconds) ||
                fortuneAdSpins < 0 || dartDay < 0 || dartDay > currentDay || dartsUsed < 0 || dartBonus < 0 || dartBonus > int.MaxValue - DartsPerDay || dartsUsed > (long)DartsPerDay + dartBonus ||
                dartBalloonsPopped < 0 || dartBalloonsPopped >= 1 << DartBalloonCount ||
                water < 0 || electricity < 0 || !(dirt >= 0f && dirt <= MaxDirt) || basementRepairsPaid < 0 ||
                basementPlants == null || nextBasementPlantId < 1) return false;
            var plantIds = new HashSet<int>();
            foreach (var plant in basementPlants)
                if (plant == null || plant.instanceId < 1 || plant.instanceId >= nextBasementPlantId ||
                    !plantIds.Add(plant.instanceId) || string.IsNullOrWhiteSpace(plant.profileId) || plant.level < 1) return false;
            int previousDay = 0;
            foreach (var entry in dayRatings)
            {
                if (entry == null || entry.day <= previousDay || entry.day >= currentDay ||
                    entry.rating < 1f || !IsRatingValid(entry.rating)) return false;
                previousDay = entry.day;
            }
            previousDay = 0;
            foreach (var entry in clinicDirtDays)
            {
                if (entry == null || entry.day <= previousDay || entry.day > currentDay ||
                    entry.IsExpiredOn(currentDay) || !entry.IsValid()) return false;
                previousDay = entry.day;
            }
            var clinicIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in rivalRatings)
                if (entry == null || string.IsNullOrWhiteSpace(entry.clinicId) ||
                    !clinicIds.Add(entry.clinicId) || !IsRatingValid(entry.rating)) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var stock in medicines)
                if (stock == null || string.IsNullOrWhiteSpace(stock.medicineId) ||
                    stock.quantity < 0 || !ids.Add(stock.medicineId)) return false;
            ids.Clear();
            foreach (var stock in shopItems)
                if (stock == null || string.IsNullOrWhiteSpace(stock.itemId) ||
                    stock.quantity < 1 || !ids.Add(stock.itemId)) return false;
            var slots = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in equippedFurniture)
                if (entry == null || string.IsNullOrWhiteSpace(entry.slotId) ||
                    !ids.Contains(entry.itemId) || !slots.Add(entry.slotId)) return false;
            return true;
        }
    }
}
