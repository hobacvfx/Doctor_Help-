using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{

    [Serializable]
    public sealed class NpcDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private GameObject prefab;
        [SerializeField] private string npcName;
        [SerializeField] private string sicknessId;

        // The prefab asset GUID is independent of its name and folder location.
        public string Id => id;
        public GameObject Prefab => prefab;
        public string NpcName => npcName;
        public string SicknessId => sicknessId;
        public bool HasSickness => !string.IsNullOrEmpty(sicknessId);
    }

    [CreateAssetMenu(fileName = "NpcProfile", menuName = "Doctor Help/Data/NPC Profile")]
    public sealed class NpcProfile : ScriptableObject
    {
        [SerializeField] private string npcFolder = "Assets/2D_Assets/NPC";
        [SerializeField] private AllSickness allSickness;
        [SerializeField] private List<NpcDefinition> npcs = new List<NpcDefinition>();

        public string NpcFolder => npcFolder;
        public AllSickness AllSickness => allSickness;
        public IReadOnlyList<NpcDefinition> Npcs => npcs.AsReadOnly();
        public NpcDefinition FindByPrefab(GameObject prefab) => prefab == null
            ? null : npcs.Find(npc => npc != null && npc.Prefab == prefab);
        public NpcDefinition Find(string id) => string.IsNullOrEmpty(id)
            ? null : npcs.Find(npc => npc != null && npc.Id == id);
    }
}


