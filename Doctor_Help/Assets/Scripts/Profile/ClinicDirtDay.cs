using System;
using System.Collections.Generic;

namespace DoctorHelp
{
    public enum ClinicVisitorDirt { None, Smelly, Bleeding }

    [Serializable]
    public sealed class ClinicWallStain
    {
        public int visitorIndex;
        public int floor;
        public float x, height, rotation;
        public int variant;
        public bool cleaned;

        internal ClinicWallStain Copy() => (ClinicWallStain)MemberwiseClone();
    }

    [Serializable]
    public sealed class ClinicDirtDay
    {
        internal const int Lifetime = 5;
        internal const int MaxStainsPerVisitor = 2;
        internal const int VariantCount = 6;
        public int day;
        public List<ClinicVisitorDirt> visitors = new();
        public float remainingFraction = 1f;
        public List<ClinicWallStain> stains = new();

        internal void AddVisitors(int count, Func<int> rollPercent)
        {
            bool addedDirtyVisitor = false;
            for (int i = visitors.Count; i < count; i++)
            {
                int roll = rollPercent();
                var kind = roll < 10 ? ClinicVisitorDirt.Smelly :
                    roll < 20 ? ClinicVisitorDirt.Bleeding : ClinicVisitorDirt.None;
                visitors.Add(kind);
                addedDirtyVisitor |= kind != ClinicVisitorDirt.None;
            }
            if (addedDirtyVisitor) UpdateRemainingStains();
        }

        internal bool IsExpiredOn(int currentDay) => currentDay - day >= Lifetime;

        internal void UpdateRemainingStains()
        {
            int total = stains.Count;
            int cleaned = 0;
            foreach (var stain in stains)
                if (stain.cleaned) cleaned++;
            if (cleaned == 0) return;

            // Visitors whose stains have not appeared yet still contribute to this day's dirt.
            for (int i = 0; i < visitors.Count; i++)
                if (visitors[i] != ClinicVisitorDirt.None && !stains.Exists(stain => stain.visitorIndex == i))
                    total++;
            remainingFraction = (float)(total - cleaned) / total;
        }

        internal float StarPenalty
        {
            get
            {
                int dirtyVisitors = 0;
                foreach (var visitor in visitors)
                    if (visitor != ClinicVisitorDirt.None) dirtyVisitors++;
                return dirtyVisitors * 0.5f * remainingFraction;
            }
        }

        internal ClinicDirtDay Copy()
        {
            var copy = new ClinicDirtDay
            {
                day = day,
                remainingFraction = remainingFraction,
                visitors = new List<ClinicVisitorDirt>(visitors)
            };
            foreach (var stain in stains) copy.stains.Add(stain.Copy());
            return copy;
        }

        internal bool IsValid()
        {
            if (!(remainingFraction >= 0f && remainingFraction <= 1f) ||
                visitors == null || visitors.Count > ClinicDemand.MaxIndoorVisitors + ClinicDemand.MaxFurnitureLevel ||
                stains == null) return false;
            foreach (var visitor in visitors)
                if (visitor < ClinicVisitorDirt.None || visitor > ClinicVisitorDirt.Bleeding) return false;
            var stainCounts = new int[visitors.Count];
            foreach (var stain in stains)
            {
                if (stain == null || stain.visitorIndex < 0 || stain.visitorIndex >= visitors.Count ||
                    visitors[stain.visitorIndex] == ClinicVisitorDirt.None ||
                    ++stainCounts[stain.visitorIndex] > MaxStainsPerVisitor ||
                    stain.floor < 0 || stain.floor > 1 || stain.variant < 0 || stain.variant >= VariantCount ||
                    !float.IsFinite(stain.x) || !float.IsFinite(stain.height) || !float.IsFinite(stain.rotation))
                    return false;
            }
            return true;
        }
    }
}
