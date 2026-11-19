using UnityEngine;

namespace DoctorHelp
{
    internal static class ClinicDemand
    {
        internal const int MaxIndoorVisitors = 6;
        internal const int MaxFurnitureLevel = 5;
        internal const string SignSlot = "clinic-sign";
        internal const string ReceptionSlot = "reception-table";
        internal const string ReceptionistSlot = "receptionist";
        internal const string ElevatorSlot = "elevator";
        private static float RatingProgress(float rating) => Mathf.InverseLerp(1f, PlayerProfileData.MaxRating, rating);

        // Player_Home shows at most this many ghosts; each shown ghost costs every patient a quarter star.
        internal const int MaxHomeGhosts = 15;
        internal const float GhostRatingPenalty = .25f;
        internal static int VisibleHomeGhosts(int ghostNumber) => Mathf.Clamp(ghostNumber, 0, MaxHomeGhosts);
        internal static float HomeGhostRatingPenalty(int ghostNumber) => GhostRatingPenalty * VisibleHomeGhosts(ghostNumber);

        internal const int MinDailyClients = 1;
        internal const int MaxDailyClients = 15;
        // Extra daily clients granted by each clinic-sign level. Index = sign level 0..5.
        private static readonly int[] SignClientBonus = { 0, 0, 1, 2, 3, 4 };

        // Office patients one working day can admit: 1 star = 1, 5 stars = 15 (linear, rounded), plus the sign bonus.
        internal static int DailyClientLimit(float rating, int signLevel) =>
            Mathf.FloorToInt(Mathf.Lerp(MinDailyClients, MaxDailyClients, RatingProgress(rating)) + .5f) +
            SignClientBonus[Mathf.Clamp(signLevel, 0, MaxFurnitureLevel)];

        internal static int IndoorVisitors(float rating) =>
            Mathf.FloorToInt(Mathf.Lerp(1f, MaxIndoorVisitors, RatingProgress(rating)) + .5f);

        // Per clinic-sign level: X lowers the chance of a wait, Y shortens the wait. Index = sign level 0..5.
        private static readonly int[] SignWaitChanceStep = { 0, 0, 3, 4, 5, 7 };
        private static readonly float[] SignWaitReduction = { 0f, 0f, 1f, 2f, 3f, 4f };

        // Chance of a wait = (20 - 10X)% + (50 / rating)%; the wait is 0-2 s plus 8 / rating s minus Y s.
        internal static float NextPatientDelay(int signLevel, float rating, float roll, float durationRoll)
        {
            int level = Mathf.Clamp(signLevel, 0, MaxFurnitureLevel);
            // Ratings below one star only come from debug edits; clamp so the formula stays finite.
            float safeRating = Mathf.Max(rating, 1f);
            float chance = Mathf.Clamp01((20f - 10f * SignWaitChanceStep[level] + 50f / safeRating) / 100f);
            float wait = Mathf.Lerp(0f, 2f, durationRoll) + 8f / safeRating - SignWaitReduction[level];
            return roll < chance ? Mathf.Max(0f, wait) : 0f;
        }
    }
}
