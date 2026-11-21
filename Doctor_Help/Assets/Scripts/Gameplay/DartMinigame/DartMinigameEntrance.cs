using System.Collections;
using UnityEngine;

namespace DoctorHelp
{
    // Street world button that opens the Dart minigame, following the ClinicShopEntrance pattern.
    public sealed class DartMinigameEntrance : MonoBehaviour
    {
        [SerializeField] private DeskActionButton actionButton;
        [SerializeField] private DartMinigame minigame;
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
            if (actionButton != null) actionButton.gameObject.SetActive(profile != null && profile.IsLoaded);
        }

        private void OnEnable()
        {
            if (actionButton != null && minigame != null) actionButton.Clicked += ScheduleOpen;
        }

        private void OnDisable()
        {
            if (actionButton != null) actionButton.Clicked -= ScheduleOpen;
            if (pendingOpen != null) StopCoroutine(pendingOpen);
            pendingOpen = null;
        }

        private void ScheduleOpen()
        {
            if (pendingOpen == null && DartMinigame.Current == null) pendingOpen = StartCoroutine(OpenAfterWorldClick());
        }

        private IEnumerator OpenAfterWorldClick()
        {
            // Let the world click finish before the minigame covers the Street.
            yield return null;
            pendingOpen = null;
            DartMinigame.Open(minigame, actionButton.TargetCamera);
        }

        private void OnDestroy()
        {
            if (profile != null) profile.Changed -= RefreshVisibility;
        }
    }
}
