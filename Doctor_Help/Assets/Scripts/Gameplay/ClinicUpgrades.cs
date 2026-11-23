namespace DoctorHelp
{
    // Effects of the clinic furniture levels, matching the shop descriptions. Each level grants only the
    // bonuses its own description lists. Arrays are indexed by furniture level 0..5.
    internal static class ClinicUpgrades
    {
        private static readonly float[] ReceptionTableStars = { 0f, .25f, .5f, .75f, 1f, 1.5f };
        private static readonly float[] ReceptionTableGold = { 0f, .05f, 0f, 0f, 0f, .15f };
        private static readonly float[] ReceptionTablePatience = { 0f, 0f, .1f, 0f, .2f, 0f };
        private static readonly int[] ReceptionTableGoldPerMinute = { 0, 0, 0, 15, 0, 0 };

        private static readonly float[] ReceptionistStars = { 0f, 0f, .5f, 0f, 1.5f, 0f };
        private static readonly float[] ReceptionistGold = { 0f, .2f, .4f, .6f, .8f, 1.2f };
        private static readonly float[] ReceptionistPatience = { 0f, .1f, 0f, 0f, 0f, .2f };
        private static readonly int[] ReceptionistGoldPerMinute = { 0, 0, 0, 20, 0, 60 };

        private static readonly float[] ElevatorPatience = { 0f, .1f, .2f, .3f, .4f, .5f };

        // Added to every non-police patient's review stars.
        internal static float StarBonus(int receptionTable, int receptionist) =>
            ReceptionTableStars[Level(receptionTable)] + ReceptionistStars[Level(receptionist)];

        // Multiplies the gold earned from each correctly treated patient.
        internal static float GoldMultiplier(int receptionTable, int receptionist) =>
            1f + ReceptionTableGold[Level(receptionTable)] + ReceptionistGold[Level(receptionist)];

        // Multiplies each Office patient's patience duration.
        internal static float PatienceMultiplier(int elevator, int receptionTable, int receptionist) =>
            1f + ElevatorPatience[Level(elevator)] + ReceptionTablePatience[Level(receptionTable)] +
            ReceptionistPatience[Level(receptionist)];

        // Passive income per real minute the clinic is open.
        internal static int GoldPerMinute(int receptionTable, int receptionist) =>
            ReceptionTableGoldPerMinute[Level(receptionTable)] + ReceptionistGoldPerMinute[Level(receptionist)];

        private static int Level(int level) => level < 0 ? 0 : level > ClinicDemand.MaxFurnitureLevel ? ClinicDemand.MaxFurnitureLevel : level;
    }
}
