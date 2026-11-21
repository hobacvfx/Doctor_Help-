using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public enum CurrencyKind { Gold, Water, Electricity }

    // Shared top bar: gold, clean water, electricity, day and Settings.
    // Rewards fly into it as icons (see HeadbarRewardFlight.cs); the shown amounts count up as they land.
    public sealed partial class Headbar : MonoBehaviour
    {
        [SerializeField] private Text gold, day;
        [SerializeField] private Text water, electricity;
        [SerializeField] private Button settingsButton;
        [SerializeField] private SettingsPopup settings;

        private static readonly List<Headbar> enabledBars = new List<Headbar>();
        private PlayerProfileService profile;

        // The most recently enabled Headbar, e.g. the Dart minigame's own bar while it is open.
        internal static Headbar Current => enabledBars.Count > 0 ? enabledBars[enabledBars.Count - 1] : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => enabledBars.Clear();

        private void Awake() => PrepareFlightLayer();

        private void OnEnable()
        {
            enabledBars.Remove(this);
            enabledBars.Add(this);
            profile = PlayerProfileService.Instance;
            if (profile != null) profile.Changed += Refresh;
            settingsButton.onClick.AddListener(settings.Open);
            Refresh();
        }

        private void Refresh()
        {
            bool loaded = profile != null && profile.IsLoaded;
            gold.text = loaded ? Shown(CurrencyKind.Gold, profile.Money) : "—";
            if (water != null) water.text = loaded ? Shown(CurrencyKind.Water, profile.Water) : "—";
            if (electricity != null) electricity.text = loaded ? Shown(CurrencyKind.Electricity, profile.Electricity) : "—";
            day.text = loaded ? $"DAY {profile.CurrentDay}" : "DAY —";
        }

        private void OnDisable()
        {
            enabledBars.Remove(this);
            StopFlights();
            if (profile != null) profile.Changed -= Refresh;
            settingsButton.onClick.RemoveListener(settings.Open);
        }

        public void CloseSettings() => settings.Close();

        private Text Amount(CurrencyKind kind) => kind switch
        {
            CurrencyKind.Water => water,
            CurrencyKind.Electricity => electricity,
            _ => gold
        };
    }
}
