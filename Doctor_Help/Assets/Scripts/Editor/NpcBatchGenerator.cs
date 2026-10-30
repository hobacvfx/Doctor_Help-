using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DoctorHelp.Editor
{
    // Authoring operation: creates independent NPC prefabs while preserving nested appearance parts.
    public static class NpcBatchGenerator
    {
        public static IReadOnlyList<GameObject> Generate(GameObject template, NpcProfile profile,
            NpcCatalog catalog, int firstSeed, int count, bool keepLockedSlots = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Dừng Play Mode trước khi tạo NPC.");
            if (template == null || profile == null || profile.AllSickness == null)
                throw new InvalidOperationException("Cần prefab mẫu, NPC Profile và danh mục bệnh.");
            if (firstSeed < 0 || count < 1 || (long)firstSeed + count - 1 > int.MaxValue)
                throw new InvalidOperationException("Seed/số lượng không hợp lệ hoặc vượt phạm vi số nguyên.");
            string templatePath = AssetDatabase.GetAssetPath(template);
            if (!templatePath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
                template.GetComponent<NpcAppearanceProfile>() == null ||
                template.GetComponent<PatientExaminationProfile>()?.Heart == null)
                throw new InvalidOperationException("Prefab mẫu cần NpcAppearanceProfile và Heart đã liên kết.");
            string folder = profile.NpcFolder.TrimEnd('/');
            if (!folder.StartsWith("Assets/", StringComparison.Ordinal) || !AssetDatabase.IsValidFolder(folder))
                throw new InvalidOperationException("NPC Folder trong profile phải là thư mục hợp lệ bên trong Assets.");
            var sicknesses = profile.AllSickness.Sicknesses;
            var sicknessIds = new HashSet<string>();
            foreach (var sickness in sicknesses)
                if (sickness == null || string.IsNullOrWhiteSpace(sickness.Id) || !sicknessIds.Add(sickness.Id))
                    throw new InvalidOperationException("Danh mục bệnh có mục rỗng hoặc ID trùng/không hợp lệ.");
            if (sicknessIds.Count == 0) throw new InvalidOperationException("Danh mục bệnh chưa có bệnh để chọn.");
            var knownIds = new HashSet<string>();
            var knownPrefabs = new HashSet<GameObject>();
            foreach (var npc in profile.Npcs)
                if (npc == null || string.IsNullOrWhiteSpace(npc.Id) || !knownIds.Add(npc.Id) ||
                    (npc.Prefab != null && !knownPrefabs.Add(npc.Prefab)))
                    throw new InvalidOperationException("NPC Profile có ID hoặc prefab trùng/không hợp lệ.");
            for (int i = 0; i < count; i++)
            {
                string path = $"{folder}/NPC_{firstSeed + i}.prefab";
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)) || System.IO.File.Exists(path) || System.IO.File.Exists(path + ".meta"))
                    throw new InvalidOperationException($"Đã có {path}. Chọn seed khác; tool không ghi đè NPC cũ.");
            }

            string profileBefore = EditorJsonUtility.ToJson(profile);
            string catalogBefore = catalog != null ? EditorJsonUtility.ToJson(catalog) : null;
            var createdPaths = new List<string>();
            var generated = new List<GameObject>();
            var random = new System.Random();
            bool committed = false;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    int seed = firstSeed + i;
                    if (EditorUtility.DisplayCancelableProgressBar("Tạo NPC", $"NPC_{seed} ({i + 1}/{count})", (float)i / count))
                        throw new OperationCanceledException("Đã hủy tạo NPC; các NPC mới trong đợt này được dọn.");
                    var root = PrefabUtility.LoadPrefabContents(templatePath);
                    try
                    {
                        root.name = $"NPC_{seed}";
                        var appearance = root.GetComponent<NpcAppearanceProfile>();
                        appearance.Seed = seed;
                        appearance.BindMissingAttachPoints();
                        foreach (var slot in appearance.Slots)
                        {
                            if (slot == null || slot.attachPoint == null)
                                throw new InvalidOperationException("Mẫu thiếu attach point ngoại hình.");
                            if (!keepLockedSlots) slot.lockOverride = false;
                            if (!slot.lockOverride && NpcAppearanceProfile.LoadPrefabs(slot.folderPath).Length == 0 && !slot.allowEmpty)
                                throw new InvalidOperationException($"Slot {slot.id} không có prefab để tạo.");
                        }
                        appearance.ConfirmAndApply(false);
                        string path = $"{folder}/NPC_{seed}.prefab";
                        createdPaths.Add(path);
                        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                        if (!success || prefab == null) throw new InvalidOperationException("Không lưu được " + path);
                        generated.Add(prefab);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
                var serialized = new SerializedObject(profile);
                var entries = serialized.FindProperty("npcs");
                foreach (var prefab in generated)
                {
                    var entry = entries.GetArrayElementAtIndex(entries.arraySize++);
                    entry.FindPropertyRelative("id").stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
                    entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                    entry.FindPropertyRelative("npcName").stringValue = prefab.name;
                    entry.FindPropertyRelative("sicknessId").stringValue = sicknesses[random.Next(sicknesses.Count)].Id;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (catalog != null)
                {
                    catalog.prefabs = (catalog.prefabs ?? Array.Empty<GameObject>()).Concat(generated).Distinct().ToArray();
                    EditorUtility.SetDirty(catalog);
                    AssetDatabase.SaveAssetIfDirty(catalog);
                }
                AssetDatabase.SaveAssetIfDirty(profile);
                committed = true;
                return generated;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!committed)
                {
                    EditorJsonUtility.FromJsonOverwrite(profileBefore, profile);
                    EditorUtility.SetDirty(profile); AssetDatabase.SaveAssetIfDirty(profile);
                    if (catalog != null)
                    {
                        EditorJsonUtility.FromJsonOverwrite(catalogBefore, catalog);
                        EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssetIfDirty(catalog);
                    }
                    foreach (string path in createdPaths)
                        if (!AssetDatabase.DeleteAsset(path) && System.IO.File.Exists(path))
                            Debug.LogError("Không dọn được prefab sau khi hủy/lỗi: " + path);
                }
            }
        }
    }
}

