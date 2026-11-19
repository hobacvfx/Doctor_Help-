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
        [SerializeField] private List<string> sicknessIds = new List<string>();
        [SerializeField] private NpcDialogueAsset dialogueAsset;
        [SerializeField, Range(1, 4)] private int difficulty = 1;

        // The prefab asset GUID is independent of its name and folder location.
        public string Id => id;
        public GameObject Prefab => prefab;
        public string NpcName => npcName;
        public IReadOnlyList<string> SicknessIds => sicknessIds.AsReadOnly();
        public NpcDialogueAsset Dialogue => dialogueAsset;
        public bool HasSickness => sicknessIds.Count > 0;
        public int Difficulty => sicknessIds.Count;
        internal int HauntingDifficulty => Mathf.Clamp(difficulty, 1, 4);
        internal bool RequiresPoliceReport => sicknessIds.Contains(NpcMedicalConditions.Vampire) ||
            sicknessIds.Contains(NpcMedicalConditions.Zombie);
        internal bool IsHauntedOnly => sicknessIds.Count == 1 && sicknessIds[0] == NpcMedicalConditions.Haunted;
        internal bool HasBrainRot => sicknessIds.Contains(NpcMedicalConditions.BrainRot);

        internal NpcDefinition ForConsultation(bool forceHaunted)
        {
            if (!forceHaunted || sicknessIds.Contains(NpcMedicalConditions.Haunted) ||
                Difficulty >= DifficultyProfile.MaxDifficulty) return this;
            var consultation = (NpcDefinition)MemberwiseClone();
            consultation.sicknessIds = new List<string>(sicknessIds) { NpcMedicalConditions.Haunted };
            return consultation;
        }
    }

    [CreateAssetMenu(fileName = "NpcProfile", menuName = "Doctor Help/Data/NPC Profile")]
    public sealed class NpcProfile : ScriptableObject
    {
        [SerializeField] private string npcFolder = "Assets/2D_Assets/NPC";
        [SerializeField] private AllSickness allSickness;
        [SerializeField, Range(0, 1)] private float secondSicknessChance = 0.3f;
        [SerializeField] private List<NpcDefinition> npcs = new List<NpcDefinition>();

        public string NpcFolder => npcFolder;
        public AllSickness AllSickness => allSickness;
        public float SecondSicknessChance => secondSicknessChance;
        public IReadOnlyList<NpcDefinition> Npcs => npcs.AsReadOnly();
        public NpcDefinition FindByPrefab(GameObject prefab) => prefab == null
            ? null : npcs.Find(npc => npc != null && npc.Prefab == prefab);
        public NpcDefinition Find(string id) => string.IsNullOrEmpty(id)
            ? null : npcs.Find(npc => npc != null && npc.Id == id);
    }
}





