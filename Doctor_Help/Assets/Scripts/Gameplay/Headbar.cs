using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class Headbar : MonoBehaviour
    {
        [SerializeField] private Text gold, day;
        [SerializeField] private Button settingsButton;
        [SerializeField] private SettingsPopup settings;

        private PlayerProfileService profile;
        internal RectTransform GoldTarget => gold.rectTransform;

        private void OnEnable()
        {
            profile = PlayerProfileService.Instance;
            if (profile != null) profile.Changed += Refresh;
            settingsButton.onClick.AddListener(settings.Open);
            Refresh();
        }

        private void Refresh()
        {
            bool loaded = profile != null && profile.IsLoaded;
            gold.text = loaded ? profile.Money.ToString() : "—";
            day.text = loaded ? $"DAY {profile.CurrentDay}" : "DAY —";
        }

        private void OnDisable()
        {
            if (profile != null) profile.Changed -= Refresh;
            settingsButton.onClick.RemoveListener(settings.Open);
        }

        public void CloseSettings() => settings.Close();
    }
}
