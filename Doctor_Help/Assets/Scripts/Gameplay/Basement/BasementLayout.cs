using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    // What the simulation needs from the Player_Home basement, built from the scene by BasementDefenseView so the
    // scene stays the single source of the layout:
    //  - the pipe as world XY points sampled evenly by length (0 = right opening, 1 = generator end);
    //  - for every shelf: its mouth-zone windows (path fractions a melee plant can reach) and its world origin;
    //  - the measured muzzle (shot origin) of each plant kind standing on each shelf;
    //  - world units per metre, so Range in metres means the same whatever the Home/Basement transform scale.
    // Kept by BasementDefenseService after Player_Home unloads, so ranged plants keep working in other scenes.
    public sealed class BasementLayout
    {
        public readonly struct SlotZone
        {
            public readonly string SlotId;
            public readonly Vector2[] Windows; // (start, end) path fractions, in path order.
            public readonly Vector2 Origin;    // World XY of the plant pot.

            public SlotZone(string slotId, Vector2[] windows, Vector2 origin)
            {
                SlotId = slotId;
                Windows = windows;
                Origin = origin;
            }
        }

        // Default shot origin above the pot when a plant's muzzle was never measured (metres).
        private const float DefaultMuzzleHeightMetres = .6f;

        private readonly List<SlotZone> slots = new List<SlotZone>();
        private readonly Dictionary<(string slot, string plant), Vector2> muzzles = new Dictionary<(string, string), Vector2>();
        private Vector2[] path = new Vector2[0];

        public IReadOnlyList<SlotZone> Slots => slots;
        // World units in one metre (the scale of the Range space transform). Never zero.
        public float WorldPerMetre { get; private set; } = 1f;

        public void Add(string slotId, Vector2[] windows) => Add(slotId, windows, Vector2.zero);

        public void Add(string slotId, Vector2[] windows, Vector2 origin)
        {
            if (string.IsNullOrEmpty(slotId) || Contains(slotId)) return;
            slots.Add(new SlotZone(slotId, windows ?? new Vector2[0], origin));
        }

        public bool Contains(string slotId) => slots.Exists(slot => slot.SlotId == slotId);

        public void SetPath(Vector2[] points, float worldPerMetre)
        {
            path = points ?? new Vector2[0];
            WorldPerMetre = worldPerMetre > 1e-4f && !float.IsInfinity(worldPerMetre) ? worldPerMetre : 1f;
        }

        // World XY of a rat at this path fraction (linear between even samples).
        public Vector2 PathPosition(float fraction)
        {
            if (path.Length == 0) return Vector2.zero;
            if (path.Length == 1) return path[0];
            float scaled = Mathf.Clamp01(fraction) * (path.Length - 1);
            int index = Mathf.Min((int)scaled, path.Length - 2);
            return Vector2.Lerp(path[index], path[index + 1], scaled - index);
        }

        public void SetMuzzle(string slotId, string plantId, Vector2 world)
        {
            if (!string.IsNullOrEmpty(slotId) && !string.IsNullOrEmpty(plantId)) muzzles[(slotId, plantId)] = world;
        }

        // Where a plant of this kind on this shelf shoots from, and measures its Range from.
        public Vector2 Muzzle(string slotId, string plantId)
        {
            if (muzzles.TryGetValue((slotId, plantId), out var muzzle)) return muzzle;
            foreach (var slot in slots)
                if (slot.SlotId == slotId) return slot.Origin + Vector2.up * DefaultMuzzleHeightMetres * WorldPerMetre;
            return Vector2.zero;
        }
    }
}
