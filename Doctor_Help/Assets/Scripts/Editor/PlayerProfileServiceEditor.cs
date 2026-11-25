using UnityEditor;
using UnityEngine;

namespace DoctorHelp.Editor
{
    [CustomEditor(typeof(PlayerProfileService))]
    public sealed class PlayerProfileServiceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var profile = (PlayerProfileService)target;
            if (!Application.isPlaying || !profile.IsLoaded) return;
            EditorGUILayout.HelpBox("Working day only. Saved at day completion.", MessageType.Info);
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("testNpcSmellyBleed"),
                new GUIContent("Test NPC smelly/bleed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hauntedTest"),
                new GUIContent("Dirty test", "Adds Dirty on arrival when the patient has room for another disease."));
            serializedObject.ApplyModifiedProperties();
            int money = EditorGUILayout.IntField("Player Money", profile.Money);
            if (money != profile.Money) profile.SetMoney(money);
            int water = EditorGUILayout.IntField("Clean Water", profile.Water);
            if (water != profile.Water) profile.SetWater(water);
            int electricity = EditorGUILayout.IntField("Electricity", profile.Electricity);
            if (electricity != profile.Electricity) profile.SetElectricity(electricity);
            float dirt = EditorGUILayout.Slider("Player Dirt", profile.Dirt, 0f, PlayerProfileData.MaxDirt);
            if (dirt != profile.Dirt) profile.SetDirt(Mathf.Round(dirt * 2f) / 2f);
            float rating = EditorGUILayout.Slider("Rating", profile.Rating, 0f, PlayerProfileData.MaxRating);
            if (rating != profile.Rating) profile.SetRating(rating);
            profile.SetThrowRock(EditorGUILayout.Toggle("Throw Rock", profile.ThrowRock));
            profile.SetClean(EditorGUILayout.Toggle("Clean", profile.Clean));
            EditorGUILayout.LabelField("Day", profile.CurrentDay.ToString());
            EditorGUILayout.LabelField("Diff Progress", profile.DiffProgress.ToString());
            EditorGUILayout.LabelField("Success Streak", $"{profile.DiffStreakDays} days, {profile.DiffStreakIncorrectPatients} incorrect");
            EditorGUILayout.LabelField("Ghost Number", profile.GhostNumber.ToString());
            var history = profile.GetTreatmentSnapshot();
            EditorGUILayout.LabelField("Treatments in RAM", (history?.treatments.Count ?? 0).ToString());
        }
    }
}
