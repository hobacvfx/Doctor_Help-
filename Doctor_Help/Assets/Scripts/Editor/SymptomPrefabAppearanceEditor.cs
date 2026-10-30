using UnityEditor;
using UnityEngine;
namespace DoctorHelp.Editor
{
    [CustomEditor(typeof(SymptomPrefabAppearance))]
    public sealed class SymptomPrefabAppearanceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var bindings = serializedObject.FindProperty("bindings");
            bindings.arraySize = Mathf.Max(0, EditorGUILayout.IntField("Số liên kết", bindings.arraySize));
            var source = AssetDatabase.LoadAssetAtPath<SymptomProfile>("Assets/Data/Medical/SymptomProfile.asset");
            for (int i = 0; i < bindings.arraySize; i++)
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    var entry = bindings.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("prefab"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("position"));
                    var mapping = entry.FindPropertyRelative("symptomId");
                    if (source == null) { EditorGUILayout.PropertyField(mapping); continue; }
                    var names = new string[source.Symptoms.Count + 1]; names[0] = "Missing / unassigned: " + mapping.stringValue;
                    int current = 0;
                    for (int j = 0; j < source.Symptoms.Count; j++)
                    {
                        names[j + 1] = source.Symptoms[j].DisplayName;
                        if (source.Symptoms[j].Id == mapping.stringValue) current = j + 1;
                    }
                    int selected = EditorGUILayout.Popup("Triệu chứng", current, names);
                    if (selected != current) mapping.stringValue = selected == 0 ? "" : source.Symptoms[selected - 1].Id;
                }
            }
            EditorGUILayout.HelpBox("Mỗi triệu chứng hiện có sinh prefab tại đúng slot đã gắn. Các liên kết độc lập, không random hoặc đổi trạng thái khi đổi dụng cụ.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
