using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    // Memory-only history, available for results processing before the day checkpoint.
    [Serializable] public sealed class TreatmentJournal
    {
        [Serializable] public sealed class Item
        {
            public string id, name;
            public int quantity = 1;
        }
        [Serializable] public sealed class Entry
        {
            public string consultationId, npcId, npcName, givenAtUtc;
            public TreatmentDecisionKind decisionKind;
            public PoliceReportReason reportReason;
            public string reportedSicknessId;
            public int patienceStars = 5;
            public bool hauntingCleared;
            public List<Item> sicknesses = new List<Item>();
            public List<Item> medicines = new List<Item>();
        }
        [Serializable] public sealed class Day
        {
            public int day;
            public List<Entry> treatments = new List<Entry>();
        }
        [SerializeField] private Day current;
        // Unity inline serialization replaces null custom classes with default instances.
        // An empty Day (including a restored day 0) is not a pending working day.
        private bool HasEntries => current != null && current.treatments != null && current.treatments.Count > 0;
        public bool Contains(string consultationId) => HasEntries && current.treatments.Exists(x => x.consultationId == consultationId);
        private Entry Find(string consultationId) => HasEntries ? current.treatments.Find(x => x.consultationId == consultationId) : null;
        public bool MatchesMedication(string consultationId, IReadOnlyList<string> ids)
        {
            var entry = Find(consultationId);
            if (entry == null || entry.decisionKind != TreatmentDecisionKind.GiveMedicine || ids == null || entry.medicines.Count != ids.Count) return false;
            var recorded = new HashSet<string>();
            foreach (var item in entry.medicines) recorded.Add(item.id);
            return recorded.SetEquals(ids);
        }
        public bool MatchesPoliceReport(string consultationId, PoliceReportReason reason)
        {
            var entry = Find(consultationId);
            return entry != null && entry.decisionKind == TreatmentDecisionKind.ReportToPolice && entry.reportReason == reason;
        }
        public void Clear() => current = null;

        // A caller can inspect results without mutating the active day's history.
        public Day GetSnapshot()
        {
            if (!HasEntries) return null;
            var copy = new Day { day = current.day };
            foreach (var entry in current.treatments)
            {
                var cloned = new Entry { consultationId = entry.consultationId, npcId = entry.npcId,
                    npcName = entry.npcName, givenAtUtc = entry.givenAtUtc, decisionKind = entry.decisionKind,
                    reportReason = entry.reportReason, reportedSicknessId = entry.reportedSicknessId,
                    patienceStars = entry.patienceStars, hauntingCleared = entry.hauntingCleared };
                foreach (var item in entry.sicknesses) cloned.sicknesses.Add(Copy(item));
                foreach (var item in entry.medicines) cloned.medicines.Add(Copy(item));
                copy.treatments.Add(cloned);
            }
            return copy;
        }
        private static Item Copy(Item item) => new Item { id = item.id, name = item.name, quantity = item.quantity };

        public void Record(int day, PatientExaminationProfile patient, PillProfile pills, IReadOnlyList<string> ids)
        {
            if (Contains(patient.ConsultationId))
            {
                if (MatchesMedication(patient.ConsultationId, ids)) return;
                throw new InvalidOperationException("This consultation already has a different decision.");
            }
            var entry = CreateEntry(day, patient);
            foreach (var id in ids) entry.medicines.Add(new Item { id = id, name = pills.Find(id).DisplayName });
            Append(day, entry);
        }

        public void RecordPoliceReport(int day, PatientExaminationProfile patient, PoliceReportReason reason)
        {
            string sicknessId = PoliceReportReasons.SicknessId(reason);
            if (Contains(patient.ConsultationId))
            {
                if (MatchesPoliceReport(patient.ConsultationId, reason)) return;
                throw new InvalidOperationException("This consultation already has a different decision.");
            }
            var entry = CreateEntry(day, patient);
            entry.decisionKind = TreatmentDecisionKind.ReportToPolice;
            entry.reportReason = reason;
            entry.reportedSicknessId = sicknessId;
            Append(day, entry);
        }

        public void RecordDecline(int day, PatientExaminationProfile patient,
            TreatmentDecisionKind decision = TreatmentDecisionKind.Decline)
        {
            var previous = Find(patient.ConsultationId);
            if (previous != null)
            {
                if (previous.decisionKind == decision) return;
                throw new InvalidOperationException("This consultation already has a different decision.");
            }
            var entry = CreateEntry(day, patient);
            entry.decisionKind = decision;
            Append(day, entry);
        }

        private Entry CreateEntry(int day, PatientExaminationProfile patient)
        {
            if (day < 1) throw new ArgumentOutOfRangeException(nameof(day));
            if (HasEntries && current.day != day)
                throw new InvalidOperationException("Previous day's treatment history has not been completed.");
            var npc = patient.Definition;
            var entry = new Entry { consultationId = patient.ConsultationId, npcId = npc.Id,
                npcName = npc.NpcName, givenAtUtc = DateTime.UtcNow.ToString("O"),
                patienceStars = patient.PatienceStars, hauntingCleared = patient.HauntingCleared };
            foreach (var id in npc.SicknessIds)
                entry.sicknesses.Add(new Item { id = id, name = patient.Profile.AllSickness.Find(id)?.DisplayName });
            return entry;
        }
        private void Append(int day, Entry entry)
        {
            // Build the complete entry before touching history; failed validation cannot append a partial record.
            if (!HasEntries) current = new Day { day = day };
            current.treatments.Add(entry);
        }
    }
}
