using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DoctorHelp.Editor
{
    [CustomEditor(typeof(SymptomReferenceArt))]
    public sealed class SymptomReferenceArtEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var sourceProperty = serializedObject.FindProperty("source");
            EditorGUILayout.PropertyField(sourceProperty, new GUIContent("Symptom Profile"));
            var source = sourceProperty.objectReferenceValue as SymptomProfile;
            EditorGUILayout.HelpBox("Ảnh minh họa riêng cho sổ bác sĩ, liên kết bằng ID triệu chứng. Để Sprite trống với triệu chứng chỉ mô tả bằng chữ. Đây không phải prefab triệu chứng trên NPC.", MessageType.Info);
            var entries = serializedObject.FindProperty("illustrations");
            var used = new HashSet<string>();
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var id = entry.FindPropertyRelative("symptomId");
                var ids = new List<string> { "" }; var labels = new List<string> { "— Chọn triệu chứng —" };
                if (source != null) foreach (var symptom in source.Symptoms) { ids.Add(symptom.Id); labels.Add(symptom.DisplayName); }
                int selected = ids.IndexOf(id.stringValue);
                if (selected < 0) { selected = ids.Count; ids.Add(id.stringValue); labels.Add("Thiếu triệu chứng: " + id.stringValue); }
                EditorGUILayout.BeginVertical(EditorStyles.helpBox); EditorGUILayout.BeginHorizontal();
                id.stringValue = ids[EditorGUILayout.Popup("Triệu chứng", selected, labels.ToArray())];
                bool remove = GUILayout.Button("−", GUILayout.Width(26));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("sprite"), new GUIContent("Ảnh minh họa"));
                if (source == null || source.Find(id.stringValue) == null || !used.Add(id.stringValue))
                    EditorGUILayout.HelpBox("ID triệu chứng bị thiếu hoặc trùng; chọn lại để sửa. Không tự đổi sang triệu chứng khác.", MessageType.Error);
                EditorGUILayout.EndVertical();
                if (remove) { entries.DeleteArrayElementAtIndex(i); break; }
            }
            using (new EditorGUI.DisabledScope(source == null))
                if (GUILayout.Button("Thêm slot cho triệu chứng mới (giữ ảnh cũ)"))
                {
                    var existing = new HashSet<string>();
                    for (int i = 0; i < entries.arraySize; i++) existing.Add(entries.GetArrayElementAtIndex(i).FindPropertyRelative("symptomId").stringValue);
                    foreach (var symptom in source.Symptoms)
                    {
                        if (!existing.Add(symptom.Id)) continue;
                        var added = entries.GetArrayElementAtIndex(entries.arraySize++);
                        added.FindPropertyRelative("symptomId").stringValue = symptom.Id;
                        added.FindPropertyRelative("sprite").objectReferenceValue = null;
                    }
                }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
