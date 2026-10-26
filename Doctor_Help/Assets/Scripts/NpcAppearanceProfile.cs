using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Editor-time NPC look profile: pick Eye / Hair / Mouth / Outfit prefabs from a seed,
/// spawn them into attach points, then save the NPC as a nested prefab.
/// </summary>
[DisallowMultipleComponent]
public class NpcAppearanceProfile : MonoBehaviour
{
    public const string DefaultEyeFolder = "Assets/2D_Assets/eye";
    public const string DefaultHairFolder = "Assets/2D_Assets/Hair";
    public const string DefaultMouthFolder = "Assets/2D_Assets/Mouth";
    public const string DefaultOutfitFolder = "Assets/2D_Assets/Clothes";

    [Serializable]
    public class Slot
    {
        public string id;
        public Transform attachPoint;
        public string folderPath;
        public GameObject prefab;
        public bool lockOverride;
        public bool allowEmpty;
    }

    [SerializeField] int seed;
    [SerializeField] int confirmedSeed;
    [SerializeField] Slot[] slots = CreateDefaultSlots();

    public int Seed
    {
        get => seed;
        set => seed = value;
    }

    public int ConfirmedSeed => confirmedSeed;
    public Slot[] Slots => slots;

    public static Slot[] CreateDefaultSlots()
    {
        return new[]
        {
            new Slot { id = "Eye", folderPath = DefaultEyeFolder },
            new Slot { id = "Hair", folderPath = DefaultHairFolder, allowEmpty = true },
            new Slot { id = "Mouth", folderPath = DefaultMouthFolder },
            new Slot { id = "Outfit", folderPath = DefaultOutfitFolder }
        };
    }

    public bool TryReadSeedFromName(out int parsed)
    {
        return TryParseTrailingDigits(name, out parsed);
    }

    public void PullSeedFromName()
    {
        if (TryReadSeedFromName(out int parsed))
            seed = parsed;
        else
            Debug.LogWarning($"[{nameof(NpcAppearanceProfile)}] '{name}' does not end with digits.", this);
    }

    public void EnsureSlots()
    {
        Slot[] defaults = CreateDefaultSlots();
        if (slots == null || slots.Length == 0)
        {
            slots = defaults;
            return;
        }

        var merged = new System.Collections.Generic.List<Slot>(slots);
        foreach (Slot def in defaults)
        {
            bool exists = false;
            for (int i = 0; i < merged.Count; i++)
            {
                Slot existing = merged[i];
                if (existing != null &&
                    string.Equals(existing.id, def.id, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
                merged.Add(new Slot
                {
                    id = def.id,
                    folderPath = def.folderPath,
                    allowEmpty = def.allowEmpty
                });
        }

        if (merged.Count != slots.Length)
            slots = merged.ToArray();
    }

    public void BindMissingAttachPoints()
    {
        EnsureSlots();
        foreach (Slot slot in slots)
        {
            if (slot == null || string.IsNullOrEmpty(slot.id) || slot.attachPoint != null)
                continue;
            slot.attachPoint = FindDescendantByName(transform, slot.id);
        }
    }

    public static bool TryParseTrailingDigits(string objectName, out int value)
    {
        value = 0;
        if (string.IsNullOrEmpty(objectName))
            return false;

        int end = objectName.Length - 1;
        if (!char.IsDigit(objectName[end]))
            return false;

        int start = end;
        while (start > 0 && char.IsDigit(objectName[start - 1]))
            start--;

        return int.TryParse(objectName.Substring(start), out value);
    }

    public static int MixSeed(int baseSeed, string slotId)
    {
        unchecked
        {
            int hash = baseSeed;
            if (!string.IsNullOrEmpty(slotId))
            {
                for (int i = 0; i < slotId.Length; i++)
                    hash = hash * 31 + slotId[i];
            }

            return hash;
        }
    }

    public static int PickIndex(int baseSeed, string slotId, int optionCount)
    {
        if (optionCount <= 0)
            return -1;
        var rng = new System.Random(MixSeed(baseSeed, slotId));
        return rng.Next(optionCount);
    }

    static Transform FindDescendantByName(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                return child;

            Transform found = FindDescendantByName(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }

#if UNITY_EDITOR
    void Reset()
    {
        EnsureSlots();
        BindMissingAttachPoints();
        if (TryReadSeedFromName(out int parsed))
            seed = parsed;
    }

    void OnValidate()
    {
        EnsureSlots();
        BindMissingAttachPoints();
    }

    public void ConfirmAndApply()
    {
        BindMissingAttachPoints();
        confirmedSeed = seed;

        Undo.RegisterCompleteObjectUndo(this, "Apply NPC Appearance");

        foreach (Slot slot in slots)
            ApplySlot(slot);

        EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    void ApplySlot(Slot slot)
    {
        if (slot == null)
            return;
        if (slot.attachPoint == null)
        {
            Debug.LogWarning($"[{nameof(NpcAppearanceProfile)}] Slot '{slot.id}' has no attach point.", this);
            return;
        }

        GameObject chosen = slot.lockOverride
            ? ResolvePrefabAsset(slot.prefab)
            : ChoosePrefab(slot);

        slot.prefab = chosen;
        ReplaceChildren(slot.attachPoint, chosen);
    }

    GameObject ChoosePrefab(Slot slot)
    {
        GameObject[] options = LoadPrefabs(slot.folderPath);
        int extra = slot.allowEmpty ? 1 : 0;
        int optionCount = options.Length + extra;
        if (optionCount == 0)
        {
            Debug.LogWarning(
                $"[{nameof(NpcAppearanceProfile)}] Slot '{slot.id}' folder '{slot.folderPath}' has no prefabs.",
                this);
            return null;
        }

        int index = PickIndex(seed, slot.id, optionCount);
        if (slot.allowEmpty && index == options.Length)
            return null;
        if (index < 0 || index >= options.Length)
            return null;
        return options[index];
    }

    public static GameObject[] LoadPrefabs(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
            return Array.Empty<GameObject>();

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        var prefabs = new System.Collections.Generic.List<GameObject>(guids.Length);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                prefabs.Add(prefab);
        }

        prefabs.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return prefabs.ToArray();
    }

    static GameObject ResolvePrefabAsset(GameObject prefabOrInstance)
    {
        if (prefabOrInstance == null)
            return null;

        GameObject source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefabOrInstance);
        if (source != null)
            return source;

        source = PrefabUtility.GetCorrespondingObjectFromSource(prefabOrInstance);
        return source != null ? source : prefabOrInstance;
    }

    static void ReplaceChildren(Transform parent, GameObject prefab)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);

        if (prefab == null)
            return;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(instance, "Spawn NPC Part");
        instance.transform.SetLocalPositionAndRotation(
            prefab.transform.localPosition,
            prefab.transform.localRotation);
        instance.transform.localScale = prefab.transform.localScale;
    }
#endif
}
