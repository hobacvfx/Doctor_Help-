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

        internal static int IndoorVisitors(float rating) =>
            Mathf.FloorToInt(Mathf.Lerp(1f, MaxIndoorVisitors, RatingProgress(rating)) + .5f);

        internal static float NextPatientDelay(int signLevel, float roll, float durationRoll)
        {
            float chance = (60 - 10 * signLevel) / 100f;
            return roll < chance ? Mathf.Lerp(5f, 7f, durationRoll) - 0.5f * signLevel : 0f;
        }
    }
}
