using System;

namespace DoctorHelp.Editor
{
    // Pure C# tests: no scene, real save folder, or Unity runtime is required.
    public static class DailyProfileSessionValidation
    {
        public static int Run()
        {
            var disk = new PlayerProfileData();
            var session = new PlayerProfileSession(disk, value => disk = value.Copy());
            session.WorkingProfile.money = 125;
            session.WorkingProfile.patientsSaved = 2;
            session.WorkingProfile.patientsKilled = 1;
            session.WorkingProfile.medicines.Add(new MedicineStock { medicineId = "pill_a", quantity = 3 });
            Require(disk.money == 0 && disk.currentDay == 1 && disk.medicines.Count == 0,
                "Mid-day changes must not change the checkpoint");
            var restarted = new PlayerProfileSession(disk, value => disk = value.Copy());
            Require(restarted.WorkingProfile.currentDay == 1 && restarted.WorkingProfile.money == 0,
                "Restart must discard unfinished progress and repeat day 1");
            session.CompleteDay();
            Require(disk.currentDay == 2 && disk.money == 125 && disk.patientsSaved == 2 &&
                disk.patientsKilled == 1 && disk.medicines[0].quantity == 3,
                "End of day must persist all values and next day");
            restarted = new PlayerProfileSession(disk, value => disk = value.Copy());
            Require(restarted.WorkingProfile.currentDay == 2 && restarted.WorkingProfile.money == 125,
                "Restart after day 1 must begin day 2");
            restarted.WorkingProfile.money = -50;
            restarted.WorkingProfile.medicines[0].quantity = 0;
            Require(disk.money == 125 && disk.medicines[0].quantity == 3,
                "Working data must not share medicine entries with the checkpoint");
            bool failSave = true;
            var retry = new PlayerProfileSession(disk, value =>
            {
                if (failSave) throw new InvalidOperationException("Simulated disk failure");
                disk = value.Copy();
            });
            bool failed = false;
            try { retry.CompleteDay(); }
            catch (InvalidOperationException) { failed = true; }
            Require(failed && disk.currentDay == 2 && retry.WorkingProfile.currentDay == 2,
                "Failed save must not advance the day");
            failSave = false;
            retry.CompleteDay();
            Require(disk.currentDay == 3 && retry.WorkingProfile.currentDay == 3,
                "Retry must advance exactly once");
            retry.WorkingProfile.money = -10;
            retry.CompleteDay();
            Require(disk.currentDay == 4 && disk.money == -10, "Subsequent days and debt must persist");
            return 8;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Daily profile validation failed: " + message);
        }
    }
}
