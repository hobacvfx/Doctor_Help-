using System;
using System.Collections.Generic;

namespace DoctorHelp
{
    public sealed class DayTreatmentResult
    {
        internal const int CorrectGold = 25;
        internal const int IncorrectGold = -20;
        public int Day { get; }
        public int CorrectPatients { get; }
        public int IncorrectPatients { get; }
        public bool HasCompletedConsultations => CorrectPatients > 0 || IncorrectPatients > 0;
        public bool IsSuccess => HasCompletedConsultations && IncorrectPatients == 0;
        public int TotalEarn { get; }
        public int TotalLoss => checked(IncorrectPatients * -IncorrectGold);
        public int NetGold => TotalEarn - TotalLoss;
        public int GoldBefore { get; }
        public int GoldAfter => checked(GoldBefore + NetGold);
        public IReadOnlyList<PatientTreatmentResult> Patients { get; }
        public float? DailyRating { get; }
        public bool HasDirtyClinicFeedback { get; }
        public bool HasColdClinicFeedback { get; }
        // Passive income from furniture (gold per minute open), already included in TotalEarn.
        public int PassiveGold { get; }
        // True when the rating-based daily client limit closed the clinic early.
        public bool ClientLimitReached { get; internal set; }
        public DayTreatmentResult(int day, int correct, int incorrect)
            : this(day, correct, incorrect, 0, new List<PatientTreatmentResult>()) { }
        internal DayTreatmentResult(int day, int correct, int incorrect, int goldBefore, List<PatientTreatmentResult> patients,
            int passiveGold = 0)
        {
            Day = day;
            CorrectPatients = correct;
            IncorrectPatients = incorrect;
            GoldBefore = goldBefore;
            Patients = patients.AsReadOnly();
            TotalEarn = patients.Count == 0 ? checked(correct * CorrectGold) : 0;
            float totalStars = 0;
            int votes = 0;
            foreach (var patient in patients)
            {
                if (patient.IsCorrect) TotalEarn = checked(TotalEarn + patient.GoldDelta);
                if (patient.FeedbackStars is not float stars) continue;
                totalStars += stars;
                votes++;
                HasDirtyClinicFeedback |= patient.RatingReducedByDirt;
                HasColdClinicFeedback |= patient.RatingReducedByGhost;
            }
            if (votes > 0) DailyRating = totalStars / votes;
            PassiveGold = passiveGold;
            TotalEarn = checked(TotalEarn + passiveGold);
        }
    }

    public enum TreatmentTextTone { Normal, Correct, Incorrect }

    public sealed class TreatmentTextPart
    {
        public string Text { get; }
        public TreatmentTextTone Tone { get; }
        public TreatmentTextPart(string text, TreatmentTextTone tone = TreatmentTextTone.Normal)
        { Text = text; Tone = tone; }
    }

    // Immutable presentation snapshot survives journal cleanup and Home/video retries.
    public sealed class PatientTreatmentResult
    {
        public string NpcId { get; }
        public string NpcName { get; }
        public string Illness { get; }
        public bool IsCorrect { get; }
        public float? FeedbackStars { get; }
        public bool IsJailed { get; }
        public bool RatingReducedByDirt { get; }
        public bool RatingReducedByGhost { get; }
        public int GoldDelta { get; }
        public IReadOnlyList<TreatmentTextPart> Required { get; }
        public IReadOnlyList<TreatmentTextPart> Action { get; }
        internal PatientTreatmentResult(TreatmentJournal.Entry entry, bool correct, bool isPoliceCase,
            List<TreatmentTextPart> required, List<TreatmentTextPart> action, float clinicDirtPenalty,
            float receptionRatingBonus, float goldMultiplier, float ghostPenalty)
        {
            NpcId = entry.npcId; NpcName = entry.npcName; IsCorrect = correct;
            GoldDelta = correct ? (int)Math.Round(DayTreatmentResult.CorrectGold * goldMultiplier, MidpointRounding.AwayFromZero) :
                DayTreatmentResult.IncorrectGold;
            IsJailed = isPoliceCase && correct;
            if (!isPoliceCase)
            {
                int baseStars = correct ? Math.Clamp(entry.patienceStars, 1, 5) : 1;
                float withoutGhosts = Math.Clamp(baseStars - clinicDirtPenalty + receptionRatingBonus, 1f, PlayerProfileData.MaxRating);
                float stars = Math.Clamp(baseStars - clinicDirtPenalty - ghostPenalty + receptionRatingBonus, 1f, PlayerProfileData.MaxRating);
                FeedbackStars = stars;
                RatingReducedByDirt = withoutGhosts < Math.Min(baseStars + receptionRatingBonus, PlayerProfileData.MaxRating);
                RatingReducedByGhost = stars < withoutGhosts;
            }
            Illness = string.Join(" + ", entry.sicknesses.ConvertAll(x => x.name));
            Required = required.AsReadOnly(); Action = action.AsReadOnly();
        }
    }

    public static class TreatmentEvaluator
    {
        public static DayTreatmentResult Evaluate(int day, TreatmentJournal.Day history, AllSickness medical,
            int goldBefore = 0, float clinicDirtPenalty = 0f, float receptionRatingBonus = 0f,
            float goldMultiplier = 1f, float ghostPenalty = 0f, int passiveGold = 0)
        {
            if (!(goldMultiplier >= 1f && float.IsFinite(goldMultiplier)))
                throw new ArgumentOutOfRangeException(nameof(goldMultiplier));
            if (passiveGold < 0) throw new ArgumentOutOfRangeException(nameof(passiveGold));
            if (!(ghostPenalty >= 0f && float.IsFinite(ghostPenalty)))
                throw new ArgumentOutOfRangeException(nameof(ghostPenalty));
            if (!(clinicDirtPenalty >= 0f && float.IsFinite(clinicDirtPenalty)))
                throw new ArgumentOutOfRangeException(nameof(clinicDirtPenalty));
            if (!(receptionRatingBonus >= 0f && float.IsFinite(receptionRatingBonus)))
                throw new ArgumentOutOfRangeException(nameof(receptionRatingBonus));
            if (day < 1 || (history != null && (history.day != day || history.treatments == null)))
                throw new InvalidOperationException("Missing treatment history for the closing day.");
            if (history == null || history.treatments.Count == 0)
                return new DayTreatmentResult(day, 0, 0, goldBefore, new List<PatientTreatmentResult>(), passiveGold);
            if (medical == null || medical.PillProfile == null)
                throw new InvalidOperationException("Medical catalogue is unavailable for day results.");
            int correct = 0, incorrect = 0;
            var patients = new List<PatientTreatmentResult>();
            var consultations = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in history.treatments)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.consultationId) || !consultations.Add(entry.consultationId))
                    throw new InvalidOperationException("Treatment history contains a missing or duplicate consultation.");
                bool valid = IsCorrect(entry, medical);
                if (valid) correct++; else incorrect++;
                patients.Add(BuildPatient(entry, medical, valid, clinicDirtPenalty, receptionRatingBonus, goldMultiplier, ghostPenalty));
            }
            return new DayTreatmentResult(day, correct, incorrect, goldBefore, patients, passiveGold);
        }

        private static PatientTreatmentResult BuildPatient(TreatmentJournal.Entry entry, AllSickness medical,
            bool correct, float clinicDirtPenalty, float receptionRatingBonus, float goldMultiplier, float ghostPenalty)
        {
            var required = new List<TreatmentTextPart>();
            var action = new List<TreatmentTextPart>();
            bool police = entry.sicknesses.Exists(x => x.id == NpcMedicalConditions.Vampire || x.id == NpcMedicalConditions.Zombie);
            var requiredIds = new HashSet<string>(StringComparer.Ordinal);
            if (police)
            {
                string reason = entry.sicknesses.Exists(x => x.id == NpcMedicalConditions.Vampire) ? "Vampire infected" : "Zombie infected";
                required.Add(new TreatmentTextPart("Report to police (" + reason + ")"));
            }
            else
                foreach (var disease in entry.sicknesses)
                    foreach (var id in medical.Find(disease.id).PillIds)
                        if (requiredIds.Add(id))
                        {
                            bool supplied = entry.decisionKind == TreatmentDecisionKind.GiveMedicine && entry.medicines.Exists(x => x.id == id && x.quantity > 0);
                            required.Add(new TreatmentTextPart(medical.PillProfile.Find(id).DisplayName,
                                supplied ? TreatmentTextTone.Normal : TreatmentTextTone.Incorrect));
                        }
            if (!police && entry.sicknesses.Exists(x => medical.Find(x.id).Treatment == SicknessTreatment.RemoveGhosts))
            {
                required.Insert(0, new TreatmentTextPart("Remove all flies"));
                action.Add(new TreatmentTextPart(entry.hauntingCleared ? "Removed all flies" : "Flies remaining",
                    entry.hauntingCleared ? TreatmentTextTone.Correct : TreatmentTextTone.Incorrect));
            }
            if (entry.decisionKind == TreatmentDecisionKind.HitPatientWithRock)
                action.Add(new TreatmentTextPart("Hit patient with a rock", TreatmentTextTone.Incorrect));
            else if (entry.decisionKind == TreatmentDecisionKind.Decline)
                action.Add(new TreatmentTextPart("Declined patient", TreatmentTextTone.Incorrect));
            else if (entry.decisionKind == TreatmentDecisionKind.ReportToPolice)
            {
                string reason = entry.reportReason == PoliceReportReason.VampireInfected ? "Vampire infected" :
                    entry.reportReason == PoliceReportReason.ZombieInfected ? "Zombie infected" : "Unspecified";
                action.Add(new TreatmentTextPart("Reported to police (" + reason + ")",
                    correct ? TreatmentTextTone.Correct : TreatmentTextTone.Incorrect));
            }
            else
            {
                foreach (var item in entry.medicines)
                    if (item.quantity > 0)
                        action.Add(new TreatmentTextPart(item.name, police ? TreatmentTextTone.Incorrect :
                            requiredIds.Contains(item.id) ? TreatmentTextTone.Correct : TreatmentTextTone.Normal));
                if (entry.medicines.Count == 0 && (police || requiredIds.Count > 0))
                    action.Add(new TreatmentTextPart("No medicine given", TreatmentTextTone.Incorrect));
                if (!police)
                    foreach (var part in required)
                        if (part.Tone == TreatmentTextTone.Incorrect)
                            action.Add(new TreatmentTextPart("Missing: " + part.Text, TreatmentTextTone.Incorrect));
            }
            return new PatientTreatmentResult(entry, correct, police, required, action,
                clinicDirtPenalty, receptionRatingBonus, goldMultiplier, ghostPenalty);
        }

        private static bool IsCorrect(TreatmentJournal.Entry entry, AllSickness medical)
        {
            if (entry.sicknesses == null || entry.medicines == null)
                throw new InvalidOperationException("Treatment history is incomplete.");
            var diseases = new HashSet<string>(StringComparer.Ordinal);
            var definitions = new List<SicknessDefinition>();
            foreach (var item in entry.sicknesses)
            {
                var sickness = item != null ? medical.Find(item.id) : null;
                if (sickness == null || !diseases.Add(item.id))
                    throw new InvalidOperationException("Treatment references a missing or duplicate disease.");
                definitions.Add(sickness);
            }
            bool vampire = diseases.Contains(NpcMedicalConditions.Vampire), zombie = diseases.Contains(NpcMedicalConditions.Zombie);
            if (vampire && zombie) throw new InvalidOperationException("A patient cannot have both Vampire and Zombie Infection.");
            if (vampire || zombie)
            {
                var expected = vampire ? PoliceReportReason.VampireInfected : PoliceReportReason.ZombieInfected;
                return entry.decisionKind == TreatmentDecisionKind.ReportToPolice && entry.reportReason == expected &&
                    entry.reportedSicknessId == PoliceReportReasons.SicknessId(expected) && entry.medicines.Count == 0;
            }
            if (entry.decisionKind != TreatmentDecisionKind.GiveMedicine ||
                (!entry.hauntingCleared && definitions.Exists(x => x.Treatment == SicknessTreatment.RemoveGhosts))) return false;
            var required = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sickness in definitions)
            {
                if (sickness.Treatment == SicknessTreatment.RemoveGhosts) continue;
                if (sickness.Treatment != SicknessTreatment.RequiredPills || sickness.PillIds.Count == 0)
                    throw new InvalidOperationException("Missing or unsupported treatment recipe: " + sickness.DisplayName);
                var own = new HashSet<string>(StringComparer.Ordinal);
                foreach (var id in sickness.PillIds)
                {
                    if (medical.PillProfile.Find(id) == null || !own.Add(id))
                        throw new InvalidOperationException("Invalid medicine recipe: " + sickness.DisplayName);
                    required.Add(id);
                }
            }
            var provided = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in entry.medicines)
            {
                if (item == null || medical.PillProfile.Find(item.id) == null)
                    throw new InvalidOperationException("Treatment references a missing medicine.");
                if (item.quantity > 0) provided.Add(item.id);
            }
            // Each distinct required type needs one unit. Extra medicine is allowed by design.
            return required.IsSubsetOf(provided);
        }
    }
}
