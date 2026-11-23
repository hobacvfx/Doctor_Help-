using System;
using System.Collections.Generic;

namespace DoctorHelp
{
    // Saved as numbers: never renumber. Ready/Eating/Cooldown: swallowing plants. Attacking: a strike plant's
    // Attack→fire→cooldown cycle (stateSeconds since the strike began). Reloading: a magazine regrowing.
    public enum BasementPlantState { Ready = 0, Eating = 1, Cooldown = 2, Attacking = 3, Reloading = 4 }

    // Intact → Breaking (BreakWire; the generator fails at the wire_break time) → Broken (Gnaw loop) → Repair → Intact.
    public enum BasementWireState { Intact, Breaking, Broken }

    [Serializable]
    public sealed class BasementRatState
    {
        public int id;
        // Seconds of running along the pipe; travelled / travelSeconds is the fraction of the path length.
        public float travelled;
        // Slot whose plant is eating this rat; empty while the rat runs. A caught rat never reaches the generator.
        public string caughtBy = "";
        // The plant's Eat time when this rat was caught (0 for the rat that started the Eat).
        public float caughtAt;
        // Health (BasementDefenseSettings.ratMaxHealth when spawned). −1: not set yet (rats from older saves).
        public float health = -1f;
        // Slow: speed is multiplied by (1 − slowFraction) while slowSeconds > 0. Hits refresh it, never stack it.
        public float slowSeconds;
        public float slowFraction;

        internal bool IsCaught => !string.IsNullOrEmpty(caughtBy);
        internal float SpeedMultiplier => slowSeconds > 0f ? 1f - slowFraction : 1f;
    }

    // A needle or petal in flight, homing on one rat. Removed when it hits (damage applied exactly once, by ID)
    // or when its rat is no longer a valid target (eaten, dead, reached the generator).
    [Serializable]
    public sealed class BasementProjectile
    {
        public int id;
        public BasementPlantAttack kind;
        public int ratId;
        public int sourceInstanceId;
        public float x, y;                 // World XY.
        public float damage;
        public float speed;                // World units per second.
        public float slowFraction, slowSeconds;
        // Seconds in flight (presentation only).
        public float age;
    }

    // An electric stream: its damage was applied once when it was created; this only keeps it visible.
    [Serializable]
    public sealed class BasementArc
    {
        public int id;
        public int ratId;
        public string slotId = "";
        public int sourceInstanceId;
        public float fromX, fromY, toX, toY; // World XY at the hit.
        public float age;
    }

    // Timers of one owned plant, wherever it is (on a shelf or in storage), so swapping never resets them.
    [Serializable]
    public sealed class BasementPlantRuntime
    {
        public int instanceId;
        public BasementPlantState state;
        // Seconds spent in the current state: Eat progress, or cooldown elapsed.
        public float stateSeconds;
        // Rats caught by the current Eat, removed together at the bite.
        public List<int> targetRatIds = new List<int>();
        // Ranged plants. Petals left in the magazine (−1: full, not set yet).
        public int ammo = -1;
        // Magazine: seconds since the last petal (capped at the interval while waiting for a target).
        public float shotTimer;
        // Strike: whether this strike has fired yet, and the rat an electric strike locked on at its start.
        public bool fired;
        public int lockedRatId;
    }

    // Which owned plant stands on a shelf. A plant is on at most one shelf; owned plants on no shelf are in storage.
    [Serializable]
    public sealed class BasementSlotEquip
    {
        public string slotId;
        public int instanceId;
    }

    // A shelf plant from a schema 1 save, waiting to become an owned plant in the profile (see BasementDefenseService).
    [Serializable]
    public sealed class BasementLegacyPlant
    {
        public string slotId;
        public string plantId;
        public BasementPlantState state;
        public float stateSeconds;
        public int targetRatId;
    }

    // Schema 1 shelf record (plant kind only, no instance). Read for migration, never written with content.
    [Serializable]
    public sealed class BasementSlotState
    {
        public string slotId;
        public string plantId;
        public BasementPlantState state;
        public float stateSeconds;
        public int targetRatId;
    }

    // Basement progress, saved in its own file next to the player profile (basement-defense.json).
    // It holds no gold, water, medicine or day data, so writing it during a working day never leaks profile changes.
    // Plant ownership and levels live in the profile; this file only says where each plant stands and its timers.
    [Serializable]
    public sealed class BasementDefenseData
    {
        internal const int CurrentSchemaVersion = 3;

        public int schemaVersion = CurrentSchemaVersion;
        public double simulatedSeconds;
        public float spawnTimer;
        public int nextRatId = 1;
        public List<BasementRatState> rats = new List<BasementRatState>();
        public List<BasementSlotEquip> equips = new List<BasementSlotEquip>();
        public List<BasementPlantRuntime> plants = new List<BasementPlantRuntime>();
        public List<BasementLegacyPlant> pendingLegacy = new List<BasementLegacyPlant>();
        public List<BasementProjectile> projectiles = new List<BasementProjectile>();
        public List<BasementArc> arcs = new List<BasementArc>();
        public int nextShotId = 1;
        // Schema 1 only; emptied by Migrate.
        public List<BasementSlotState> slots = new List<BasementSlotState>();
        public BasementWireState wire = BasementWireState.Intact;
        public float wireSeconds;
        // Repairs reflected here; compared with the profile's paid count after loading (see BasementDefenseService).
        public int repairsApplied;
        // End-of-day electricity decided for a day, reused if that checkpoint is retried.
        public int productionDay;
        public int productionAmount;

        internal BasementSlotEquip Equip(string slotId) => equips.Find(equip => equip.slotId == slotId);
        internal BasementSlotEquip EquipOf(int instanceId) => equips.Find(equip => equip.instanceId == instanceId);
        internal BasementPlantRuntime Runtime(int instanceId) => plants.Find(plant => plant.instanceId == instanceId);
        internal BasementLegacyPlant Legacy(string slotId) => pendingLegacy.Find(plant => plant.slotId == slotId);

        internal BasementPlantRuntime RuntimeOrNew(int instanceId)
        {
            var runtime = Runtime(instanceId);
            if (runtime != null) return runtime;
            runtime = new BasementPlantRuntime { instanceId = instanceId };
            plants.Add(runtime);
            return runtime;
        }

        // Schema 1 → 2: shelf plants wait in pendingLegacy until the profile owns them as Lv.1 instances.
        // Schema 2 → 3: rats get health (set from the settings by the simulation), plants get ammo/strike timers.
        internal void Migrate()
        {
            if (schemaVersion >= CurrentSchemaVersion) return;
            if (schemaVersion < 2 && slots != null)
                foreach (var slot in slots)
                    if (slot != null && !string.IsNullOrEmpty(slot.slotId) && !string.IsNullOrEmpty(slot.plantId))
                        pendingLegacy.Add(new BasementLegacyPlant
                        {
                            slotId = slot.slotId, plantId = slot.plantId, state = slot.state,
                            stateSeconds = slot.stateSeconds, targetRatId = slot.targetRatId
                        });
            slots = new List<BasementSlotState>();
            schemaVersion = CurrentSchemaVersion;
        }

        // Repairs what a damaged or hand-edited file may contain instead of refusing the whole save.
        internal void Sanitize()
        {
            if (rats == null) rats = new List<BasementRatState>();
            if (equips == null) equips = new List<BasementSlotEquip>();
            if (plants == null) plants = new List<BasementPlantRuntime>();
            if (pendingLegacy == null) pendingLegacy = new List<BasementLegacyPlant>();
            if (slots == null) slots = new List<BasementSlotState>();
            if (projectiles == null) projectiles = new List<BasementProjectile>();
            if (arcs == null) arcs = new List<BasementArc>();
            if (!(simulatedSeconds >= 0d)) simulatedSeconds = 0d;
            if (!(spawnTimer >= 0f)) spawnTimer = 0f;
            if (!(wireSeconds >= 0f)) wireSeconds = 0f;
            if (!Enum.IsDefined(typeof(BasementWireState), wire)) { wire = BasementWireState.Intact; wireSeconds = 0f; }
            if (repairsApplied < 0) repairsApplied = 0;
            if (productionAmount < 0) productionAmount = 0;

            var ids = new HashSet<int>();
            rats.RemoveAll(rat => rat == null || rat.id <= 0 || !ids.Add(rat.id) || !(rat.travelled >= 0f));
            foreach (var rat in rats)
            {
                if (rat.caughtBy == null) rat.caughtBy = "";
                if (!(rat.caughtAt >= 0f)) rat.caughtAt = 0f;
                if (float.IsNaN(rat.health) || float.IsInfinity(rat.health)) rat.health = -1f;
                if (!(rat.slowSeconds >= 0f) || float.IsInfinity(rat.slowSeconds)) rat.slowSeconds = 0f;
                if (!(rat.slowFraction >= 0f && rat.slowFraction < 1f)) rat.slowFraction = 0f;
                if (rat.id >= nextRatId) nextRatId = rat.id + 1;
            }
            if (nextRatId < 1) nextRatId = 1;

            // One plant per shelf and one shelf per plant.
            var slotIds = new HashSet<string>();
            var placed = new HashSet<int>();
            equips.RemoveAll(equip => equip == null || string.IsNullOrEmpty(equip.slotId) || equip.instanceId < 1 ||
                !slotIds.Add(equip.slotId) || !placed.Add(equip.instanceId));
            pendingLegacy.RemoveAll(legacy => legacy == null || string.IsNullOrEmpty(legacy.slotId) ||
                string.IsNullOrEmpty(legacy.plantId) || !slotIds.Add(legacy.slotId));
            var plantIds = new HashSet<int>();
            plants.RemoveAll(plant => plant == null || plant.instanceId < 1 || !plantIds.Add(plant.instanceId));
            foreach (var plant in plants)
            {
                if (plant.targetRatIds == null) plant.targetRatIds = new List<int>();
                if (!(plant.shotTimer >= 0f) || float.IsInfinity(plant.shotTimer)) plant.shotTimer = 0f;
                if (plant.ammo < -1) plant.ammo = -1;
                if (!Enum.IsDefined(typeof(BasementPlantState), plant.state) || !(plant.stateSeconds >= 0f))
                {
                    plant.state = BasementPlantState.Ready;
                    plant.stateSeconds = 0f;
                }
                // A target must still be a rat caught by the shelf this plant stands on.
                string shelf = EquipOf(plant.instanceId)?.slotId;
                plant.targetRatIds.RemoveAll(id => plant.state != BasementPlantState.Eating || shelf == null ||
                    rats.Find(rat => rat.id == id)?.caughtBy != shelf);
            }
            // Shots: finite numbers, unique IDs, a rat that still exists.
            var shotIds = new HashSet<int>();
            bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
            projectiles.RemoveAll(shot => shot == null || shot.id <= 0 || !shotIds.Add(shot.id) ||
                !Finite(shot.x) || !Finite(shot.y) || !(shot.damage >= 0f) || !Finite(shot.damage) || !(shot.speed > 0f) ||
                !Finite(shot.speed) || !(shot.slowFraction >= 0f && shot.slowFraction < 1f) || !(shot.slowSeconds >= 0f) ||
                !Finite(shot.slowSeconds) || rats.Find(rat => rat.id == shot.ratId) == null);
            arcs.RemoveAll(arc => arc == null || arc.id <= 0 || !shotIds.Add(arc.id) || !Finite(arc.fromX) ||
                !Finite(arc.fromY) || !Finite(arc.toX) || !Finite(arc.toY) || !(arc.age >= 0f));
            foreach (var arc in arcs) if (arc.slotId == null) arc.slotId = "";
            foreach (int id in shotIds) if (id >= nextShotId) nextShotId = id + 1;
            if (nextShotId < 1) nextShotId = 1;

            foreach (var legacy in pendingLegacy)
            {
                if (!Enum.IsDefined(typeof(BasementPlantState), legacy.state) || !(legacy.stateSeconds >= 0f))
                {
                    legacy.state = BasementPlantState.Ready;
                    legacy.stateSeconds = 0f;
                }
                if (legacy.state != BasementPlantState.Eating ||
                    rats.Find(rat => rat.id == legacy.targetRatId)?.caughtBy != legacy.slotId) legacy.targetRatId = 0;
            }
            foreach (var rat in rats)
            {
                if (!rat.IsCaught) continue;
                var equip = Equip(rat.caughtBy);
                bool held = equip != null && Runtime(equip.instanceId)?.targetRatIds.Contains(rat.id) == true;
                held |= Legacy(rat.caughtBy)?.targetRatId == rat.id;
                if (!held) { rat.caughtBy = ""; rat.caughtAt = 0f; } // Its plant no longer holds it: the rat keeps running.
            }
        }
    }
}
