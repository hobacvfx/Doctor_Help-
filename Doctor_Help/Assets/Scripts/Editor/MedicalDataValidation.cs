using System;
using UnityEditor;
using UnityEngine;

namespace DoctorHelp.Editor
{
    public static class MedicalDataValidation
    {
        public static int Run()
        {
            int checks = 0;
            var symptoms = ScriptableObject.CreateInstance<SymptomProfile>();
            var pills = ScriptableObject.CreateInstance<PillProfile>();
            var diseases = ScriptableObject.CreateInstance<AllSickness>();
            var copy = ScriptableObject.CreateInstance<PillProfile>();
            try
            {
                string symptom = Add(symptoms, "symptoms", "Test symptom");
                string pillA = Add(pills, "pills", "Test A");
                string pillB = Add(pills, "pills", "Test B");
                string sickness = Add(diseases, "sicknesses", "Test sickness");
                var data = new SerializedObject(diseases);
                data.FindProperty("symptomProfile").objectReferenceValue = symptoms;
                data.FindProperty("pillProfile").objectReferenceValue = pills;
                var entry = data.FindProperty("sicknesses").GetArrayElementAtIndex(0);
                SetIds(entry.FindPropertyRelative("symptomIds"), symptom);
                SetIds(entry.FindPropertyRelative("pillIds"), pillA, pillB);
                data.ApplyModifiedPropertiesWithoutUndo();
                Check(diseases.HasRequiredPills(sickness, new[] { pillA, pillB }), "All required pills accepted", ref checks);
                Check(!diseases.HasRequiredPills(sickness, new[] { pillA }), "Partial treatment rejected", ref checks);
                Check(!diseases.HasRequiredPills(sickness, new string[0]), "Empty treatment rejected", ref checks);
                Check(!diseases.HasRequiredPills("missing", new[] { pillA, pillB }), "Unknown disease rejected", ref checks);
                Check(!diseases.HasRequiredPills(sickness, null), "Null treatment rejected", ref checks);
                var source = new SerializedObject(pills);
                source.FindProperty("pills").GetArrayElementAtIndex(0).FindPropertyRelative("displayName").stringValue = "Renamed A";
                source.ApplyModifiedPropertiesWithoutUndo();
                Check(pills.Find(pillA).DisplayName == "Renamed A" && diseases.HasRequiredPills(sickness, new[] { pillA, pillB }),
                    "Rename preserves references", ref checks);
                source.Update();
                source.FindProperty("pills").MoveArrayElement(0, 1);
                source.ApplyModifiedPropertiesWithoutUndo();
                Check(diseases.HasRequiredPills(sickness, new[] { pillB, pillA }), "Catalog ordering does not affect references", ref checks);
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(pills), copy);
                Check(copy.Find(pillA).DisplayName == "Renamed A", "Serialized IDs/names survive round trip", ref checks);
                source.Update();
                source.FindProperty("pills").DeleteArrayElementAtIndex(0); // Remove B after reorder.
                source.ApplyModifiedPropertiesWithoutUndo();
                Check(pills.Find(pillB) == null && !diseases.HasRequiredPills(sickness, new[] { pillA, pillB }),
                    "Deleted source does not silently cure or retarget the disease", ref checks);
                Check(diseases.Find(sickness).PillIds[1] == pillB, "Missing reference remains visible for repair", ref checks);
                data.Update();
                SetIds(data.FindProperty("sicknesses").GetArrayElementAtIndex(0).FindPropertyRelative("pillIds"), pillA, pillA);
                data.ApplyModifiedPropertiesWithoutUndo();
                Check(!diseases.HasRequiredPills(sickness, new[] { pillA }), "Duplicate required IDs rejected", ref checks);
                data.Update();
                SetIds(data.FindProperty("sicknesses").GetArrayElementAtIndex(0).FindPropertyRelative("pillIds"));
                data.ApplyModifiedPropertiesWithoutUndo();
                Check(!diseases.HasRequiredPills(sickness, new[] { pillA }), "Unconfigured recipe is not an automatic cure", ref checks);
                Check(diseases.Find(sickness).SymptomIds[0] == symptom && symptoms.Find(symptom) != null,
                    "Symptom references remain independent of pill changes", ref checks);
                data.Update();
                var police = data.FindProperty("sicknesses").GetArrayElementAtIndex(0);
                police.FindPropertyRelative("treatment").enumValueIndex = (int)SicknessTreatment.ReportToPolice;
                SetIds(police.FindPropertyRelative("pillIds"), pillA);
                data.ApplyModifiedPropertiesWithoutUndo();
                Check(!diseases.HasRequiredPills(sickness, new[] { pillA }), "Police cases cannot be cured by pills", ref checks);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(diseases);
                UnityEngine.Object.DestroyImmediate(pills);
                UnityEngine.Object.DestroyImmediate(symptoms);
                UnityEngine.Object.DestroyImmediate(copy);
            }
            Debug.Log($"MEDICAL_DATA_VALIDATION_PASSED: {checks} checks; no production data modified.");
            return checks;
        }

        private static string Add(UnityEngine.Object target, string listName, string name)
        {
            var serialized = new SerializedObject(target);
            var list = serialized.FindProperty(listName);
            list.InsertArrayElementAtIndex(list.arraySize);
            var entry = list.GetArrayElementAtIndex(list.arraySize - 1);
            string id = Guid.NewGuid().ToString("N");
            entry.FindPropertyRelative("id").stringValue = id;
            entry.FindPropertyRelative("displayName").stringValue = name;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return id;
        }
        private static void SetIds(SerializedProperty list, params string[] ids)
        {
            list.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++) list.GetArrayElementAtIndex(i).stringValue = ids[i];
        }
        private static void Check(bool ok, string description, ref int count)
        {
            if (!ok) throw new Exception("Medical data validation failed: " + description);
            count++;
        }
    }
}
