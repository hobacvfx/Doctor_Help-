using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DoctorHelp.Editor
{
    public static class InitialSetupValidation
    {
        [MenuItem("Doctor Help/Validate Initial Setup")]
        public static void Run()
        {
            int sessionChecks = DailyProfileSessionValidation.Run();
            int flowChecks = DayFlowValidation.Run();
            int uiChecks = UiPrefabValidation.Run();
            int medicalChecks = MedicalDataValidation.Run();
            Require(PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android)
                == "com.doctorhelp.game", "Android package name");
            Require(PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait, "Portrait orientation");
            string directory = Path.Combine(Application.dataPath, "../Library/ProfileValidation", Guid.NewGuid().ToString("N"));
            var store = new PlayerProfileStore(directory);
            var profile = store.Load();
            Require(profile.money == 0 && profile.currentDay == 1 && profile.patientsKilled == 0 &&
                profile.patientsSaved == 0 && profile.medicines.Count == 0, "New profile defaults");
            profile.money = -75;
            profile.currentDay = 3;
            profile.patientsKilled = 2;
            profile.patientsSaved = 5;
            profile.medicines.Add(new MedicineStock { medicineId = "test_medicine", quantity = 4 });
            store.Save(profile);
            var loaded = new PlayerProfileStore(directory).Load();
            Require(loaded.money == -75 && loaded.currentDay == 3 && loaded.patientsKilled == 2 &&
                loaded.patientsSaved == 5 && loaded.medicines[0].quantity == 4, "Save/load all fields and debt");
            profile.money = 20;
            store.Save(profile);
            File.WriteAllText(store.SavePath, "broken JSON");
            var recoveryStore = new PlayerProfileStore(directory);
            var recovered = recoveryStore.Load();
            Require(recovered.money == -75, "Recover previous save from backup");
            recoveryStore.Save(recovered);
            Require(new PlayerProfileStore(directory).Load().money == -75, "Save after backup recovery");
            profile.medicines[0].quantity = -1;
            Require(!profile.IsValid(), "Reject negative stock");
            profile.medicines[0].quantity = 1;
            profile.medicines.Add(new MedicineStock { medicineId = "test_medicine", quantity = 1 });
            Require(!profile.IsValid(), "Reject duplicate medicine IDs");
            string futureJson = "{\"schemaVersion\":2}";
            File.WriteAllText(store.SavePath, futureJson);
            bool futureRejected = false;
            try { new PlayerProfileStore(directory).Load(); }
            catch (NotSupportedException) { futureRejected = true; }
            Require(futureRejected && File.ReadAllText(store.SavePath) == futureJson, "Preserve newer save version");
            File.WriteAllText(store.SavePath, "broken primary");
            File.WriteAllText(store.SavePath + ".bak", "broken backup");
            bool corruptionRejected = false;
            try { new PlayerProfileStore(directory).Load(); }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException) { corruptionRejected = true; }
            Require(corruptionRejected && File.ReadAllText(store.SavePath) == "broken primary",
                "Preserve files when both saves are corrupt");
            Require(EditorBuildSettings.scenes.Length >= 2 && EditorBuildSettings.scenes[0].enabled &&
                EditorBuildSettings.scenes[0].path == "Assets/Scenes/Home.unity" &&
                EditorBuildSettings.scenes[1].enabled && EditorBuildSettings.scenes[1].path == "Assets/Scenes/Office.unity",
                "Home must start before Office in builds");
            var catalog = Resources.Load<NpcCatalog>("NpcCatalog");
            Require(catalog != null && catalog.prefabs != null && catalog.prefabs.Length > 0 &&
                Array.TrueForAll(catalog.prefabs, prefab => prefab != null), "NPC catalog references");
            foreach (string name in new[] { "OpenButton", "GivePillsButton", "GiveButton", "PillOptionButton" })
            {
                var buttonPrefab = Resources.Load<GameObject>("UI/" + name);
                Require(buttonPrefab != null && buttonPrefab.GetComponent<UnityEngine.UI.Button>() != null,
                    "UI button prefab: " + name);
            }
            var popupPrefab = Resources.Load<GameObject>("UI/SelectPillPopup");
            Require(popupPrefab != null && popupPrefab.GetComponent<SelectPillPopup>() != null &&
                popupPrefab.GetComponent<SelectPillPopup>().IsConfigured, "Nested popup button references");
            Require(Resources.Load<GameObject>("UI/ResultsPopup") != null, "Results popup prefab");
            Debug.Log($"INITIAL_SETUP_VALIDATION_PASSED: {18 + sessionChecks + flowChecks + uiChecks + medicalChecks} checks. Test files: " + directory);
        }

        private static void Require(bool condition, string description)
        {
            if (!condition) throw new Exception("Initial setup validation failed: " + description);
        }
    }
}
