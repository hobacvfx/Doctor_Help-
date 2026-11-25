using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    [CreateAssetMenu(fileName = "Diff_System", menuName = "Doctor Help/Data/Difficulty Profile")]
    public sealed class DifficultyProfile : ScriptableObject
    {
        public const int MaxDifficulty = 4;
        public const float DefaultSuccessStreak = 2f;

        [SerializeField, Min(1), Tooltip("Consecutive clinic days with at most one incorrect consultation in total. Rounded up to whole days.")]
        private float successStreak = DefaultSuccessStreak;

        [SerializeField] private SpawnWeights progress1 = new(0, 0, 0);
        [SerializeField] private SpawnWeights progress2 = new(50, 0, 0);
        [SerializeField] private SpawnWeights progress3 = new(50, 30, 0);
        [SerializeField] private SpawnWeights progress4 = new(35, 35, 30);

        public float SuccessStreak => successStreak;

        [Serializable]
        private struct SpawnWeights
        {
            [Range(0, 100), InspectorName("Diff 2 (%)")] public float diff2;
            [Range(0, 100), InspectorName("Diff 3 (%)")] public float diff3;
            [Range(0, 100), InspectorName("Diff 4 (%)")] public float diff4;

            public SpawnWeights(float diff2, float diff3, float diff4)
            { this.diff2 = diff2; this.diff3 = diff3; this.diff4 = diff4; }

            public float For(int difficulty) => difficulty switch
            {
                1 => 100f - diff2 - diff3 - diff4,
                2 => diff2,
                3 => diff3,
                4 => diff4,
                _ => 0f
            };

            public bool IsValid => diff2 >= 0 && diff3 >= 0 && diff4 >= 0 && For(1) >= 0;
        }

        private SpawnWeights WeightsFor(int progress)
        {
            var weights = progress switch
            {
                1 => progress1,
                2 => progress2,
                3 => progress3,
                4 => progress4,
                _ => throw new ArgumentOutOfRangeException(nameof(progress))
            };
            if (!weights.IsValid)
                throw new InvalidOperationException($"Diff_System Progress {progress} percentages must add up to at most 100%.");
            return weights;
        }

        internal static DifficultyProfile Load()
        {
            var profile = Resources.Load<DifficultyProfile>("Diff_System");
            return profile != null ? profile : throw new InvalidOperationException("Missing Resources/Diff_System difficulty profile.");
        }

        internal List<NpcDefinition>[] GroupPatients(IReadOnlyList<NpcDefinition> roster, int progress)
        {
            var weights = WeightsFor(progress);
            var groups = new List<NpcDefinition>[MaxDifficulty];
            for (int i = 0; i < groups.Length; i++) groups[i] = new List<NpcDefinition>();
            foreach (var npc in roster)
                if (weights.For(npc.Difficulty) > 0) groups[npc.Difficulty - 1].Add(npc);
            return groups;
        }

        internal int ChooseGroup(IReadOnlyList<List<NpcDefinition>> groups, int progress, float roll)
        {
            var weights = WeightsFor(progress);
            float total = 0;
            for (int i = 0; i < groups.Count; i++)
                if (groups[i].Count > 0) total += weights.For(i + 1);

            float remaining = roll * total;
            int lastAvailable = -1;
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].Count == 0 || weights.For(i + 1) <= 0) continue;
                lastAvailable = i;
                remaining -= weights.For(i + 1);
                if (remaining < 0) return i;
            }
            return lastAvailable; // Random.value can be exactly 1.
        }

        internal static void Advance(PlayerProfileData nextDay, int incorrectPatients,
            bool hadConsultations, float requiredDays)
        {
            if (!(requiredDays >= 1f && float.IsFinite(requiredDays)))
                throw new InvalidOperationException("Diff_System Success Streak must be at least one day.");
            if (!hadConsultations || incorrectPatients > 1 || nextDay.diffProgress == MaxDifficulty)
            {
                ClearStreak(nextDay);
                return;
            }

            int completedDay = nextDay.currentDay - 1;
            nextDay.diffStreakDays++;
            if (incorrectPatients == 1)
            {
                // Drop the oldest mistake and keep the consecutive days after it.
                if (nextDay.diffStreakMistakeDay != 0)
                    nextDay.diffStreakDays = completedDay - nextDay.diffStreakMistakeDay;
                nextDay.diffStreakMistakeDay = completedDay;
            }
            if (nextDay.diffStreakDays < (double)requiredDays) return;
            nextDay.diffProgress++;
            ClearStreak(nextDay);
        }

        private static void ClearStreak(PlayerProfileData profile)
        {
            profile.diffStreakDays = 0;
            profile.diffStreakMistakeDay = 0;
        }
    }
}
