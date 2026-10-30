using UnityEditor;
using UnityEngine;
namespace DoctorHelp.Editor
{
    [CustomEditor(typeof(OrganAbnormalityAppearance))]
    public sealed class OrganAbnormalityAppearanceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "organSymptomId");
            var mapping = serializedObject.FindProperty("organSymptomId");
            var source = AssetDatabase.LoadAssetAtPath<SymptomProfile>("Assets/Data/Medical/SymptomProfile.asset");
            if (source == null) EditorGUILayout.PropertyField(mapping);
            else
            {
                var names = new string[source.Symptoms.Count + 1]; names[0] = "Missing / unassigned: " + mapping.stringValue;
                int current = 0;
                for (int i = 0; i < source.Symptoms.Count; i++)
                {
                    names[i + 1] = source.Symptoms[i].DisplayName;
                    if (source.Symptoms[i].Id == mapping.stringValue) current = i + 1;
                }
                int selected = EditorGUILayout.Popup("Organ symptom", current, names);
                if (selected != current) mapping.stringValue = selected == 0 ? "" : source.Symptoms[selected - 1].Id;
            }
            EditorGUILayout.HelpBox("One random organ per consultation, placed at its paired anchor. Reinitializing replaces only the generated organ. Prefab layers, materials and scale are preserved.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }
    }
}


