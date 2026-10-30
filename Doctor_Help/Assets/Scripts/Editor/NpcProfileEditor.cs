using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DoctorHelp.Editor
{
    [CustomEditor(typeof(NpcProfile))]
    public sealed class NpcProfileEditor : UnityEditor.Editor
    {
        private const int PageSize = 30;
        private NpcCatalog catalog;
        private void OnEnable() => catalog = AssetDatabase.LoadAssetAtPath<NpcCatalog>("Assets/Resources/NpcCatalog.asset");
        private string search = "";
        private string selectedId;
        private int page;
        private PreviewRenderUtility preview;
        private GameObject previewPrefab;
        private Bounds previewBounds;
        private string status;
        private MessageType statusType;

        private void OnDisable() => ClearPreview();
        private void ClearPreview()
        {
            preview?.Cleanup();
            preview = null;
            previewPrefab = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var profile = (NpcProfile)target;
            var folder = serializedObject.FindProperty("npcFolder");
            var folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder.stringValue);
            EditorGUI.BeginChangeCheck();
            var picked = (DefaultAsset)EditorGUILayout.ObjectField("NPC Folder", folderAsset, typeof(DefaultAsset), false);
            if (EditorGUI.EndChangeCheck()) folder.stringValue = picked != null ? AssetDatabase.GetAssetPath(picked) : "";
            EditorGUILayout.PropertyField(serializedObject.FindProperty("allSickness"), new GUIContent("Danh mục bệnh"));
            serializedObject.ApplyModifiedProperties();
            catalog = (NpcCatalog)EditorGUILayout.ObjectField("Danh sách spawn", catalog, typeof(NpcCatalog), false);
            NpcProfileSynchronization.DrawButtons(profile, catalog, (text, type) =>
            {
                status = text; statusType = type; ClearPreview(); serializedObject.Update();
            });
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, statusType);
            EditorGUILayout.HelpBox("Nạp cả thư mục con. NPC mới: tên theo prefab, không bệnh. Bấm slot để xem prefab và chỉnh thông tin. Refresh giữ các bản ghi mất prefab để bạn kiểm tra; dùng nút Xóa bản ghi mất prefab khi muốn dọn chúng.", MessageType.Info);

            var list = serializedObject.FindProperty("npcs");
            EditorGUI.BeginChangeCheck();
            search = EditorGUILayout.TextField("Tìm NPC", search);
            if (EditorGUI.EndChangeCheck()) page = 0;
            var matches = new List<int>();
            int selected = -1;
            for (int i = 0; i < list.arraySize; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("id").stringValue == selectedId) selected = i;
                var prefab = entry.FindPropertyRelative("prefab").objectReferenceValue;
                string label = entry.FindPropertyRelative("npcName").stringValue;
                if (string.IsNullOrEmpty(search) || label.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (prefab != null && prefab.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)) matches.Add(i);
            }
            int pages = Math.Max(1, (matches.Count + PageSize - 1) / PageSize);
            page = Mathf.Clamp(page, 0, pages - 1);
            EditorGUILayout.LabelField($"NPC: {list.arraySize}   |   Kết quả: {matches.Count}", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(page == 0)) if (GUILayout.Button("◀")) page--;
                GUILayout.Label($"Trang {page + 1}/{pages}", EditorStyles.centeredGreyMiniLabel);
                using (new EditorGUI.DisabledScope(page == pages - 1)) if (GUILayout.Button("▶")) page++;
            }
            for (int j = page * PageSize; j < Math.Min(matches.Count, (page + 1) * PageSize); j++)
            {
                int index = matches[j];
                var entry = list.GetArrayElementAtIndex(index);
                bool missing = entry.FindPropertyRelative("prefab").objectReferenceValue == null;
                string label = entry.FindPropertyRelative("npcName").stringValue;
                if (GUILayout.Toggle(selected == index, $"{index + 1}. {label}" + (missing ? "  [Missing prefab]" : ""), "Button") && selected != index)
                {
                    selectedId = entry.FindPropertyRelative("id").stringValue;
                    selected = index;
                    ClearPreview();
                }
            }
            if (selected >= 0) DrawSelected(list.GetArrayElementAtIndex(selected), profile);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSelected(SerializedProperty entry, NpcProfile profile)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Thông tin NPC", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("id"), new GUIContent("ID"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("prefab"), new GUIContent("Prefab"));
            }
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("npcName"), new GUIContent("Tên NPC"));
            var dialogue = entry.FindPropertyRelative("dialogueAsset");
            EditorGUILayout.PropertyField(dialogue, new GUIContent("Asset hội thoại"));
            if (dialogue.objectReferenceValue == null)
            {
                if (GUILayout.Button("Tạo hội thoại mới"))
                {
                    string path = EditorUtility.SaveFilePanelInProject("Tạo hội thoại NPC", entry.FindPropertyRelative("npcName").stringValue + "_Dialogue", "asset", "Chọn nơi lưu asset hội thoại.", "Assets/Data/NPC");
                    if (!string.IsNullOrEmpty(path))
                    {
                        path = AssetDatabase.GenerateUniqueAssetPath(path);
                        var created = CreateInstance<NpcDialogueAsset>();
                        AssetDatabase.CreateAsset(created, path);
                        dialogue.objectReferenceValue = created;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }
            if (dialogue.objectReferenceValue is NpcDialogueAsset conversation)
            {
                using (var content = new SerializedObject(conversation))
                {
                    content.Update();
                    EditorGUILayout.PropertyField(content.FindProperty("content"), new GUIContent("Nội dung (mỗi dòng một lượt)"));
                    content.ApplyModifiedProperties();
                }
                if (GUILayout.Button("Chọn asset hội thoại"))
                { Selection.activeObject = conversation; EditorGUIUtility.PingObject(conversation); }
            }
            EditorGUILayout.HelpBox("Chỉnh nội dung ngay tại đây. Không gán asset hoặc nội dung trống thì NPC không nói.", MessageType.Info);
            var sickness = entry.FindPropertyRelative("sicknessId");
            var ids = new List<string> { "" };
            var names = new List<string> { "Không bệnh" };
            if (profile.AllSickness != null) foreach (var disease in profile.AllSickness.Sicknesses)
            {
                if (disease == null || string.IsNullOrEmpty(disease.Id)) continue;
                ids.Add(disease.Id);
                names.Add(disease.DisplayName);
            }
            int current = ids.IndexOf(sickness.stringValue);
            if (current < 0)
            {
                current = ids.Count;
                ids.Add(sickness.stringValue);
                names.Add("Thiếu bệnh: " + sickness.stringValue);
                EditorGUILayout.HelpBox("Bệnh không còn trong danh mục nguồn. Liên kết được giữ nguyên; hãy chọn lại bệnh.", MessageType.Error);
            }
            int chosen = EditorGUILayout.Popup("Bệnh", current, names.ToArray());
            if (chosen != current) sickness.stringValue = ids[chosen];
            if (profile.AllSickness == null) EditorGUILayout.HelpBox("Gán AllSickness để chọn bệnh.", MessageType.Warning);
            var prefab = entry.FindPropertyRelative("prefab").objectReferenceValue as GameObject;
            if (prefab == null)
            {
                ClearPreview();
                EditorGUILayout.HelpBox("Prefab đã bị xóa hoặc bị thiếu. Khôi phục asset cùng file .meta rồi Load NPC để nối lại đúng ID.", MessageType.Error);
                return;
            }
            if (GUILayout.Button("Mở prefab NPC")) AssetDatabase.OpenAsset(prefab);
            if (preview == null || previewPrefab != prefab)
            {
                ClearPreview();
                preview = new PreviewRenderUtility();
                previewPrefab = prefab;
                var instance = Instantiate(prefab);
                instance.hideFlags = HideFlags.HideAndDontSave;
                preview.AddSingleGO(instance);
                bool found = false;
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                    if (!found) { previewBounds = renderer.bounds; found = true; }
                    else previewBounds.Encapsulate(renderer.bounds);
                    // This preview has no X-ray gesture; do not edit shared materials.
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    block.SetFloat("_Hold", 0f);
                    renderer.SetPropertyBlock(block);
                }
                if (!found) previewBounds = new Bounds(instance.transform.position, Vector3.one);
            }
            Rect rect = GUILayoutUtility.GetRect(100, 320, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                preview.BeginPreview(rect, EditorStyles.helpBox);
                var camera = preview.camera;
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(0.1f, Mathf.Max(previewBounds.extents.y,
                    previewBounds.extents.x / Mathf.Max(0.1f, rect.width / rect.height)) * 1.1f);
                camera.transform.position = previewBounds.center + Vector3.back * (previewBounds.extents.z + 10f);
                camera.transform.rotation = Quaternion.identity;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = previewBounds.size.z + 20f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.23f, 0.23f, 0.23f);
                preview.Render(true);
                GUI.DrawTexture(rect, preview.EndPreview(), ScaleMode.StretchToFill, false);
            }
        }
    }
}




