using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    // Square Protection popup of one basement shelf. Left: the shelf's plant (portrait, name, level) with an
    // icon-only Swap button, or a large + when the shelf is empty. Right: Attack Damage, Attack Cooldown,
    // Targets, then Upgrade with its clean-water price. Reads everything from BasementDefenseService.
    public sealed class BasementProtectionPopup : MonoBehaviour
    {
        [SerializeField] private RectTransform safeArea, panel;
        [SerializeField] private Vector2 panelSize = new Vector2(500f, 500f);
        [SerializeField] private Button outside, close, add, swap, upgrade;
        [SerializeField] private Image portrait;
        [SerializeField] private GameObject nameplate;
        [SerializeField] private Text plantName, plantLevel;
        [SerializeField] private Text damageValue, cooldownValue, targetsValue, rangeValue;
        [SerializeField] private Text upgradeLabel;
        [Tooltip("Water icon + price row inside the Upgrade button.")]
        [SerializeField] private GameObject upgradeCost;
        [SerializeField] private Text upgradeCostValue;
        [SerializeField] private Text message;
        [SerializeField] private string upgradeText = "Upgrade";
        [SerializeField] private string maxLevelText = "Max Level";
        [SerializeField] private string levelFormat = "Lv. {0}";
        [Tooltip("Upgrade label height above the button center while the price row is shown.")]
        [SerializeField] private float upgradeLabelRaise = 17f;

        private BasementProtectionFlow flow;
        private string slotId;
        private int openedFrame;
        private (int instance, int level, BasementPlantProfile kind, int water, bool canChange)? shownKey;

        internal string SlotId => slotId;
        internal bool IsOpen => gameObject.activeSelf;

        internal void Open(string slot, BasementProtectionFlow owner)
        {
            slotId = slot; // A new shelf: everything below is rebuilt for it, nothing kept from the last one.
            flow = owner;
            shownKey = null;
            transform.SetAsLastSibling();
            PopupAnimation.Show(gameObject);
            openedFrame = Time.frameCount;
            SetMessage(flow != null && !flow.CanChangePlants ? flow.UnavailableMessage : "");
            Refresh();
        }

        internal void Close() => PopupAnimation.Hide(gameObject);

        internal void SetMessage(string text)
        {
            if (text == null) return; // Ignored repeated tap.
            message.text = text;
        }

        private void OnEnable()
        {
            openedFrame = Time.frameCount;
            outside.onClick.AddListener(CloseFromOutside);
            close.onClick.AddListener(Close);
            add.onClick.AddListener(OpenShop);
            swap.onClick.AddListener(OpenSwap);
            upgrade.onClick.AddListener(Upgrade);
        }

        private void OnDisable()
        {
            outside.onClick.RemoveListener(CloseFromOutside);
            close.onClick.RemoveListener(Close);
            add.onClick.RemoveListener(OpenShop);
            swap.onClick.RemoveListener(OpenSwap);
            upgrade.onClick.RemoveListener(Upgrade);
        }

        private void CloseFromOutside()
        {
            if (Time.frameCount != openedFrame) Close();
        }

        private void OpenShop() { if (flow != null) flow.OpenShop(slotId, false); }
        private void OpenSwap() { if (flow != null) flow.OpenShop(slotId, true); }

        private void Upgrade()
        {
            if (flow == null) return;
            SetMessage(flow.Upgrade(slotId));
            Refresh();
        }

        private void LateUpdate()
        {
            BasementPopupLayout.Fit(safeArea, panel, panelSize, 16f, 56f, 54f, false, message.rectTransform);
            Refresh(); // Cheap unless the shelf, level, water or spending rule changed.
        }

        private void Refresh()
        {
            var service = flow != null ? flow.Service : null;
            var plant = service != null ? service.ShelfPlant(slotId) : default;
            bool canChange = flow != null && flow.CanChangePlants;
            var profile = PlayerProfileService.Instance;
            int water = profile != null && profile.IsLoaded ? profile.Water : 0;
            var key = (plant.InstanceId, plant.Level, plant.Kind, water, canChange);
            if (shownKey.HasValue && shownKey.Value.Equals(key)) return;
            shownKey = key;

            bool hasPlant = plant.Kind != null;
            add.gameObject.SetActive(!hasPlant);
            add.interactable = canChange;
            portrait.gameObject.SetActive(hasPlant);
            nameplate.SetActive(hasPlant);
            swap.gameObject.SetActive(hasPlant);
            swap.interactable = canChange;
            if (!hasPlant)
            {
                damageValue.text = cooldownValue.text = targetsValue.text = BasementPlantText.Empty;
                if (rangeValue != null) rangeValue.text = BasementPlantText.Empty;
                upgradeLabel.text = upgradeText;
                upgradeLabel.rectTransform.anchoredPosition = Vector2.zero;
                upgradeCost.SetActive(false); // No made-up price for an empty shelf.
                upgrade.interactable = false;
                return;
            }

            var stats = plant.Stats;
            portrait.sprite = plant.Kind.Portrait;
            portrait.enabled = plant.Kind.Portrait != null;
            plantName.text = plant.Kind.DisplayName;
            plantLevel.text = string.Format(levelFormat, plant.Level);
            damageValue.text = BasementPlantText.Damage(stats);
            cooldownValue.text = BasementPlantText.Cooldown(stats);
            targetsValue.text = BasementPlantText.Targets(stats);
            if (rangeValue != null) rangeValue.text = BasementPlantText.Range(stats);
            bool canLevel = plant.Kind.CanUpgradeFrom(plant.Level);
            upgradeLabel.text = canLevel ? upgradeText : maxLevelText;
            upgradeCost.SetActive(canLevel);
            upgradeLabel.rectTransform.anchoredPosition = new Vector2(0f, canLevel ? upgradeLabelRaise : 0f);
            if (canLevel) upgradeCostValue.text = plant.Kind.UpgradeCostFrom(plant.Level).ToString("N0");
            // Not enough water stays clickable so the player is told why; Max Level and closed spending lock it.
            upgrade.interactable = canLevel && canChange;
        }
    }
}
