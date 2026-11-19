namespace DoctorHelp
{
    internal enum MainObjective
    {
        ClearFirstDay = 0,
        MakeGold = 1,
        UpgradeReceptionist = 2,
        LeadRankings = 3,
        Completed = 4
    }

    internal static class MainObjectives
    {
        internal static void Advance(PlayerProfileData profile, int receptionistLevel, ClinicRankingsCatalog rankings)
        {
            if (profile.mainObjective == MainObjective.ClearFirstDay && profile.currentDay >= 2)
                profile.mainObjective = MainObjective.MakeGold;
            if (profile.mainObjective == MainObjective.MakeGold && profile.money >= 1200)
                profile.mainObjective = MainObjective.UpgradeReceptionist;
            if (profile.mainObjective == MainObjective.UpgradeReceptionist && receptionistLevel >= 3)
                profile.mainObjective = MainObjective.LeadRankings;
            if (profile.mainObjective == MainObjective.LeadRankings && rankings != null &&
                rankings.IsPlayerFirst(profile.currentDay, profile.money))
                profile.mainObjective = MainObjective.Completed;
        }
    }
}
