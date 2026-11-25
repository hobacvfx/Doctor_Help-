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
        private bool available = true;
        internal DeskActionButton ActionButton => actionButton;

        internal void SetAvailable(bool value)
        {
            available = value;
            if (!available && pendingOpen != null)
            {
                StopCoroutine(pendingOpen);
                pendingOpen = null;
            }
            RefreshVisibility();
        }

        private void Awake()
        {
            profile = PlayerProfileService.Instance;
            if (profile != null) profile.Changed += RefreshVisibility;
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            if (actionButton != null)
                actionButton.gameObject.SetActive(available && profile != null && profile.ShopsOpen);
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
            if (!available) return;
            if (pendingOpen == null) pendingOpen = StartCoroutine(OpenAfterWorldClick());
        }

        private IEnumerator OpenAfterWorldClick()
        {
            // Let the EventSystem finish the click on the world button before showing the modal.
            yield return null;
            pendingOpen = null;
            if (available && popup != null) popup.Open();
        }

        private void OnDestroy()
        {
            if (profile != null) profile.Changed -= RefreshVisibility;
        }
    }
}
