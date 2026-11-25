using UnityEngine;

namespace DoctorHelp
{
    internal static class GeneratorUpgrades
    {
        internal const string Slot = "generator";
        // Temporary balance. Purchase prices are authored separately on each shop item.
        internal static int EnergyPerDay(int level) => 5 + 10 * Mathf.Clamp(level, 1, 5);
    }
}
