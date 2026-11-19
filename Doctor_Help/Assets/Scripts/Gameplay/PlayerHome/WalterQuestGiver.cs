using UnityEngine;
using TMPro;

namespace DoctorHelp
{
    [RequireComponent(typeof(DeskActionButton))]
    internal sealed class WalterQuestGiver : MonoBehaviour
    {
        [SerializeField] private TMP_Text marker;
        [SerializeField] private WalterDialogueView dialogue;
        [SerializeField] private Color availableColor = new(1f, .78f, .27f);
        [SerializeField] private Color acceptedColor = new(.59f, .59f, .59f);
        private PlayerProfileService profile;
        private DeskActionButton interaction;

        private void OnEnable()
        {
            profile = PlayerProfileService.Instance;
            interaction = GetComponent<DeskActionButton>();
            interaction.Clicked += OpenDialogue;
            if (profile != null) profile.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            interaction.Clicked -= OpenDialogue;
            if (profile != null) profile.Changed -= Refresh;
        }

        private void Refresh()
        {
            bool loaded = profile != null && profile.IsLoaded;
            var state = loaded ? profile.WalterContract.state : WalterQuestState.Failed;
            marker.gameObject.SetActive(loaded && state is not (WalterQuestState.Failed or WalterQuestState.Claimed));
            marker.color = state == WalterQuestState.Available ? availableColor : acceptedColor;
        }

        private void OpenDialogue()
        {
            if (profile == null || !profile.IsLoaded) return;
            dialogue.Show(profile);
        }
    }
}
