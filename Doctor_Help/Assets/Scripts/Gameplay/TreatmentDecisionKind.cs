namespace DoctorHelp
{
    public enum TreatmentDecisionKind { GiveMedicine = 0, ReportToPolice = 1, Decline = 2, HitPatientWithRock = 3 }
    public enum PoliceReportReason { None = 0, VampireInfected = 1, ZombieInfected = 2 }

    public static class PoliceReportReasons
    {
        public static string SicknessId(PoliceReportReason reason)
        {
            switch (reason)
            {
                case PoliceReportReason.VampireInfected: return NpcMedicalConditions.Vampire;
                case PoliceReportReason.ZombieInfected: return NpcMedicalConditions.Zombie;
                default: throw new System.ArgumentOutOfRangeException(nameof(reason), "Select one police report reason.");
            }
        }
    }
}
