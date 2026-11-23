using UnityEngine;

namespace DoctorHelp
{
    // Basement generator defense rules, in one asset: Assets/Resources/BasementDefenseSettings.asset.
    // Plant kinds, prices, cooldowns and levels: Assets/Resources/BasementPlantCatalog.asset and its profiles.
    // Scene layout (pipe, shelves, mouth zones) lives in Player_Home.
    [CreateAssetMenu(menuName = "Doctor Help/Basement Defense Settings", fileName = "BasementDefenseSettings")]
    public sealed class BasementDefenseSettings : ScriptableObject
    {
        internal const string ResourcePath = "BasementDefenseSettings";

        [Header("Rats")]
        [Tooltip("A rat appears at the right pipe opening every this many seconds; the first one this long after start.")]
        [SerializeField, Min(1f)] private float spawnIntervalSeconds = 25f;
        [Tooltip("Seconds an unslowed rat takes to run the whole pipe. Speed follows path length, not waypoint count.")]
        [SerializeField, Min(1f)] private float travelSeconds = 30f;
        [Tooltip("Health of every new rat. TEMPORARY test value, not approved balance. Rats from old saves get this too.")]
        [SerializeField, Min(.1f)] private float ratMaxHealth = 20f;

        [Header("Ranged plants")]
        [Tooltip("How long an electric stream stays visible after it hits (damage is applied once, at the hit).")]
        [SerializeField, Min(.05f)] private float arcVisibleSeconds = .3f;

        [Header("Swallowing plants (match the RatEater Spine clips; cooldown/targets are per plant profile)")]
        [Tooltip("Length of the Eat clip. Cooldown starts when it ends.")]
        [SerializeField, Min(.1f)] private float eatSeconds = 1.68f;
        [Tooltip("Moment in Eat when the jaws snap shut; the caught rat disappears then.")]
        [SerializeField, Min(0f)] private float biteSeconds = .58f;

        [Header("Generator wiring (matches generator_wiring Spine clips)")]
        [Tooltip("Time of the wire_break event in BreakWire: the generator stops here.")]
        [SerializeField, Min(0f)] private float wireBreakSeconds = 1.28f;
        [Tooltip("Length of BreakWire; Gnaw loops after it.")]
        [SerializeField, Min(.1f)] private float breakWireSeconds = 2.6f;

        [Header("Economy")]
        [Tooltip("Electricity added at the end-of-day checkpoint while the generator works; 0 if it is broken.")]
        [SerializeField, Min(0)] private int electricityPerDay = 50;
        [Tooltip("Gold paid for one repair. The player must have it; no debt.")]
        [SerializeField, Min(0)] private int repairCost = 50;

        [Header("Saving")]
        [Tooltip("State changes are batched into one write this many seconds after the first change.")]
        [SerializeField, Min(.1f)] private float autosaveDelaySeconds = 1f;

        internal float SpawnIntervalSeconds => spawnIntervalSeconds;
        internal float TravelSeconds => travelSeconds;
        internal float RatMaxHealth => ratMaxHealth;
        internal float ArcVisibleSeconds => arcVisibleSeconds;
        internal float EatSeconds => eatSeconds;
        internal float BiteSeconds => Mathf.Min(biteSeconds, eatSeconds);
        internal float WireBreakSeconds => Mathf.Min(wireBreakSeconds, breakWireSeconds);
        internal float BreakWireSeconds => breakWireSeconds;
        internal int ElectricityPerDay => electricityPerDay;
        internal int RepairCost => repairCost;
        internal float AutosaveDelaySeconds => autosaveDelaySeconds;

        internal static BasementDefenseSettings Load()
        {
            var settings = Resources.Load<BasementDefenseSettings>(ResourcePath);
            if (settings != null) return settings;
            Debug.LogWarning("Resources/BasementDefenseSettings is missing; using default basement rules.");
            return CreateInstance<BasementDefenseSettings>();
        }
    }
}
