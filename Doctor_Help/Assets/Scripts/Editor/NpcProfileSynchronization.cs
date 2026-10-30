using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DoctorHelp.Editor
{
    public static class NpcProfileSynchronization
    {
        public static string Refresh(NpcProfile profile, NpcCatalog catalog, bool removeMissing = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Dừng Play Mode trước khi cập nhật dữ liệu NPC.");
            if (profile == null) throw new InvalidOperationException("Chọn NPC Profile trước.");
            Undo.RecordObject(profile, "Refresh NPC Profile");
            int added = NpcProfileImporter.LoadNpcs(profile);
            int removed = 0;
            if (removeMissing)
            {
                var so = new SerializedObject(profile);
                var entries = so.FindProperty("npcs");
                for (int i = entries.arraySize - 1; i >= 0; i--)
                    if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue == null)
                    { entries.DeleteArrayElementAtIndex(i); removed++; }
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssetIfDirty(profile);
            }
            UpdateCatalog(profile, catalog, true);
            int missing = profile.Npcs.Count(n => n.Prefab == null);
            return $"Thêm {added} NPC; xóa {removed} bản ghi thiếu prefab; còn {missing} bản ghi thiếu prefab." +
                (catalog != null ? $" Danh sách spawn: {catalog.prefabs.Length} NPC hợp lệ." : " Chưa chọn danh sách spawn.");
        }

        public static void UpdateCatalog(NpcProfile profile, NpcCatalog catalog, bool recordUndo = false)
        {
            if (catalog == null) return;
            var live = profile.Npcs.Where(n => n != null && n.Prefab != null).Select(n => n.Prefab).Distinct().ToArray();
            if (recordUndo) Undo.RecordObject(catalog, "Refresh NPC Spawn Catalog");
            catalog.prefabs = live;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
        }

        public static void DrawButtons(NpcProfile profile, NpcCatalog catalog, Action<string, MessageType> report)
        {
            using (new EditorGUI.DisabledScope(profile == null || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Refresh / cập nhật NPC", GUILayout.Height(26))) Run(false);
                using (new EditorGUI.DisabledScope(profile == null || !profile.Npcs.Any(n => n.Prefab == null)))
                    if (GUILayout.Button("Xóa bản ghi mất prefab + cập nhật spawn")) Run(true);
            }
            void Run(bool removeMissing)
            {
                try { report(Refresh(profile, catalog, removeMissing), MessageType.Info); }
                catch (Exception error) { report(error.Message, MessageType.Error); }
            }
        }
    }
}
