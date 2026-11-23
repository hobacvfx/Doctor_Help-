using System;
using System.Collections.Generic;

namespace DoctorHelp
{
    // Ephemeral per-consultation evidence; never serialized into the player profile.
    public sealed class ConsultationClueJournal
    {
        private readonly HashSet<string> found = new HashSet<string>(StringComparer.Ordinal);
        public string ConsultationId { get; private set; }
        public bool Unread { get; private set; }
        public int Count => found.Count;
        public bool Contains(string id) => id != null && found.Contains(id);
        public bool Begin(string consultationId)
        {
            if (ConsultationId == consultationId) return false;
            ConsultationId = consultationId; found.Clear(); Unread = false; return true;
        }
        public bool Discover(string consultationId, string symptomId)
        {
            if (string.IsNullOrEmpty(consultationId) || consultationId != ConsultationId || string.IsNullOrEmpty(symptomId) || !found.Add(symptomId)) return false;
            Unread = true; return true;
        }
        public void Acknowledge() => Unread = false;
    }

    public sealed class ContinuousClueHold
    {
        public float Seconds { get; private set; }
        public bool Advance(bool eligible, float delta, float required)
        {
            if (!eligible) { Reset(); return false; }
            Seconds += Math.Max(0, delta);
            return Seconds + .00001f >= required;
        }
        public void Reset() => Seconds = 0;
    }
}
