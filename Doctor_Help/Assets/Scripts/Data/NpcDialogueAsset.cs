using UnityEngine;
namespace DoctorHelp
{
    [CreateAssetMenu(fileName = "NpcDialogue", menuName = "Doctor Help/Dialogue/NPC Dialogue")]
    public sealed class NpcDialogueAsset : ScriptableObject
    {
        [SerializeField, TextArea(6, 20)] private string content;
        public string Content => content;
    }
}
