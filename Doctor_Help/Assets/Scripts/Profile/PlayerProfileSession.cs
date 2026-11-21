using System;
using System.Collections.Generic;

namespace DoctorHelp
{
    // PlayerProfileService autosaves WorkingProfile outside the Office working day.
    // An opened working day stays memory-only until CompleteDay creates the next-day checkpoint.
    public sealed class PlayerProfileSession
    {
        private const int InitialRatingDays = 5;
        public PlayerProfileData WorkingProfile { get; private set; }
        private readonly Action<PlayerProfileData> persist;
        private readonly Action<PlayerProfileData> updateObjectives;

        public PlayerProfileSession(PlayerProfileData checkpoint, Action<PlayerProfileData> persist)
            : this(checkpoint, persist, null) { }

        internal PlayerProfileSession(PlayerProfileData checkpoint, Action<PlayerProfileData> persist,
            Action<PlayerProfileData> updateObjectives)
        {
            if (checkpoint == null || !checkpoint.IsValid())
                throw new ArgumentException("Invalid checkpoint.", nameof(checkpoint));
            this.persist = persist ?? throw new ArgumentNullException(nameof(persist));
            this.updateObjectives = updateObjectives;
            WorkingProfile = checkpoint.Copy();
            updateObjectives?.Invoke(WorkingProfile);
        }

        public void CompleteDay(int incorrectPatients, int goldDelta, float? dailyRating = null,
            bool hadConsultations = true, float requiredStreakDays = DifficultyProfile.DefaultSuccessStreak,
            int goldEarned = 0, int patientsExamined = 0, int electricityUsed = 0)
        {
            if (incorrectPatients < 0) throw new ArgumentOutOfRangeException(nameof(incorrectPatients));
            if (patientsExamined < 0) throw new ArgumentOutOfRangeException(nameof(patientsExamined));
            if (electricityUsed < 0) throw new ArgumentOutOfRangeException(nameof(electricityUsed));
            var nextDay = WorkingProfile.Copy();
            if (dailyRating is float value)
            {
                if (value < 1f || !PlayerProfileData.IsRatingValid(value))
                    throw new ArgumentOutOfRangeException(nameof(dailyRating));
                nextDay.dayRatings.Add(new ClinicDayRating { day = nextDay.currentDay, rating = value });
                // The ten starting ratings contribute to the average without creating calendar days.
                double total = InitialRatingDays * PlayerProfileData.DefaultRating;
                foreach (var entry in nextDay.dayRatings) total += entry.rating;
                nextDay.rating = (float)(total / (InitialRatingDays + (double)nextDay.dayRatings.Count));
            }
            nextDay.money = checked(nextDay.money + goldDelta);
            nextDay.walterQuest.RecordEarnings(goldEarned);
            nextDay.walterQuest.CloseDay(nextDay.currentDay);
            nextDay.currentDay = checked(nextDay.currentDay + 1);
            nextDay.playSeconds = 0d;
            // Every examined patient leaves the doctor half a point dirtier for the next day.
            nextDay.dirt = Math.Min(PlayerProfileData.MaxDirt,
                nextDay.dirt + patientsExamined * PlayerProfileData.DirtPerPatient);
            // The day's furniture electricity is paid when the next day starts; it never goes below zero.
            nextDay.electricity = Math.Max(0, nextDay.electricity - electricityUsed);
            DifficultyProfile.Advance(nextDay, incorrectPatients, hadConsultations, requiredStreakDays);
            nextDay.clinicDirtDays.RemoveAll(entry => entry.IsExpiredOn(nextDay.currentDay));
            nextDay.ghostNumber = checked(nextDay.ghostNumber + incorrectPatients);
            // Day results already use one Ghost for each failed consultation.
            nextDay.patientsKilled = checked(nextDay.patientsKilled + incorrectPatients);
            UpdateStreetRockThrowing(nextDay, incorrectPatients);
            updateObjectives?.Invoke(nextDay);
            persist(nextDay.Copy()); // Failure leaves the current day available for retry.
            WorkingProfile = nextDay;
        }

        private static void UpdateStreetRockThrowing(PlayerProfileData nextDay, int deathsToday)
        {
            if (!nextDay.IsStreetRockThrowingActive)
            {
                int remaining = PlayerProfileData.RockThrowDeathThreshold - nextDay.rockThrowDeaths;
                nextDay.rockThrowDeaths += Math.Min(deathsToday, remaining);
                return;
            }

            nextDay.rockThrowCleanDays = deathsToday == 0 ? nextDay.rockThrowCleanDays + 1 : 0;
            if (nextDay.rockThrowCleanDays < 2) return;
            nextDay.rockThrowCleanDays = 0;
            nextDay.rockThrowDeaths = 0;
        }

        public bool TryGive(PillProfile pills, IReadOnlyList<string> ids, Action record, out string error,
            bool allowEmpty = false)
        {
            error = null;
            if (ids == null || (ids.Count == 0 && !allowEmpty)) { error = "Select at least one medicine."; return false; }
            if (!pills.TryGetCost(ids, out long cost)) { error = "Selected medicine is unavailable."; return false; }
            // Medicines may be paid on credit: gold can go negative (debt), only int overflow is refused.
            if ((long)WorkingProfile.money - cost < int.MinValue) { error = "Debt limit reached."; return false; }
            try { record(); }
            catch (Exception ex) { error = "Cannot record treatment. Retry: " + ex.Message; return false; }
            WorkingProfile.money -= (int)cost;
            return true;
        }

        internal bool CanBuyUpgrade(ClinicShopItem item, IReadOnlyList<ClinicShopItem> definitions)
        {
            int highestOwned = -1;
            foreach (var candidate in definitions)
            {
                if (candidate == null || !candidate.IsConfigured || candidate.FurnitureSlot != item.FurnitureSlot)
                    continue;
                if (candidate.IsDefaultFurniture || WorkingProfile.shopItems.Exists(stock => stock.itemId == candidate.Id))
                    highestOwned = Math.Max(highestOwned, candidate.FurnitureLevel);
            }
            // Existing saves can already own a higher tier. Keep it and allow filling gaps.
            return !item.IsDefaultFurniture && item.FurnitureLevel <= highestOwned + 1;
        }

        internal bool TryBuy(ClinicShopItem item, IReadOnlyList<ClinicShopItem> definitions, out string error)
        {
            error = null;
            if (item == null || !item.IsConfigured) { error = "This item is unavailable."; return false; }
            var stock = WorkingProfile.shopItems.Find(entry => entry.itemId == item.Id);
            if (item.IsEquippable && stock != null) { error = "You already own this item."; return false; }
            if (item.IsEquippable && !CanBuyUpgrade(item, definitions))
            { error = "Buy the previous level first."; return false; }
            if (stock?.quantity == int.MaxValue) { error = "Your inventory is full for this item."; return false; }
            if (item.Price > 0 && WorkingProfile.money < item.Price) { error = "Not enough gold."; return false; }

            if (stock == null)
                WorkingProfile.shopItems.Add(new ShopItemStock { itemId = item.Id, quantity = 1 });
            else
                stock.quantity++;
            WorkingProfile.money -= item.Price;
            if (item.IsEquippable) SetEquipment(item);
            return true;
        }

        internal bool TryEquip(ClinicShopItem item)
        {
            if (item == null || !item.IsConfigured || !item.IsEquippable ||
                (!item.IsDefaultFurniture && !WorkingProfile.shopItems.Exists(entry => entry.itemId == item.Id)))
                return false;
            SetEquipment(item);
            return true;
        }

        private void SetEquipment(ClinicShopItem item)
        {
            var entry = WorkingProfile.equippedFurniture.Find(value => value.slotId == item.FurnitureSlot);
            if (item.IsDefaultFurniture)
            {
                if (entry != null) WorkingProfile.equippedFurniture.Remove(entry);
            }
            else if (entry == null)
                WorkingProfile.equippedFurniture.Add(new FurnitureEquipment { slotId = item.FurnitureSlot, itemId = item.Id });
            else
                entry.itemId = item.Id;
        }
    }
}
