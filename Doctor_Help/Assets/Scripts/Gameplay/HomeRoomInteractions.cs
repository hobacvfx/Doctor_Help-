using System;
using UnityEngine;

namespace DoctorHelp
{
    public sealed class HomeRoomInteractions : MonoBehaviour
    {
        [SerializeField] private DeskActionButton newsButton;
        [SerializeField] private NewspaperResultsPopup newspaper;

        public bool IsConfigured => newsButton != null && newsButton.IsConfigured &&
            newspaper != null && newspaper.IsConfigured;

        private void OnEnable() => newsButton.Clicked += OpenNews;

        private void OnDisable()
        {
            if (newsButton != null) newsButton.Clicked -= OpenNews;
            if (newspaper != null) newspaper.gameObject.SetActive(false);
        }

        private async void OpenNews()
        {
            var loop = FindFirstObjectByType<DailyGameLoop>();
            if (loop == null || loop.Step != DayStep.Preparation || loop.IsTransitioning ||
                newspaper.gameObject.activeSelf) return;

            try { await newspaper.ShowLatestAsync(loop.LatestDayResult, destroyCancellationToken); }
            catch (OperationCanceledException) { }
            catch (Exception error) { Debug.LogException(error, this); }
        }
    }
}
