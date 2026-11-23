using System;
using UnityEngine;

namespace DoctorHelp
{
    // How a plant hurts rats. Values are saved/serialized: never renumber them.
    //  Swallow       — eats a whole rat in one bite (infinite damage, shown as ∞); melee mouth zone (RatEater).
    //  Damage        — generic finite-damage plant with no gameplay yet; never sold.
    //  NeedleVolley  — one strike fires a needle at each of up to Targets different rats (Thorn Cactus).
    //  ElectricArc   — one strike hits the nearest rat with an instant electric stream (Volt Mushroom).
    //  PetalMagazine — fires one petal per Attack Cooldown until the magazine is empty, then reloads (Frost Sunflower).
    public enum BasementPlantAttack { Swallow = 0, Damage = 1, NeedleVolley = 2, ElectricArc = 3, PetalMagazine = 4 }

    // Melee: the shelf's mouth zone. Normal: world XY distance up to Range metres. Far: anywhere on the pipe.
    public enum BasementPlantRange { Melee = 0, Normal = 1, Far = 2 }

    // Stats and attack rules of one plant instance at its level. Damage is infinite for swallowing plants.
    public readonly struct BasementPlantStats
    {
        public readonly string PlantId;
        public readonly BasementPlantAttack Attack;
        public readonly float Damage;
        // Swallow: cooldown after Eat. Volley/Arc: time between strikes. Magazine: time between petals.
        public readonly float CooldownSeconds;
        public readonly int Targets;
        public readonly BasementPlantRange Range;
        public readonly float RangeMetres;
        // Ranged attack timing (authored Spine clips): Attack length and the fire moment, at speed 1.
        public readonly float AttackClipSeconds, FireSeconds;
        public readonly float ProjectileSpeed;      // Metres per second.
        public readonly int Magazine;
        public readonly float ReloadSeconds;
        public readonly float SlowFraction, SlowSeconds;

        public bool Swallows => Attack == BasementPlantAttack.Swallow;

        public BasementPlantStats(bool swallows, float damage, float cooldownSeconds, int targets)
            : this(null, swallows ? BasementPlantAttack.Swallow : BasementPlantAttack.Damage, damage, cooldownSeconds,
                targets, BasementPlantRange.Melee, 0f, 0f, 0f, 0f, 0, 0f, 0f, 0f) { }

        public BasementPlantStats(string plantId, BasementPlantAttack attack, float damage, float cooldownSeconds,
            int targets, BasementPlantRange range, float rangeMetres, float attackClipSeconds, float fireSeconds,
            float projectileSpeed, int magazine, float reloadSeconds, float slowFraction, float slowSeconds)
        {
            PlantId = plantId;
            Attack = attack;
            Damage = damage;
            CooldownSeconds = cooldownSeconds;
            Targets = targets;
            Range = range;
            RangeMetres = rangeMetres;
            AttackClipSeconds = attackClipSeconds;
            FireSeconds = fireSeconds;
            ProjectileSpeed = projectileSpeed;
            Magazine = magazine;
            ReloadSeconds = reloadSeconds;
            SlowFraction = slowFraction;
            SlowSeconds = slowSeconds;
        }

        // Strike plants never animate slower than authored; when the cooldown is shorter than the Attack clip
        // (high levels), clip and fire moment speed up together so the hit stays on the authored frame.
        public float StrikeSpeed => AttackClipSeconds > 0f && CooldownSeconds > 0f
            ? Mathf.Max(1f, AttackClipSeconds / CooldownSeconds) : 1f;
        public float StrikeFireSeconds => FireSeconds / StrikeSpeed;
    }

    // One kind of basement plant (definition only). Owned plants are instances saved in the player profile
    // (instance ID, this profile's ID, level); see PlayerProfileData.basementPlants.
    [CreateAssetMenu(menuName = "Doctor Help/Basement Plant Profile", fileName = "BasementPlant")]
    public sealed class BasementPlantProfile : ScriptableObject
    {
        // Cooldown never drops below this fraction of its base value, however high the level.
        public const float MinCooldownFraction = .2f;

        [Tooltip("Stable save ID. Never change it once players own this plant.")]
        [SerializeField] private string plantId = "RatEater_1";
        [SerializeField] private string displayName = "RatEater";
        [Tooltip("UI picture (preserve aspect). Shop card and Protection popup.")]
        [SerializeField] private Sprite portrait;
        [Tooltip("Shelf visual. Swallow: RatEater component (Idle/Eat/Chew). Ranged: DefensePlantAnimation (Idle/Attack[/Reload]).")]
        [SerializeField] private GameObject prefab;
        [SerializeField] private BasementPlantAttack attack = BasementPlantAttack.Swallow;

        [Header("Range (not raised by levels)")]
        [SerializeField] private BasementPlantRange range = BasementPlantRange.Melee;
        [Tooltip("Normal range only: world XY distance in metres (1 m = 1 unit of the view's Range Space).")]
        [SerializeField, Min(.1f)] private float rangeMetres = 5f;

        [Header("Price (clean water)")]
        [Tooltip("Clean water paid to buy one plant (starts at Lv.1). No debt.")]
        [SerializeField, Min(0)] private int buyCost = 100;

        [Header("Stats at Lv.1 and fixed change per level")]
        [Tooltip("Per needle / stream / petal. Swallowing plants always eat the whole rat (∞).")]
        [SerializeField, Min(0f)] private float baseDamage;
        [SerializeField, Min(0f)] private float damageIncreasePerLevel;
        [Tooltip("Swallow: after Eat. Volley/Arc: between strikes. Magazine: between petals.")]
        [SerializeField, Min(.01f)] private float baseCooldownSeconds = 30f;
        [Tooltip("Cooldown removed per level. Floored at 20% of the base cooldown.")]
        [SerializeField, Min(0f)] private float cooldownReductionPerLevel = 3f;
        [Tooltip("Most rats hit by one attack (different rats for a needle volley).")]
        [SerializeField, Min(1)] private int baseTargets = 1;
        [SerializeField, Min(0)] private int targetsIncreasePerLevel;

        [Header("Ranged attack timing (Spine clips)")]
        [Tooltip("Length of the Attack clip (volley/arc), or one petal step for a magazine.")]
        [SerializeField, Min(0f)] private float attackClipSeconds = .8f;
        [Tooltip("Spine 'fire' event time in the Attack clip: needles launch / the stream hits.")]
        [SerializeField, Min(0f)] private float fireSeconds = .32f;
        [Tooltip("Needles and petals, metres per second.")]
        [SerializeField, Min(.1f)] private float projectileSpeed = 12f;
        [Tooltip("Petals per magazine; must match the rig's petals (16).")]
        [SerializeField, Min(1)] private int magazineSize = 16;
        [Tooltip("Seconds to regrow the magazine (Reload clip is time-scaled to fit).")]
        [SerializeField, Min(.05f)] private float reloadSeconds = 1.4f;
        [Tooltip("Speed removed by a hit (0.3 = 30% slower). Does not stack; a new hit refreshes the time.")]
        [SerializeField, Range(0f, .9f)] private float slowFraction;
        [SerializeField, Min(0f)] private float slowSeconds;

        [Header("Upgrades (clean water)")]
        [Tooltip("Cost of Lv.1 → Lv.2.")]
        [SerializeField, Min(0)] private int initialUpgradeCost = 100;
        [Tooltip("Added to the upgrade cost for every level after the first.")]
        [SerializeField, Min(0)] private int upgradeCostIncreasePerLevel = 50;
        [SerializeField, Min(1)] private int maxLevel = 5;

        public string PlantId => plantId;
        public string DisplayName => displayName;
        public Sprite Portrait => portrait;
        public GameObject Prefab => prefab;
        public BasementPlantAttack Attack => attack;
        public BasementPlantRange Range => range;
        public int BuyCost => buyCost;
        public int MaxLevel => maxLevel;
        public bool IsRanged => attack == BasementPlantAttack.NeedleVolley || attack == BasementPlantAttack.ElectricArc ||
            attack == BasementPlantAttack.PetalMagazine;

        // Only kinds whose gameplay exists can be sold, placed or simulated.
        public bool IsPlayable => (attack == BasementPlantAttack.Swallow || IsRanged) && IsValid(out _);

        public int ClampLevel(int level) => Mathf.Clamp(level, 1, Mathf.Max(1, maxLevel));

        // Damage = base + perLevel × (L − 1); Targets likewise; Cooldown = max(20% base, base − reduction × (L − 1)).
        // Range, timing, magazine, reload and slow do not change with level.
        public BasementPlantStats StatsAt(int level)
        {
            int steps = ClampLevel(level) - 1;
            bool swallows = attack == BasementPlantAttack.Swallow;
            float damage = swallows ? float.PositiveInfinity : baseDamage + damageIncreasePerLevel * steps;
            float cooldown = Mathf.Max(baseCooldownSeconds * MinCooldownFraction,
                baseCooldownSeconds - cooldownReductionPerLevel * steps);
            int targets = Mathf.Max(1, baseTargets + targetsIncreasePerLevel * steps);
            return new BasementPlantStats(plantId, attack, damage, cooldown, targets, range, rangeMetres,
                attackClipSeconds, fireSeconds, projectileSpeed, magazineSize, reloadSeconds, slowFraction, slowSeconds);
        }

        public bool CanUpgradeFrom(int level) => level >= 1 && level < maxLevel;

        // Cost of Lv.L → Lv.L+1 = initial + increase × (L − 1).
        public int UpgradeCostFrom(int level) =>
            checked(initialUpgradeCost + upgradeCostIncreasePerLevel * (Mathf.Max(1, level) - 1));

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public bool IsValid(out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(plantId)) error = "Plant ID is empty.";
            else if (string.IsNullOrWhiteSpace(displayName)) error = "Display name is empty.";
            else if (!Enum.IsDefined(typeof(BasementPlantAttack), attack) || !Enum.IsDefined(typeof(BasementPlantRange), range))
                error = "Unknown attack or range type.";
            else if (buyCost < 0 || initialUpgradeCost < 0 || upgradeCostIncreasePerLevel < 0) error = "Prices cannot be negative.";
            else if (!(baseCooldownSeconds > 0f) || !Finite(baseCooldownSeconds)) error = "Base cooldown must be positive.";
            else if (!(cooldownReductionPerLevel >= 0f) || !Finite(cooldownReductionPerLevel)) error = "Cooldown reduction cannot be negative.";
            else if (baseTargets < 1 || targetsIncreasePerLevel < 0) error = "Targets must be at least 1 and cannot shrink.";
            else if (maxLevel < 1) error = "Max level must be at least 1.";
            else if (attack == BasementPlantAttack.Swallow)
            {
                if (prefab == null || prefab.GetComponent<RatEater>() == null) error = "Swallowing plants need a prefab with a RatEater component.";
                else if (range != BasementPlantRange.Melee) error = "Swallowing plants are Melee (mouth zone).";
            }
            else
            {
                if (!(baseDamage > 0f) || !Finite(baseDamage) || !(damageIncreasePerLevel >= 0f) || !Finite(damageIncreasePerLevel))
                    error = "Damage plants need positive, finite damage.";
                else if (IsRanged && (prefab == null || prefab.GetComponent<DefensePlantAnimation>() == null))
                    error = "Ranged plants need a prefab with a DefensePlantAnimation component.";
                else if (IsRanged && range == BasementPlantRange.Melee) error = "Ranged plants need Normal or Far range.";
                else if (range == BasementPlantRange.Normal && (!(rangeMetres > 0f) || !Finite(rangeMetres))) error = "Normal range needs positive metres.";
                else if ((attack == BasementPlantAttack.NeedleVolley || attack == BasementPlantAttack.ElectricArc) &&
                    (!(attackClipSeconds > 0f) || !(fireSeconds >= 0f) || fireSeconds >= attackClipSeconds))
                    error = "Strike plants need a fire time inside the Attack clip.";
                else if ((attack == BasementPlantAttack.NeedleVolley || attack == BasementPlantAttack.PetalMagazine) &&
                    (!(projectileSpeed > 0f) || !Finite(projectileSpeed)))
                    error = "Projectile speed must be positive.";
                else if (attack == BasementPlantAttack.PetalMagazine && (magazineSize < 1 || !(reloadSeconds > 0f) || !Finite(reloadSeconds)))
                    error = "A magazine needs at least one petal and a positive reload time.";
                else if (!(slowFraction >= 0f && slowFraction < 1f) || !(slowSeconds >= 0f) || !Finite(slowSeconds))
                    error = "Slow must be 0–90% for a non-negative time.";
            }
            if (error == null)
            {
                try { UpgradeCostFrom(maxLevel); }
                catch (OverflowException) { error = "Upgrade costs overflow at max level."; }
            }
            return error == null;
        }

        private void OnValidate()
        {
            if (!IsValid(out string error)) Debug.LogWarning($"Basement plant '{name}': {error}", this);
        }
    }
}
