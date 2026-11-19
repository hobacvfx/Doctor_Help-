using System.Collections;
using UnityEngine;

namespace DoctorHelp
{
    public sealed class ClinicShopEntrance : MonoBehaviour
    {
        [SerializeField] private DeskActionButton actionButton;
        [SerializeField] private ClinicShopPopup popup;
        private Coroutine pendingOpen;
        private PlayerProfileService profile;

        private void Awake()
        {
            profile = PlayerProfileService.Instance;
            if (profile != null) profile.Changed += RefreshVisibility;
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            if (actionButton != null)
                actionButton.gameObject.SetActive(profile != null && profile.ShopsOpen);
        }

        private void OnEnable()
        {
            if (actionButton != null && popup != null) actionButton.Clicked += ScheduleOpen;
        }
        private void OnDisable()
        {
            if (actionButton != null) actionButton.Clicked -= ScheduleOpen;
            if (pendingOpen != null) StopCoroutine(pendingOpen);
            pendingOpen = null;
        }

        private void ScheduleOpen()
        {
            if (pendingOpen == null) pendingOpen = StartCoroutine(OpenAfterWorldClick());
        }

        private IEnumerator OpenAfterWorldClick()
        {
            // Let the EventSystem finish the click on the world button before showing the modal.
            yield return null;
            pendingOpen = null;
            if (popup != null) popup.Open();
        }

        private void OnDestroy()
        {
            if (profile != null) profile.Changed -= RefreshVisibility;
        }
    }
}
