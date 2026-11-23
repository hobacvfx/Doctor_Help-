using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    // Gameplay rules of the basement, independent of any scene object: spawning, rats running the pipe (health,
    // slow), plants catching/eating or shooting them (targets, timing, ammo, projectiles in flight), and the wire
    // attack on the generator. The Player_Home view only reads this state, so results are identical whether or
    // not the basement is on screen, and rebuilding the view never repeats a hit.
    // One damaging hit on a rat (needle, petal or electric stream), for impacts and death effects.
    internal readonly struct BasementHitEvent
    {
        internal readonly long Sequence;
        internal readonly BasementPlantAttack Attack;
        internal readonly int RatId;
        internal readonly Vector2 Position;     // World XY of the rat when hit.
        internal readonly float PathFraction;   // Where on the pipe it was (0..1).
        internal readonly bool Killed;          // This hit took its last health.

        internal BasementHitEvent(long sequence, BasementPlantAttack attack, int ratId, Vector2 position, float pathFraction, bool killed)
        {
            Sequence = sequence; Attack = attack; RatId = ratId; Position = position; PathFraction = pathFraction; Killed = killed;
        }
    }

    internal sealed class BasementDefenseSimulation
    {
        private const float MaxStep = .05f; // Large frame deltas are split so no window or bite is skipped.

        private readonly BasementDefenseSettings settings;
        private BasementLayout layout;
        // Current stats of an owned plant instance (from its profile and level); null when it is not usable.
        private Func<int, BasementPlantStats?> statsOf = _ => null;

        internal BasementDefenseData Data { get; private set; }
        internal bool HasLayout => layout != null;
        internal BasementLayout Layout => layout;

        // Raised for every change worth saving (not for plain timer progress).
        internal event Action Changed;

        // Reused target buffer (rat, world distance), so stepping does not allocate.
        private readonly List<(BasementRatState rat, float distance)> candidates = new List<(BasementRatState, float)>();

        internal BasementDefenseSimulation(BasementDefenseSettings settings, BasementDefenseData data)
        {
            this.settings = settings;
            Data = data ?? new BasementDefenseData();
            NormalizeRats();
        }

        internal void Replace(BasementDefenseData data)
        {
            Data = data ?? new BasementDefenseData();
            NormalizeRats();
            Changed?.Invoke();
        }

        // Rats from older saves (or with a corrupted value) get full health; health never exceeds the maximum.
        private void NormalizeRats()
        {
            float max = settings.RatMaxHealth;
            foreach (var rat in Data.rats)
                if (!(rat.health > 0f) || rat.health > max) rat.health = max;
        }

        internal void SetLayout(BasementLayout value) => layout = value;
        internal void SetStatsSource(Func<int, BasementPlantStats?> source) => statsOf = source ?? (_ => null);

        // The generator fails at the wire_break moment of BreakWire, not when the attack starts.
        internal bool GeneratorBroken => Data.wire == BasementWireState.Broken ||
            (Data.wire == BasementWireState.Breaking && Data.wireSeconds >= settings.WireBreakSeconds);
        internal bool GeneratorWorking => !GeneratorBroken;

        internal void Step(float deltaSeconds)
        {
            if (layout == null || !(deltaSeconds > 0f)) return;
            while (deltaSeconds > 0f)
            {
                float step = Mathf.Min(deltaSeconds, MaxStep);
                StepOnce(step);
                deltaSeconds -= step;
            }
        }

        private void StepOnce(float dt)
        {
            Data.simulatedSeconds += dt;
            bool changed = AdvancePlants(dt);
            changed |= AdvanceProjectiles(dt);
            changed |= AdvanceArcs(dt);
            changed |= AdvanceWire(dt);
            changed |= AdvanceSpawning(dt);
            changed |= AdvanceRats(dt);
            if (changed) Changed?.Invoke();
        }

        // Every owned plant's timers run, on a shelf or in storage, so a swap never skips or restarts a cooldown.
        private bool AdvancePlants(float dt)
        {
            bool changed = false;
            foreach (var plant in Data.plants)
            {
                var rules = statsOf(plant.instanceId);
                if (rules != null && !rules.Value.Swallows)
                {
                    changed |= rules.Value.Attack == BasementPlantAttack.PetalMagazine
                        ? AdvanceMagazine(plant, rules.Value, dt) : AdvanceStrike(plant, rules.Value, dt);
                    continue;
                }
                switch (plant.state)
                {
                    case BasementPlantState.Eating:
                        plant.stateSeconds += dt;
                        if (plant.targetRatIds.Count > 0 && plant.stateSeconds >= settings.BiteSeconds)
                        {
                            // The jaws close: every rat in this Eat is gone, each exactly once.
                            foreach (int eaten in plant.targetRatIds) Data.rats.RemoveAll(rat => rat.id == eaten);
                            plant.targetRatIds.Clear();
                            changed = true;
                        }
                        if (plant.stateSeconds >= settings.EatSeconds)
                        {
                            plant.state = BasementPlantState.Cooldown;
                            plant.stateSeconds -= settings.EatSeconds;
                            changed = true;
                        }
                        break;
                    case BasementPlantState.Cooldown:
                        // Read every step: an upgrade shortens the remaining wait but never restarts it.
                        // Unknown stats (profile not connected yet) hold the timer rather than end it.
                        var stats = statsOf(plant.instanceId);
                        if (stats == null) break;
                        plant.stateSeconds += dt;
                        if (plant.stateSeconds >= stats.Value.CooldownSeconds)
                        {
                            plant.state = BasementPlantState.Ready;
                            plant.stateSeconds = 0f;
                            changed = true;
                        }
                        break;
                }
            }
            return changed;
        }

        // ---------- Ranged plants ----------

        // Volley / electric strike: starts only when a target is in range, so no strike is wasted. The hit lands
        // at the fire moment (scaled with the strike speed); the next strike may start Attack Cooldown after this
        // one started, so the cooldown is the time between strikes, not added on top of the clip.
        // In storage the timers still run, but a strike that reaches its fire moment there is dropped unfired.
        private bool AdvanceStrike(BasementPlantRuntime plant, BasementPlantStats stats, float dt)
        {
            string slot = Data.EquipOf(plant.instanceId)?.slotId;
            if (plant.state != BasementPlantState.Ready && plant.state != BasementPlantState.Attacking) ResetStrike(plant);
            bool changed = false;
            if (plant.state == BasementPlantState.Ready)
            {
                // The target is seen during this step: the strike counts from the start of the step.
                if (!TryStartStrike(plant, stats, slot, dt)) return false;
                changed = true;
            }
            else plant.stateSeconds += dt;

            if (!plant.fired && plant.stateSeconds >= stats.StrikeFireSeconds)
            {
                bool hit = slot != null && (stats.Attack == BasementPlantAttack.ElectricArc
                    ? FireArc(plant, stats, slot) : FireVolley(plant, stats, slot));
                if (!hit) { ResetStrike(plant); return true; } // Every target left before the fire moment.
                plant.fired = true;
                changed = true;
            }
            if (plant.fired && plant.stateSeconds >= stats.CooldownSeconds)
            {
                // Keep the exact cadence: the next strike begins when the cooldown ends, if a target is there.
                float over = plant.stateSeconds - stats.CooldownSeconds;
                ResetStrike(plant);
                TryStartStrike(plant, stats, slot, over);
                changed = true;
            }
            return changed;
        }

        private bool TryStartStrike(BasementPlantRuntime plant, BasementPlantStats stats, string slot, float elapsed)
        {
            if (slot == null || FindTargets(slot, stats, 1) == 0) return false;
            plant.state = BasementPlantState.Attacking;
            plant.stateSeconds = elapsed;
            plant.fired = false;
            plant.lockedRatId = stats.Attack == BasementPlantAttack.ElectricArc ? candidates[0].rat.id : 0;
            return true;
        }

        private static void ResetStrike(BasementPlantRuntime plant)
        {
            plant.state = BasementPlantState.Ready;
            plant.stateSeconds = 0f;
            plant.fired = false;
            plant.lockedRatId = 0;
            plant.targetRatIds.Clear();
        }

        // One needle at each of up to Targets different rats, nearest first.
        private bool FireVolley(BasementPlantRuntime plant, BasementPlantStats stats, string slot)
        {
            int count = FindTargets(slot, stats, stats.Targets);
            var muzzle = layout.Muzzle(slot, stats.PlantId);
            for (int index = 0; index < count; index++) Launch(plant, stats, muzzle, candidates[index].rat);
            return count > 0;
        }

        // The rat locked at the start of the charge if it is still valid and in range; otherwise the nearest one.
        // Damage is applied once, here; the arc record only keeps the stream visible.
        private bool FireArc(BasementPlantRuntime plant, BasementPlantStats stats, string slot)
        {
            int count = FindTargets(slot, stats, int.MaxValue);
            if (count == 0) return false;
            var target = candidates[0].rat;
            for (int index = 0; index < count; index++)
                if (candidates[index].rat.id == plant.lockedRatId) { target = candidates[index].rat; break; }
            var muzzle = layout.Muzzle(slot, stats.PlantId);
            var at = RatPosition(target);
            Data.arcs.Add(new BasementArc
            {
                id = Data.nextShotId++, ratId = target.id, slotId = slot, sourceInstanceId = plant.instanceId,
                fromX = muzzle.x, fromY = muzzle.y, toX = at.x, toY = at.y
            });
            Hit(target, stats.Attack, stats.Damage, stats.SlowFraction, stats.SlowSeconds);
            return true;
        }

        // Petal magazine: one petal per Attack Cooldown while a target exists (re-chosen for every petal), the
        // remaining petals are kept while there is none, and Reload starts only when the magazine is empty.
        private bool AdvanceMagazine(BasementPlantRuntime plant, BasementPlantStats stats, float dt)
        {
            float interval = stats.CooldownSeconds;
            int magazine = Mathf.Max(1, stats.Magazine);
            bool changed = false;
            if (plant.ammo < 0 || plant.ammo > magazine)
            {
                plant.ammo = magazine;
                plant.shotTimer = interval;
                plant.state = BasementPlantState.Ready;
                plant.stateSeconds = 0f;
            }
            if (plant.state == BasementPlantState.Reloading)
            {
                plant.stateSeconds += dt;
                if (plant.stateSeconds < stats.ReloadSeconds) return false;
                plant.ammo = magazine;
                plant.state = BasementPlantState.Ready;
                plant.stateSeconds = 0f;
                plant.shotTimer = interval; // Ready to fire at once.
                return true;
            }
            if (plant.state != BasementPlantState.Ready && plant.state != BasementPlantState.Attacking)
            {
                plant.state = plant.ammo < magazine ? BasementPlantState.Attacking : BasementPlantState.Ready;
                plant.stateSeconds = 0f;
            }

            plant.shotTimer += dt;
            string slot = Data.EquipOf(plant.instanceId)?.slotId;
            while (slot != null && plant.ammo > 0 && plant.shotTimer >= interval)
            {
                if (FindTargets(slot, stats, 1) == 0) break;
                Launch(plant, stats, layout.Muzzle(slot, stats.PlantId), candidates[0].rat);
                plant.ammo--;
                plant.shotTimer -= interval;
                plant.state = BasementPlantState.Attacking;
                changed = true;
                if (plant.ammo > 0) continue;
                plant.state = BasementPlantState.Reloading; // Last petal gone: regrow the magazine.
                plant.stateSeconds = 0f;
            }
            // Waiting for a target never banks extra petals: at most one is ready the moment a rat appears.
            plant.shotTimer = Mathf.Min(plant.shotTimer, interval);
            return changed;
        }

        private void Launch(BasementPlantRuntime plant, BasementPlantStats stats, Vector2 muzzle, BasementRatState target)
        {
            Data.projectiles.Add(new BasementProjectile
            {
                id = Data.nextShotId++, kind = stats.Attack, ratId = target.id, sourceInstanceId = plant.instanceId,
                x = muzzle.x, y = muzzle.y, damage = stats.Damage, speed = stats.ProjectileSpeed * layout.WorldPerMetre,
                slowFraction = stats.SlowFraction, slowSeconds = stats.SlowSeconds
            });
        }

        // Valid targets of a shelf's plant, nearest first (equal distances: lower rat ID first). Returns how many
        // of them (up to max) are at the front of candidates. A rat is valid while alive, running (not held by a
        // RatEater) and short of the generator, and — for Normal range — within Range metres in world XY.
        private int FindTargets(string slot, BasementPlantStats stats, int max)
        {
            candidates.Clear();
            var muzzle = layout.Muzzle(slot, stats.PlantId);
            float reach = stats.Range == BasementPlantRange.Far ? float.PositiveInfinity
                : stats.Range == BasementPlantRange.Normal ? stats.RangeMetres * layout.WorldPerMetre : 0f;
            foreach (var rat in Data.rats)
            {
                if (!IsTargetable(rat)) continue;
                float distance = Vector2.Distance(muzzle, RatPosition(rat));
                if (distance <= reach) candidates.Add((rat, distance));
            }
            candidates.Sort((a, b) => a.distance != b.distance ? a.distance.CompareTo(b.distance) : a.rat.id.CompareTo(b.rat.id));
            return Mathf.Min(max, candidates.Count);
        }

        private bool IsTargetable(BasementRatState rat) =>
            rat != null && !rat.IsCaught && rat.health > 0f && rat.travelled < settings.TravelSeconds;

        internal Vector2 RatPosition(BasementRatState rat) =>
            layout != null ? layout.PathPosition(rat.travelled / settings.TravelSeconds) : Vector2.zero;

        // Needles and petals home on their rat; each hits once and is removed. A projectile whose rat is gone,
        // dead or held by a RatEater simply disappears (no damage, no exception).
        private bool AdvanceProjectiles(float dt)
        {
            bool changed = false;
            for (int index = 0; index < Data.projectiles.Count; index++)
            {
                var shot = Data.projectiles[index];
                var rat = Data.rats.Find(candidate => candidate.id == shot.ratId);
                if (!IsTargetable(rat)) { Data.projectiles.RemoveAt(index--); changed = true; continue; }
                shot.age += dt;
                var position = new Vector2(shot.x, shot.y);
                var target = RatPosition(rat);
                float step = shot.speed * dt;
                if (Vector2.Distance(position, target) > step)
                {
                    position = Vector2.MoveTowards(position, target, step);
                    shot.x = position.x;
                    shot.y = position.y;
                    continue;
                }
                Data.projectiles.RemoveAt(index--);
                Hit(rat, shot.kind, shot.damage, shot.slowFraction, shot.slowSeconds);
                changed = true;
            }
            return changed;
        }

        private bool AdvanceArcs(float dt)
        {
            if (Data.arcs.Count == 0) return false;
            foreach (var arc in Data.arcs) arc.age += dt;
            return Data.arcs.RemoveAll(arc => arc.age >= settings.ArcVisibleSeconds) > 0;
        }

        // Applies damage (exact float, no rounding) and refreshes the slow; a rat at 0 health dies once and is gone.
        // Slow never stacks: a hit sets the time back to its full length and keeps the stronger slow while active.
        private void Hit(BasementRatState rat, BasementPlantAttack attack, float damage, float slowFraction, float slowSeconds)
        {
            var at = RatPosition(rat);
            float fraction = rat.travelled / settings.TravelSeconds;
            if (!(rat.health > 0f)) rat.health = settings.RatMaxHealth;
            rat.health -= damage;
            if (slowFraction > 0f && slowSeconds > 0f)
            {
                rat.slowFraction = rat.slowSeconds > 0f ? Mathf.Max(rat.slowFraction, slowFraction) : slowFraction;
                rat.slowSeconds = Mathf.Max(rat.slowSeconds, slowSeconds);
            }
            bool killed = !(rat.health > 0f);
            Record(new BasementHitEvent(++EventSequence, attack, rat.id, at, fraction, killed));
            if (!killed) return;
            rat.health = 0f;
            Data.rats.Remove(rat);
        }

        // ---------- Hit history for presentation (impacts, deaths) ----------
        // Not saved and never read by the rules: the view shows only hits it saw happen, so a rebuilt view or a
        // restart never replays an old impact or death.
        private const int EventCapacity = 128;
        private readonly BasementHitEvent[] events = new BasementHitEvent[EventCapacity];

        // Sequence of the latest hit (0: none yet). A view starts reading from the value current when it appears.
        internal long EventSequence { get; private set; }

        private void Record(BasementHitEvent hit) => events[(int)(hit.Sequence % EventCapacity)] = hit;

        // Adds the hits after `cursor` (oldest first; at most the last EventCapacity) and returns the new cursor.
        internal long CopyHitsSince(long cursor, List<BasementHitEvent> into)
        {
            long first = Math.Max(cursor + 1, EventSequence - EventCapacity + 1);
            for (long sequence = Math.Max(1, first); sequence <= EventSequence; sequence++)
                into.Add(events[(int)(sequence % EventCapacity)]);
            return EventSequence;
        }

        private bool AdvanceWire(float dt)
        {
            if (Data.wire != BasementWireState.Breaking) return false;
            bool wasBroken = GeneratorBroken;
            Data.wireSeconds += dt;
            if (Data.wireSeconds >= settings.BreakWireSeconds)
            {
                Data.wire = BasementWireState.Broken; // BreakWire done; Gnaw loops from here.
                Data.wireSeconds = 0f;
                return true;
            }
            return GeneratorBroken != wasBroken;
        }

        private bool AdvanceSpawning(float dt)
        {
            Data.spawnTimer += dt;
            bool spawned = false;
            while (Data.spawnTimer >= settings.SpawnIntervalSeconds)
            {
                Data.spawnTimer -= settings.SpawnIntervalSeconds;
                // A rat spawned partway through the step has already run for the remainder.
                Data.rats.Add(new BasementRatState
                    { id = Data.nextRatId++, travelled = Data.spawnTimer, health = settings.RatMaxHealth });
                spawned = true;
            }
            return spawned;
        }

        private bool AdvanceRats(float dt)
        {
            bool changed = false;
            float travel = settings.TravelSeconds;
            for (int index = 0; index < Data.rats.Count; index++)
            {
                var rat = Data.rats[index];
                if (rat.IsCaught) continue;
                float before = rat.travelled;
                // travelled is progress in unslowed seconds: a slowed rat covers (1 − slow) of a second per second.
                rat.travelled += dt * rat.SpeedMultiplier;
                if (rat.slowSeconds > 0f)
                {
                    rat.slowSeconds = Mathf.Max(0f, rat.slowSeconds - dt);
                    if (rat.slowSeconds <= 1e-5f) { rat.slowSeconds = 0f; rat.slowFraction = 0f; }
                }
                if (TryCatch(rat, before)) { changed = true; continue; }
                if (rat.travelled < travel) continue;

                // Reached the generator end of the pipe: the wiring rat stands in for it from now on.
                Data.rats.RemoveAt(index--);
                if (Data.wire == BasementWireState.Intact)
                {
                    Data.wire = BasementWireState.Breaking;
                    Data.wireSeconds = Mathf.Max(0f, rat.travelled - travel);
                }
                changed = true;
            }
            return changed;
        }

        // A ready plant starts an Eat on the first untargeted rat inside its mouth zone (including one already
        // there when the cooldown ends). Until the bite, further rats entering the zone join the same Eat while
        // the plant has Targets left; all of them glide into the mouth and vanish together at the bite.
        // Rats are checked in spawn order, so the leading rat is preferred.
        private bool TryCatch(BasementRatState rat, float before)
        {
            float travel = settings.TravelSeconds;
            foreach (var zone in layout.Slots)
            {
                var equip = Data.Equip(zone.SlotId);
                if (equip == null) continue;
                var stats = statsOf(equip.instanceId);
                if (stats == null || !stats.Value.Swallows) continue;
                var plant = Data.RuntimeOrNew(equip.instanceId);
                bool starts = plant.state == BasementPlantState.Ready;
                bool joins = plant.state == BasementPlantState.Eating && plant.targetRatIds.Count > 0 &&
                    plant.targetRatIds.Count < stats.Value.Targets && plant.stateSeconds < settings.BiteSeconds;
                if (!starts && !joins) continue;
                foreach (var window in zone.Windows)
                {
                    float start = window.x * travel, end = window.y * travel;
                    if (rat.travelled < start || before > end) continue;
                    rat.travelled = Mathf.Clamp(rat.travelled, start, end);
                    rat.caughtBy = zone.SlotId;
                    if (starts)
                    {
                        plant.state = BasementPlantState.Eating;
                        plant.stateSeconds = 0f;
                        plant.targetRatIds.Clear();
                    }
                    rat.caughtAt = plant.stateSeconds;
                    plant.targetRatIds.Add(rat.id);
                    return true;
                }
            }
            return false;
        }

        // Puts an owned plant on a shelf. The plant it replaces goes to storage with its timers; if it was still
        // closing on rats, they are released (they keep running and can be caught again), never eaten twice.
        // Ownership and payment are checked by BasementDefenseService before this call.
        internal bool TryEquip(string slotId, int instanceId)
        {
            if (string.IsNullOrEmpty(slotId) || instanceId < 1 || (layout != null && !layout.Contains(slotId)) ||
                Data.Legacy(slotId) != null) return false;
            var current = Data.Equip(slotId);
            if (current != null && current.instanceId == instanceId) return false;
            if (Data.EquipOf(instanceId) != null) return false; // Already on another shelf.
            if (current != null) Unequip(current);
            Data.equips.Add(new BasementSlotEquip { slotId = slotId, instanceId = instanceId });
            Data.RuntimeOrNew(instanceId);
            Changed?.Invoke();
            return true;
        }

        private void Unequip(BasementSlotEquip equip)
        {
            var plant = Data.Runtime(equip.instanceId);
            if (plant != null)
            {
                foreach (int id in plant.targetRatIds)
                {
                    var rat = Data.rats.Find(candidate => candidate.id == id);
                    if (rat == null) continue;
                    rat.caughtBy = "";
                    rat.caughtAt = 0f;
                }
                // The Eat (now without prey) and the cooldown after it continue in storage.
                plant.targetRatIds.Clear();
            }
            Data.equips.Remove(equip);
        }

        // Removes plants the profile does not own (e.g. a profile restored from backup), releasing their rats.
        internal bool RemoveUnowned(Predicate<int> owned)
        {
            bool changed = false;
            foreach (var equip in Data.equips.ToArray())
                if (!owned(equip.instanceId)) { Unequip(equip); changed = true; }
            changed |= Data.plants.RemoveAll(plant => !owned(plant.instanceId)) > 0;
            if (changed) Changed?.Invoke();
            return changed;
        }

        // Save migration: a schema 1 shelf plant now owned by the profile takes its shelf with its old timers.
        internal void AdoptLegacy(BasementLegacyPlant legacy, int instanceId)
        {
            Data.pendingLegacy.Remove(legacy);
            if (Data.Equip(legacy.slotId) != null || Data.EquipOf(instanceId) != null) { Changed?.Invoke(); return; }
            Data.equips.Add(new BasementSlotEquip { slotId = legacy.slotId, instanceId = instanceId });
            var plant = Data.RuntimeOrNew(instanceId);
            plant.state = legacy.state;
            plant.stateSeconds = legacy.stateSeconds;
            plant.targetRatIds.Clear();
            if (legacy.targetRatId != 0) plant.targetRatIds.Add(legacy.targetRatId);
            Changed?.Invoke();
        }

        // Restores intact wiring and a working generator. Plants, rats and the spawn timer are untouched.
        internal bool Repair()
        {
            if (!GeneratorBroken) return false;
            Data.wire = BasementWireState.Intact;
            Data.wireSeconds = 0f;
            Changed?.Invoke();
            return true;
        }

        // Used only to undo a repair whose gold payment never reached the saved profile.
        internal void ForceBroken()
        {
            Data.wire = BasementWireState.Broken;
            Data.wireSeconds = 0f;
            Changed?.Invoke();
        }
    }
}
