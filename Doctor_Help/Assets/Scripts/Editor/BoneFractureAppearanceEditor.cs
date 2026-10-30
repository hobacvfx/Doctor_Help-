using UnityEditor;
using UnityEngine;
namespace DoctorHelp.Editor
{
    [CustomEditor(typeof(BoneFractureAppearance))]
    public sealed class BoneFractureAppearanceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("boneRenderer"));
            var mapping = serializedObject.FindProperty("boneFractureSymptomId");
            var source = AssetDatabase.LoadAssetAtPath<SymptomProfile>("Assets/Data/Medical/SymptomProfile.asset");
            if (source == null) EditorGUILayout.PropertyField(mapping);
            else
            {
                var names = new string[source.Symptoms.Count + 1];
                names[0] = "Missing / unassigned: " + mapping.stringValue;
                int current = 0;
                for (int i = 0; i < source.Symptoms.Count; i++)
                {
                    names[i + 1] = source.Symptoms[i].DisplayName;
                    if (source.Symptoms[i].Id == mapping.stringValue) current = i + 1;
                }
                int selected = EditorGUILayout.Popup("Bone fracture symptom", current, names);
                if (selected != current) mapping.stringValue = selected == 0 ? "" : source.Symptoms[selected - 1].Id;
            }
            EditorGUILayout.HelpBox("Each consultation: fracture enables BoneCrack and samples X/Y in 0–1 once. Healthy patients disable it. Renderer overrides preserve shared materials and X-ray properties.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
