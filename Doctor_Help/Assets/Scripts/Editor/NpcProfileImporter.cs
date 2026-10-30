using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DoctorHelp.Editor
{
    public static class NpcProfileImporter
    {
        public static int LoadNpcs(NpcProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (!AssetDatabase.IsValidFolder(profile.NpcFolder) ||
                !profile.NpcFolder.StartsWith("Assets/", StringComparison.Ordinal))
                throw new InvalidOperationException("Chọn thư mục NPC hợp lệ bên trong Assets trước khi Load NPC.");

            var serialized = new SerializedObject(profile);
            var list = serialized.FindProperty("npcs");
            var existing = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < list.arraySize; i++)
            {
                string id = list.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue;
                if (string.IsNullOrEmpty(id) || !existing.TryAdd(id, i))
                    throw new InvalidOperationException("Profile có ID NPC rỗng hoặc trùng. Hãy sửa dữ liệu trước khi nạp.");
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { profile.NpcFolder });
            Array.Sort(guids, (a, b) => string.CompareOrdinal(
                AssetDatabase.GUIDToAssetPath(a), AssetDatabase.GUIDToAssetPath(b)));
            int added = 0;
            foreach (string guid in guids)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab == null) continue;
                if (existing.TryGetValue(guid, out int index))
                {
                    list.GetArrayElementAtIndex(index).FindPropertyRelative("prefab").objectReferenceValue = prefab;
                    continue;
                }
                int next = list.arraySize++;
                var entry = list.GetArrayElementAtIndex(next);
                entry.FindPropertyRelative("id").stringValue = guid;
                entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                entry.FindPropertyRelative("npcName").stringValue = prefab.name;
                    entry.FindPropertyRelative("dialogueAsset").objectReferenceValue = null;
                entry.FindPropertyRelative("sicknessId").stringValue = "";
                existing.Add(guid, next);
                added++;
            }
            // Missing prefabs stay visible; reloading never deletes authored information.
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(profile);
            return added;
        }
    }
}




