using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NpcAppearanceProfile))]
public class NpcAppearanceProfileEditor : Editor
{
    SerializedProperty seed;
    SerializedProperty confirmedSeed;
    SerializedProperty slots;

    void OnEnable()
    {
        seed = serializedObject.FindProperty("seed");
        confirmedSeed = serializedObject.FindProperty("confirmedSeed");
        slots = serializedObject.FindProperty("slots");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var profile = (NpcAppearanceProfile)target;
        profile.EnsureSlots();

        EditorGUILayout.PropertyField(seed);
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(confirmedSeed, new GUIContent("Confirmed Seed"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Slots", EditorStyles.boldLabel);

        for (int i = 0; i < slots.arraySize; i++)
            DrawSlot(slots.GetArrayElementAtIndex(i));

        // Inspector edits must hit the live object before Apply reads lock/prefab.
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Lấy từ tên"))
        {
            Undo.RecordObject(profile, "Pull NPC Seed From Name");
            profile.PullSeedFromName();
            EditorUtility.SetDirty(profile);
            serializedObject.Update();
        }

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("Apply"))
            {
                serializedObject.ApplyModifiedProperties();
                profile.ConfirmAndApply();
                serializedObject.Update();
            }
        }

        EditorGUILayout.EndHorizontal();

        if (Application.isPlaying)
            EditorGUILayout.HelpBox("Apply chỉ chạy trong Editor, không chạy Play Mode.", MessageType.Info);
    }

    void DrawSlot(SerializedProperty slot)
    {
        SerializedProperty id = slot.FindPropertyRelative("id");
        SerializedProperty attachPoint = slot.FindPropertyRelative("attachPoint");
        SerializedProperty folderPath = slot.FindPropertyRelative("folderPath");
        SerializedProperty prefab = slot.FindPropertyRelative("prefab");
        SerializedProperty lockOverride = slot.FindPropertyRelative("lockOverride");
        SerializedProperty allowEmpty = slot.FindPropertyRelative("allowEmpty");

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(id, new GUIContent("Group"));
        EditorGUILayout.PropertyField(attachPoint);
        DrawFolderField(folderPath);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(prefab, new GUIContent("Spawned Prefab"));
        if (EditorGUI.EndChangeCheck())
            lockOverride.boolValue = true;

        EditorGUILayout.PropertyField(
            lockOverride,
            new GUIContent("Lock Override", "Giữ prefab trong slot này, không random lại theo seed."));
        EditorGUILayout.PropertyField(allowEmpty, new GUIContent("Allow Empty"));
        EditorGUILayout.EndVertical();
    }

    static void DrawFolderField(SerializedProperty folderPath)
    {
        DefaultAsset current = null;
        if (!string.IsNullOrEmpty(folderPath.stringValue))
            current = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath.stringValue);

        EditorGUI.BeginChangeCheck();
        var next = (DefaultAsset)EditorGUILayout.ObjectField(
            "Prefab Folder",
            current,
            typeof(DefaultAsset),
            false);
        if (!EditorGUI.EndChangeCheck())
            return;

        if (next == null)
        {
            folderPath.stringValue = string.Empty;
            return;
        }

        string path = AssetDatabase.GetAssetPath(next);
        if (AssetDatabase.IsValidFolder(path))
            folderPath.stringValue = path;
        else
            Debug.LogWarning($"'{path}' is not a folder.");
    }
}
