using System;
using UnityEngine;

namespace DoctorHelp
{
    [Serializable]
    public struct BpmRange
    {
        [Min(1)] public int min;
        [Min(1)] public int max;

        public BpmRange(int min, int max) { this.min = min; this.max = max; }
        internal bool IsValid => min > 0 && max >= min;
        // Whole beats per minute, both ends included.
        internal int Sample(float randomUnit) =>
            Mathf.Min(max, min + Mathf.FloorToInt(Mathf.Clamp01(randomUnit) * (max - min + 1)));
        public override string ToString() => $"{min}-{max}";
    }

    [Serializable]
    public struct HeartbeatRanges
    {
        public BpmRange normal;
        public BpmRange slow;
        public BpmRange rapid;

        public HeartbeatRanges(BpmRange normal, BpmRange slow, BpmRange rapid)
        { this.normal = normal; this.slow = slow; this.rapid = rapid; }
        internal bool IsValid => normal.IsValid && slow.IsValid && rapid.IsValid;
    }

    [Serializable]
    public sealed class HeartbeatRhythm
    {
        // NPC Diff is the number of sicknesses (1-4); a healthy NPC uses Diff 1.
        private static readonly HeartbeatRanges[] DefaultRanges =
        {
            new HeartbeatRanges(new BpmRange(65, 80), new BpmRange(15, 30), new BpmRange(180, 200)),
            new HeartbeatRanges(new BpmRange(65, 80), new BpmRange(20, 40), new BpmRange(160, 200)),
            new HeartbeatRanges(new BpmRange(65, 80), new BpmRange(20, 50), new BpmRange(140, 200)),
            new HeartbeatRanges(new BpmRange(65, 80), new BpmRange(20, 50), new BpmRange(120, 200)),
        };

        [SerializeField, InspectorName("Diff 1")] private HeartbeatRanges diff1 = DefaultRanges[0];
        [SerializeField, InspectorName("Diff 2")] private HeartbeatRanges diff2 = DefaultRanges[1];
        [SerializeField, InspectorName("Diff 3")] private HeartbeatRanges diff3 = DefaultRanges[2];
        [SerializeField, InspectorName("Diff 4")] private HeartbeatRanges diff4 = DefaultRanges[3];
        [SerializeField] private string rapidHeartbeatSymptomId;
        [SerializeField] private string slowHeartbeatSymptomId;
        [SerializeField] private string noHeartbeatSymptomId;

        // Symptoms pick the range, NPC Diff picks its width. The patient caches one sample per consultation.
        public float Sample(NpcDefinition npc, AllSickness medicalData, float randomUnit)
        {
            if (npc == null) throw new InvalidOperationException("Heartbeat requires a valid NPC record.");
            if (medicalData == null || medicalData.SymptomProfile == null)
                throw new InvalidOperationException("Heartbeat requires a valid NPC record and medical catalogue.");
            var symptoms = medicalData.SymptomProfile;
            if (symptoms.Find(rapidHeartbeatSymptomId) == null || symptoms.Find(slowHeartbeatSymptomId) == null ||
                symptoms.Find(noHeartbeatSymptomId) == null || rapidHeartbeatSymptomId == slowHeartbeatSymptomId ||
                rapidHeartbeatSymptomId == noHeartbeatSymptomId || slowHeartbeatSymptomId == noHeartbeatSymptomId)
                throw new InvalidOperationException("Heartbeat symptom mappings are missing, duplicated or no longer valid.");
            var ranges = RangesFor(npc.Difficulty);
            if (!npc.HasSickness) return ranges.normal.Sample(randomUnit);
            var resolvedSymptoms = NpcMedicalConditions.Resolve(npc.SicknessIds, medicalData);
            bool rapid = false, slow = false, none = false;
            foreach (var id in resolvedSymptoms)
            {
                if (symptoms.Find(id) == null) throw new InvalidOperationException("Sickness references a missing symptom: " + id);
                rapid |= id == rapidHeartbeatSymptomId;
                slow |= id == slowHeartbeatSymptomId;
                none |= id == noHeartbeatSymptomId;
            }
            if (none) return 0;
            if (rapid && slow) throw new InvalidOperationException("Sickness has conflicting rapid and slow heartbeat symptoms.");
            return (rapid ? ranges.rapid : slow ? ranges.slow : ranges.normal).Sample(randomUnit);
        }

        private HeartbeatRanges RangesFor(int difficulty)
        {
            int index = Mathf.Clamp(difficulty, 1, DifficultyProfile.MaxDifficulty) - 1;
            var ranges = index switch { 0 => diff1, 1 => diff2, 2 => diff3, _ => diff4 };
            if (ranges.IsValid) return ranges;
            Debug.LogWarning($"Heartbeat Diff {index + 1} BPM ranges are invalid; using the defaults.");
            return DefaultRanges[index];
        }

        public static float DistanceGain(float distance, float maxDistance)
            => maxDistance > 0 ? Mathf.Clamp01(1 - distance / maxDistance) : 0;
    }
}
