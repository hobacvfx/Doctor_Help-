using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DoctorHelp
{
    public enum BasementRepairResult { Repaired, NotBroken, NotEnoughGold, Unavailable }
    // Unavailable: spending is closed (working day / profile not saved immediately). Invalid: stale request.
    public enum BasementPlantResult { Done, NotEnoughWater, Unavailable, MaxLevel, Invalid }

    // An owned, usable plant: its instance ID, kind and level.
    public readonly struct BasementOwnedPlant
    {
        public readonly int InstanceId;
        public readonly BasementPlantProfile Kind;
        public readonly int Level;

        public BasementOwnedPlant(int instanceId, BasementPlantProfile kind, int level)
        {
            InstanceId = instanceId;
            Kind = kind;
            Level = kind != null ? kind.ClampLevel(level) : level;
        }

        public BasementPlantStats Stats => Kind.StatsAt(Level);
    }

    // Persistent owner of the basement defense (on Game Services, no visuals). Starts when Player_Home first
    // registers its basement layout, then keeps simulating in every scene while the app is focused.
    // Saves to its own file so it can save during a working day without touching the player profile.
    public sealed class BasementDefenseService : MonoBehaviour, ISerializationCallbackReceiver
    {
        public static BasementDefenseService Instance { get; private set; }

        private BasementDefenseSettings settings;
        private BasementPlantCatalog catalog;
        private BasementDefenseStore store;
        private PlayerProfileService profile;
        private bool paused, unfocused, skipNextDelta;
        private bool saveQueued;
        private float saveDueTime;
        private const float RetryDelay = 5f;
        private const float MaxFrameDelta = 1f; // Scene loads may stall a frame; never jump further than this.
        // Survives a Unity code reload in Play Mode, which clears every non-serialized field.
        [SerializeField, HideInInspector] private BasementDefenseData reloadSnapshot;

        internal BasementDefenseSimulation Simulation { get; private set; }
        internal BasementDefenseSettings Settings => settings;
        internal BasementPlantCatalog Catalog => catalog;
        // Water may only be spent while the profile saves immediately (never inside a working day).
        internal bool CanSpend => ConnectProfile() && profile.CanCommitImmediately;
        internal bool IsRunning => Simulation != null && Simulation.HasLayout;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            Initialize(null);
        }

        private void OnEnable()
        {
            if (Instance == null) Instance = this;
            if (Instance == this && Simulation == null && reloadSnapshot != null) Initialize(reloadSnapshot);
        }

        public void OnBeforeSerialize() { if (Simulation != null) reloadSnapshot = Simulation.Data; }
        public void OnAfterDeserialize() { }

        private void Initialize(BasementDefenseData resumed)
        {
            settings = BasementDefenseSettings.Load();
            catalog = BasementPlantCatalog.Load();
            store = new BasementDefenseStore(PlayerProfileService.ProfileDirectory());

            // After a code reload the in-memory state is newer than the file; the load still primes the store.
            BasementDefenseData data = resumed;
            try
            {
                var loaded = store.Load();
                if (data == null) data = loaded;
            }
            catch (Exception error)
            {
                Debug.LogError("Basement progress could not be loaded; files kept untouched. " + error);
                if (data == null) data = new BasementDefenseData();
            }
            data.Sanitize();
            Simulation = new BasementDefenseSimulation(settings, data);
            Simulation.SetStatsSource(StatsOf);
            Simulation.Changed += QueueSave;
            ConnectProfile();
            if (resumed == null)
            {
                ReconcileRepairs();
                ReconcilePlants();
            }
        }

        // Stats of an owned plant at its saved level; null if the profile does not own it or its kind is gone.
        private BasementPlantStats? StatsOf(int instanceId)
        {
            var plant = Owned(instanceId);
            return plant.Kind != null ? plant.Stats : (BasementPlantStats?)null;
        }

        internal BasementOwnedPlant Owned(int instanceId)
        {
            if (profile == null || !profile.IsLoaded) return default;
            var record = profile.FindBasementPlant(instanceId);
            var kind = record != null ? catalog.Find(record.profileId) : null;
            return kind != null ? new BasementOwnedPlant(record.instanceId, kind, record.level) : default;
        }

        // The plant on a shelf, if any.
        internal BasementOwnedPlant ShelfPlant(string slotId)
        {
            var equip = Simulation?.Data.Equip(slotId);
            return equip != null ? Owned(equip.instanceId) : default;
        }

        // Owned usable plants on no shelf, in purchase order.
        internal void StoredPlants(List<BasementOwnedPlant> result)
        {
            result.Clear();
            if (!ConnectProfile() || !profile.IsLoaded || Simulation == null) return;
            foreach (var record in profile.BasementPlants)
            {
                if (Simulation.Data.EquipOf(record.instanceId) != null) continue;
                var plant = Owned(record.instanceId);
                if (plant.Kind != null) result.Add(plant);
            }
        }

        // The profile owns plants; this file only places them. Shelf plants from schema 1 saves become owned
        // Lv.1 plants (free, once per shelf), and places of plants the profile does not own are cleared.
        private void ReconcilePlants()
        {
            if (profile == null || !profile.IsLoaded || Simulation == null) return;
            bool changed = false;
            foreach (var legacy in Simulation.Data.pendingLegacy.ToArray())
            {
                int id = profile.GrantLegacyBasementPlant(legacy.slotId, legacy.plantId); // Saved before this file.
                if (id <= 0) continue; // Profile not writable now; retried later.
                Simulation.AdoptLegacy(legacy, id);
                changed = true;
            }
            changed |= Simulation.RemoveUnowned(id => profile.FindBasementPlant(id) != null);
            if (changed) SaveNow();
        }

        // The profile service may enable after this one following a code reload; connect when it exists.
        private bool ConnectProfile()
        {
            if (profile != null) return true;
            profile = PlayerProfileService.Instance;
            if (profile == null) return false;
            profile.ProfileReset += ResetForNewProfile;
            return true;
        }

        // Repair gold lives in the profile and the repaired wire here; the two files are written separately.
        // The profile's paid count is the truth: a paid repair missing here is applied, and a repair saved
        // here whose payment never reached the profile is undone, so no repair is ever free or lost.
        private void ReconcileRepairs()
        {
            if (profile == null || !profile.IsLoaded) return;
            var data = Simulation.Data;
            int paid = profile.BasementRepairsPaid;
            if (paid == data.repairsApplied) return;
            if (paid > data.repairsApplied) Simulation.Repair();
            else Simulation.ForceBroken();
            data.repairsApplied = paid;
            QueueSave();
        }

        // Called by the Player_Home basement every time it loads. The first call starts the simulation.
        internal void RegisterLayout(BasementLayout layout)
        {
            if (layout == null) return;
            bool starting = !Simulation.HasLayout;
            Simulation.SetLayout(layout);
            if (starting) skipNextDelta = true;
        }

        // The view measures where each ranged plant kind shoots from on each shelf (its Spine muzzle bone).
        internal void SetMuzzle(string slotId, string plantId, Vector2 world) =>
            Simulation?.Layout?.SetMuzzle(slotId, plantId, world);

        private void Update()
        {
            if (Simulation == null) return;
            ConnectProfile();
            if (store.ConsumeWriteFailure()) QueueSave(RetryDelay);
            if (Simulation.Data.pendingLegacy.Count > 0 && profile != null && profile.CanCommitImmediately) ReconcilePlants();
            if (IsRunning && !paused && !unfocused)
            {
                if (skipNextDelta) skipNextDelta = false;
                else Simulation.Step(Mathf.Min(Time.unscaledDeltaTime, MaxFrameDelta));
            }
            if (saveQueued && Time.unscaledTime >= saveDueTime)
            {
                saveQueued = false;
                store.Autosave(Simulation.Data);
            }
        }

        private void QueueSave() => QueueSave(settings.AutosaveDelaySeconds);

        private void QueueSave(float delay)
        {
            if (saveQueued) return;
            saveQueued = true;
            saveDueTime = Time.unscaledTime + delay;
        }

        // Writes now, with the current timers and rat progress.
        private void SaveNow()
        {
            saveQueued = false;
            store.SaveNow(Simulation.Data);
        }

        // Buys a Lv.1 plant with clean water and puts it on the shelf (a plant already there goes to storage).
        // The profile saves water and the new plant together first; if this file then failed to save, the plant
        // would simply be in storage on the next load: never lost, never free.
        internal BasementPlantResult TryBuyPlant(string slotId, string plantId)
        {
            var kind = catalog.Find(plantId);
            if (kind == null || Simulation == null || !Simulation.HasLayout || Simulation.Data.Legacy(slotId) != null)
                return BasementPlantResult.Invalid;
            if (!CanSpend) return BasementPlantResult.Unavailable;
            if (profile.Water < kind.BuyCost) return BasementPlantResult.NotEnoughWater;
            if (!profile.TryBuyBasementPlant(kind.PlantId, kind.BuyCost, out int id)) return BasementPlantResult.Unavailable;
            if (!Simulation.TryEquip(slotId, id))
                Debug.LogWarning($"Bought basement plant {id} could not be placed on '{slotId}'; it stays in storage.");
            SaveNow();
            return BasementPlantResult.Done;
        }

        // Puts an owned plant from storage on the shelf; the shelf's plant goes to storage. No payment.
        internal BasementPlantResult TryEquipPlant(string slotId, int instanceId)
        {
            if (Simulation == null || !ConnectProfile() || !profile.IsLoaded) return BasementPlantResult.Unavailable;
            if (Owned(instanceId).Kind == null || Simulation.Data.EquipOf(instanceId) != null ||
                !Simulation.TryEquip(slotId, instanceId)) return BasementPlantResult.Invalid;
            SaveNow();
            return BasementPlantResult.Done;
        }

        // Upgrades the shelf's plant by one level. Its current Eat/cooldown continue; the new cooldown applies at once.
        internal BasementPlantResult TryUpgradePlant(string slotId)
        {
            var plant = ShelfPlant(slotId);
            if (plant.Kind == null) return BasementPlantResult.Invalid;
            if (!plant.Kind.CanUpgradeFrom(plant.Level)) return BasementPlantResult.MaxLevel;
            if (!CanSpend) return BasementPlantResult.Unavailable;
            int cost = plant.Kind.UpgradeCostFrom(plant.Level);
            if (profile.Water < cost) return BasementPlantResult.NotEnoughWater;
            return profile.TryUpgradeBasementPlant(plant.InstanceId, plant.Level, cost, plant.Kind.MaxLevel)
                ? BasementPlantResult.Done : BasementPlantResult.Unavailable;
        }

        internal BasementRepairResult TryRepair()
        {
            if (!Simulation.GeneratorBroken) return BasementRepairResult.NotBroken;
            // Gold may only be spent while the profile saves immediately (never inside a working day).
            if (!ConnectProfile() || !profile.CanCommitImmediately) return BasementRepairResult.Unavailable;
            if (!profile.TryPayBasementRepair(settings.RepairCost)) return BasementRepairResult.NotEnoughGold;
            Simulation.Repair();
            Simulation.Data.repairsApplied = profile.BasementRepairsPaid;
            SaveNow();
            return BasementRepairResult.Repaired;
        }

        // End-of-day checkpoint: the generator's electricity for that day, decided once and reused on retry.
        internal int ClaimDayProduction(int day)
        {
            var data = Simulation.Data;
            if (data.productionDay == day) return data.productionAmount;
            if (!ConnectProfile() || !profile.IsLoaded)
                throw new InvalidOperationException("Generator production requires a loaded player profile.");
            data.productionDay = day;
            data.productionAmount = Simulation.GeneratorWorking ? profile.DailyGeneratorProduction : 0;
            SaveNow();
            return data.productionAmount;
        }

        private void ResetForNewProfile()
        {
            try { store.Delete(); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Debug.LogWarning("Old basement progress could not be deleted; it is replaced instead. " + error.Message);
            }
            Simulation.Replace(new BasementDefenseData());
            SaveNow();
        }

        private void FlushSaves()
        {
            if (Simulation == null) return;
            try
            {
                if (saveQueued || IsRunning) SaveNow(); // Snapshot timers and rat progress.
                else store.Flush();
            }
            catch (Exception error) { Debug.LogError("Basement progress save failed: " + error); }
        }

        private void OnApplicationPause(bool value)
        {
            if (value) FlushSaves();
            else skipNextDelta = true; // Time in the background is not simulated.
            paused = value;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) FlushSaves();
            else skipNextDelta = true;
            unfocused = !focused;
        }

        private void OnApplicationQuit() => FlushSaves();

        private void OnDestroy()
        {
            if (Instance != this) return;
            if (profile != null) profile.ProfileReset -= ResetForNewProfile;
            FlushSaves();
            Instance = null;
        }
    }
}
