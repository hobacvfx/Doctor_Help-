using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DoctorHelp.Editor
{
    internal static class MedicalDataInspector
    {
        public static void ReadOnlyId(SerializedProperty entry)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("id"), new GUIContent("ID"));
        }
        public static void AddEntry(SerializedProperty list)
        {
            list.InsertArrayElementAtIndex(list.arraySize);
            var entry = list.GetArrayElementAtIndex(list.arraySize - 1);
            entry.FindPropertyRelative("id").stringValue = Guid.NewGuid().ToString("N");
            entry.FindPropertyRelative("displayName").stringValue = "";
        }
        public static string Label(NamedDataEntry entry) =>
            string.IsNullOrWhiteSpace(entry.DisplayName) ? "(Chưa đặt tên)" : entry.DisplayName;

        public static void References(SerializedProperty list, IReadOnlyList<NamedDataEntry> catalog, string title)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            var used = new HashSet<string>();
            for (int i = 0; i < list.arraySize; i++)
            {
                var slot = list.GetArrayElementAtIndex(i);
                string current = slot.stringValue;
                var ids = new List<string> { "" };
                var labels = new List<string> { "— Chọn —" };
                if (catalog != null) foreach (var entry in catalog)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Id)) continue;
                    ids.Add(entry.Id);
                    labels.Add(Label(entry) + "  [" + entry.Id.Substring(0, Math.Min(6, entry.Id.Length)) + "]");
                }
                int selected = ids.IndexOf(current);
                bool missing = selected < 0;
                if (missing) { selected = ids.Count; ids.Add(current); labels.Add("Thiếu dữ liệu: " + current); }
                EditorGUILayout.BeginHorizontal();
                int chosen = EditorGUILayout.Popup("Slot " + (i + 1), selected, labels.ToArray());
                if (chosen != selected) slot.stringValue = ids[chosen];
                bool remove = GUILayout.Button("−", GUILayout.Width(28));
                EditorGUILayout.EndHorizontal();
                if (remove) { list.DeleteArrayElementAtIndex(i); break; }
                if (missing) EditorGUILayout.HelpBox("Mục được chọn đã bị xóa hoặc không thuộc profile nguồn. Hãy chọn lại hoặc xóa slot.", MessageType.Error);
                if (string.IsNullOrWhiteSpace(slot.stringValue)) EditorGUILayout.HelpBox("Slot chưa được chọn.", MessageType.Warning);
                else if (!used.Add(slot.stringValue)) EditorGUILayout.HelpBox("Mục này đã có trong danh sách. Mỗi loại chỉ cần một slot.", MessageType.Warning);
            }
            using (new EditorGUI.DisabledScope(catalog == null || catalog.Count == 0))
                if (GUILayout.Button("+ Thêm " + title))
                {
                    list.InsertArrayElementAtIndex(list.arraySize);
                    list.GetArrayElementAtIndex(list.arraySize - 1).stringValue = "";
                }
            if (catalog == null || catalog.Count == 0)
                EditorGUILayout.HelpBox("Thêm dữ liệu vào profile nguồn trước khi chọn slot.", MessageType.Info);
        }
    }

    public abstract class NamedMedicalProfileEditor : UnityEditor.Editor
    {
        protected abstract string ListProperty { get; }
        protected abstract string EntryLabel { get; }
        protected abstract int CountUsage(AllSickness data, string id);
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var list = serializedObject.FindProperty(ListProperty);
            EditorGUILayout.LabelField(EntryLabel + " (" + list.arraySize + ")", EditorStyles.boldLabel);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < list.arraySize; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                var name = entry.FindPropertyRelative("displayName");
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(name, new GUIContent("Tên"));
                bool remove = GUILayout.Button("−", GUILayout.Width(28));
                EditorGUILayout.EndHorizontal();
                MedicalDataInspector.ReadOnlyId(entry);
                if (string.IsNullOrWhiteSpace(name.stringValue)) EditorGUILayout.HelpBox("Hãy đặt tên cho mục này.", MessageType.Warning);
                else if (!names.Add(name.stringValue.Trim())) EditorGUILayout.HelpBox("Tên trùng; liên kết vẫn phân biệt bằng ID.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                if (remove)
                {
                    int count = 0;
                    string id = entry.FindPropertyRelative("id").stringValue;
                    foreach (string guid in AssetDatabase.FindAssets("t:AllSickness"))
                    {
                        var data = AssetDatabase.LoadAssetAtPath<AllSickness>(AssetDatabase.GUIDToAssetPath(guid));
                        if (data != null) count += CountUsage(data, id);
                    }
                    if (count == 0 || EditorUtility.DisplayDialog("Xóa " + EntryLabel,
                        $"Mục này đang được dùng trong {count} bệnh. Các slot liên quan sẽ báo thiếu dữ liệu cho đến khi bạn chọn lại hoặc xóa slot.", "Xóa", "Hủy"))
                        list.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
            if (GUILayout.Button("+ Thêm " + EntryLabel)) MedicalDataInspector.AddEntry(list);
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(SymptomProfile))]
    public sealed class SymptomProfileEditor : NamedMedicalProfileEditor
    {
        protected override string ListProperty => "symptoms";
        protected override string EntryLabel => "Triệu chứng";
        protected override int CountUsage(AllSickness data, string id)
        {
            if (data.SymptomProfile != target) return 0;
            int count = 0;
            foreach (var sickness in data.Sicknesses)
                if (sickness != null && new HashSet<string>(sickness.SymptomIds).Contains(id)) count++;
            return count;
        }
    }

    [CustomEditor(typeof(PillProfile))]
    public sealed class PillProfileEditor : NamedMedicalProfileEditor
    {
        protected override string ListProperty => "pills";
        protected override string EntryLabel => "Pill";
        protected override int CountUsage(AllSickness data, string id)
        {
            if (data.PillProfile != target) return 0;
            int count = 0;
            foreach (var sickness in data.Sicknesses)
                if (sickness != null && new HashSet<string>(sickness.PillIds).Contains(id)) count++;
            return count;
        }
    }

    [CustomEditor(typeof(AllSickness))]
    public sealed class AllSicknessEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("symptomProfile"), new GUIContent("Symptom Profile"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pillProfile"), new GUIContent("Pill Profile"));
            var symptoms = (SymptomProfile)serializedObject.FindProperty("symptomProfile").objectReferenceValue;
            var pills = (PillProfile)serializedObject.FindProperty("pillProfile").objectReferenceValue;
            if (symptoms == null || pills == null) EditorGUILayout.HelpBox("Gán cả hai profile nguồn để chọn triệu chứng và thuốc.", MessageType.Warning);
            EditorGUILayout.HelpBox("Bệnh dùng thuốc yêu cầu đủ TẤT CẢ mục Pill. Bệnh báo cảnh sát không dùng thuốc. Slot không phải số lượng/liều thuốc.", MessageType.Info);
            var list = serializedObject.FindProperty("sicknesses");
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < list.arraySize; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                var name = entry.FindPropertyRelative("displayName");
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded,
                    string.IsNullOrWhiteSpace(name.stringValue) ? "Bệnh chưa đặt tên" : name.stringValue, true);
                bool remove = GUILayout.Button("−", GUILayout.Width(28));
                EditorGUILayout.EndHorizontal();
                if (entry.isExpanded)
                {
                    EditorGUILayout.PropertyField(name, new GUIContent("Tên bệnh"));
                    MedicalDataInspector.ReadOnlyId(entry);
                    MedicalDataInspector.References(entry.FindPropertyRelative("symptomIds"), symptoms != null ? symptoms.Symptoms : null, "Triệu chứng");
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("treatment"), new GUIContent("Cách xử lý"));
                    bool police = entry.FindPropertyRelative("treatment").enumValueIndex == (int)SicknessTreatment.ReportToPolice;
                    if (police) EditorGUILayout.HelpBox("Bệnh này phải báo cảnh sát, không điều trị bằng thuốc.", MessageType.Info);
                    MedicalDataInspector.References(entry.FindPropertyRelative("pillIds"), pills != null ? pills.Pills : null, "Pill");
                    if (!police && entry.FindPropertyRelative("pillIds").arraySize == 0)
                        EditorGUILayout.HelpBox("Chưa cấu hình thuốc yêu cầu. Bệnh này chưa có công thức điều trị bằng thuốc.", MessageType.Warning);
                    if (police && entry.FindPropertyRelative("pillIds").arraySize > 0)
                        EditorGUILayout.HelpBox("Cách xử lý là báo cảnh sát; hãy xóa các slot thuốc để tránh dữ liệu mâu thuẫn.", MessageType.Warning);
                }
                if (string.IsNullOrWhiteSpace(name.stringValue)) EditorGUILayout.HelpBox("Hãy đặt tên bệnh.", MessageType.Warning);
                else if (!names.Add(name.stringValue.Trim())) EditorGUILayout.HelpBox("Tên bệnh trùng; ID vẫn độc lập.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                if (remove) { list.DeleteArrayElementAtIndex(i); break; }
            }
            if (GUILayout.Button("+ Thêm bệnh"))
            {
                MedicalDataInspector.AddEntry(list);
                var entry = list.GetArrayElementAtIndex(list.arraySize - 1);
                entry.FindPropertyRelative("symptomIds").ClearArray();
                entry.FindPropertyRelative("pillIds").ClearArray();
                entry.FindPropertyRelative("treatment").enumValueIndex = 0;
                entry.isExpanded = true;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
