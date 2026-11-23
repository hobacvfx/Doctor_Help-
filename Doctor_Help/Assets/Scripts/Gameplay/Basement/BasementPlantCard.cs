using System;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    // Stat texts shared by the Protection popup and shop cards, so both always show the same numbers.
    internal static class BasementPlantText
    {
        internal const string Infinity = "∞";
        internal const string Empty = "—";

        internal static string Damage(BasementPlantStats stats) =>
            stats.Swallows ? Infinity : stats.Damage.ToString("0.##");
        internal static string Cooldown(BasementPlantStats stats) => $"{stats.CooldownSeconds:0.##} s";
        internal static string Targets(BasementPlantStats stats) => stats.Targets.ToString();
        internal static string Range(BasementPlantStats stats) => stats.Range switch
        {
            BasementPlantRange.Normal => $"Normal \u00B7 {stats.RangeMetres:0.#} m",
            BasementPlantRange.Far => "Far",
            _ => "Melee"
        };
    }

    // One horizontal card in the Plant Shop: portrait left; name, stats and its own action button right.
    // Owned plants (storage) show Equip with no price; catalog plants show Buy with their clean-water price.
    // Only the button acts; tapping the card itself does nothing, so scrolling never buys by accident.
    [RequireComponent(typeof(LayoutElement))]
    public sealed class BasementPlantCard : MonoBehaviour
    {
        [SerializeField] private Image portrait;
        [SerializeField] private Text plantName, detail, damageValue, cooldownValue, targetsValue, rangeValue;
        [SerializeField] private Button actionButton;
        [SerializeField] private Text actionLabel;
        [Tooltip("Water icon + price, shown for Buy only.")]
        [SerializeField] private GameObject cost;
        [SerializeField] private Text costValue;

        private Action action;

        private void OnEnable() => actionButton.onClick.AddListener(Activate);
        private void OnDisable() => actionButton.onClick.RemoveListener(Activate);
        private void Activate() => action?.Invoke();

        internal void Show(BasementPlantProfile kind, int level, string detailText, string buttonText, int? price,
            bool interactable, Action onAction)
        {
            action = onAction; // Replaces the previous callback: a reused card never acts for its old plant.
            var stats = kind.StatsAt(level);
            portrait.sprite = kind.Portrait;
            portrait.enabled = kind.Portrait != null;
            plantName.text = kind.DisplayName;
            detail.text = detailText;
            detail.gameObject.SetActive(!string.IsNullOrEmpty(detailText));
            damageValue.text = BasementPlantText.Damage(stats);
            cooldownValue.text = BasementPlantText.Cooldown(stats);
            targetsValue.text = BasementPlantText.Targets(stats);
            if (rangeValue != null) rangeValue.text = BasementPlantText.Range(stats);
            actionLabel.text = buttonText;
            actionButton.interactable = interactable;
            cost.SetActive(price.HasValue);
            if (price.HasValue) costValue.text = price.Value.ToString("N0");
        }
    }
}
