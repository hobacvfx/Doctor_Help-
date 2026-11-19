using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class ClinicShopPopup : MonoBehaviour
    {
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private VerticalLayoutGroup rowLayout;
        [SerializeField] private ClinicShopRow rowPrefab;
        [SerializeField] private Button close, outside, furnitureTab, consumablesTab;
        [SerializeField] private Text gold, message, empty;
        [SerializeField] private bool inventory;
        [Tooltip("Drag item prefabs from Assets/Resources/ShopItems into this list.")]
        [SerializeField] private ClinicShopItem[] items = System.Array.Empty<ClinicShopItem>();
        [SerializeField] private string furnitureSlot;
        [SerializeField] private string initialMessage;

        private readonly List<ClinicShopRow> rows = new();
        private readonly Dictionary<string, RectTransform> visibleInventoryItems = new();
        private ClinicShopItem[] inventoryItems;
        private PlayerProfileService profile;
        private bool showAllItems;
        private float fittedRowHeight;
        private int openedFrame;

        internal static bool CanInteract(DailyGameLoop loop) => loop != null &&
            loop.Step == DayStep.Preparation && !loop.IsTransitioning;

        internal void Open()
        {
            if (!CanInteract(FindFirstObjectByType<DailyGameLoop>()) ||
                (!inventory && PlayerProfileService.Instance?.ShopsOpen != true)) return;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        internal void Close() => gameObject.SetActive(false);

        private void OnEnable()
        {
            openedFrame = Time.frameCount;
            profile = PlayerProfileService.Instance;
            if (profile != null) profile.Changed += Refresh;
            close.onClick.AddListener(Close);
            outside.onClick.AddListener(CloseFromOutside);
            if (inventory)
            {
                if (inventoryItems == null)
                {
                    var definitions = new List<ClinicShopItem>();
                    if (profile != null)
                        foreach (var item in profile.ShopDefinitions)
                            if (item != null) definitions.Add(item);
                    definitions.Sort((a, b) =>
                    {
                        int group = a.IsFurniture.CompareTo(b.IsFurniture);
                        return group != 0 ? group : string.CompareOrdinal(a.Id, b.Id);
                    });
                    inventoryItems = definitions.ToArray();
                }
                furnitureTab.onClick.AddListener(ShowAllItems);
                consumablesTab.onClick.AddListener(ShowConsumables);
                showAllItems = true;
            }
            ScreenSafeArea.ApplyTo(safeArea);
            message.text = initialMessage;
            Refresh();
            ScrollToTop();
        }

        private void OnDisable()
        {
            if (profile != null) profile.Changed -= Refresh;
            close.onClick.RemoveListener(Close);
            outside.onClick.RemoveListener(CloseFromOutside);
            if (inventory)
            {
                furnitureTab.onClick.RemoveListener(ShowAllItems);
                consumablesTab.onClick.RemoveListener(ShowConsumables);
            }
        }

        private void LateUpdate()
        {
            ScreenSafeArea.ApplyTo(safeArea);
            FitFurnitureRows();
        }

        private void FitFurnitureRows()
        {
            if (rowLayout == null || rows.Count == 0) return;
            float height = (scroll.viewport.rect.height - rowLayout.spacing - rowLayout.padding.vertical - 2f) / 2f;
            height = Mathf.Clamp(height, 140f, rowPrefab.PreferredHeight);
            if (Mathf.Approximately(height, fittedRowHeight)) return;
            fittedRowHeight = height;
            foreach (var row in rows) row.PreferredHeight = height;
        }
        private void ShowAllItems() => SelectTab(true);
        private void ShowConsumables() => SelectTab(false);

        private void CloseFromOutside()
        {
            if (Time.frameCount != openedFrame) Close();
        }

        private void SelectTab(bool allItems)
        {
            showAllItems = allItems;
            message.text = initialMessage;
            Refresh();
            ScrollToTop();
        }

        private void ScrollToTop()
        {
            scroll.StopMovement();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            scroll.verticalNormalizedPosition = 1f;
        }

        private void Refresh()
        {
            visibleInventoryItems.Clear();
            bool loaded = profile != null && profile.IsLoaded;
            gold.text = loaded ? $"Gold: {profile.Money:N0}" : "";
            int visible = 0;
            if (loaded)
            {
                var seen = new HashSet<string>();
                foreach (var item in inventory ? inventoryItems : items)
                {
                    if (item == null || !item.IsConfigured || !seen.Add(item.Id)) continue;
                    int count = profile.ShopItemQuantity(item.Id);
                    if (inventory ? (!showAllItems && item.IsEquippable) || (count == 0 && !item.IsDefaultFurniture)
                        : item.IsDefaultFurniture || item.IsEquippable != !string.IsNullOrEmpty(furnitureSlot) ||
                          (item.IsEquippable && item.FurnitureSlot != furnitureSlot))
                        continue;

                    if (visible == rows.Count)
                    {
                        rows.Add(Instantiate(rowPrefab, scroll.content, false));
                        fittedRowHeight = 0f;
                    }
                    var row = rows[visible++];
                    row.gameObject.SetActive(true);
                    string equippedId = item.IsEquippable ? profile.EquippedFurnitureId(item.FurnitureSlot) : null;
                    bool equipped = item.IsEquippable &&
                        (equippedId == item.Id || (equippedId == null && item.IsDefaultFurniture));
                    if (inventory)
                    {
                        visibleInventoryItems.Add(item.Id, (RectTransform)row.transform);
                        string label = item.IsEquippable ? (equipped ? "Equipped" : "Equip") : "";
                        row.Show(item, label, item.IsEquippable && !equipped,
                            item.IsEquippable ? "" : $"Owned: {count:N0}", Equip);
                    }
                    else
                    {
                        string label = item.IsEquippable && count > 0
                            ? (equipped ? "Equipped" : "Owned")
                            : $"Buy · {item.Price:N0}";
                        row.Show(item, label, (!item.IsEquippable || count == 0) && profile.IsUpgradeUnlocked(item),
                            item.IsEquippable ? "" : $"Owned: {count:N0}", Buy);
                    }
                }
            }
            for (int i = visible; i < rows.Count; i++) rows[i].gameObject.SetActive(false);
            FitFurnitureRows();
            empty.gameObject.SetActive(visible == 0);
            empty.text = loaded ? (inventory ? "Nothing in this section yet." : "No items available.") :
                "The player profile is unavailable.";
            if (inventory)
            {
                furnitureTab.interactable = !showAllItems;
                consumablesTab.interactable = showAllItems;
            }
        }

        private void Buy(ClinicShopItem item)
        {
            if (!CanInteract(FindFirstObjectByType<DailyGameLoop>())) { Close(); return; }
            if (profile.TryBuyShopItem(item, out string error))
                message.text = item.IsEquippable ? $"{item.DisplayName} equipped." : $"{item.DisplayName} added to Inventory.";
            else
                message.text = error;
        }

        private void Equip(ClinicShopItem item)
        {
            if (!CanInteract(FindFirstObjectByType<DailyGameLoop>())) { Close(); return; }
            string previousId = profile.EquippedFurnitureId(item.FurnitureSlot);
            bool equipped = profile.TryEquipFurniture(item);
            message.text = equipped ? $"{item.DisplayName} equipped." : "This item cannot be equipped.";
            if (equipped && previousId != null && visibleInventoryItems.TryGetValue(previousId, out var previousRow))
                RevealInventoryItem(previousRow);
        }

        private void RevealInventoryItem(RectTransform row)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            var viewport = scroll.viewport.rect;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, row);
            float range = scroll.content.rect.height - viewport.height;
            if (range <= 0f) return;
            float offset = bounds.min.y < viewport.yMin ? viewport.yMin - bounds.min.y :
                bounds.max.y > viewport.yMax ? viewport.yMax - bounds.max.y : 0f;
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition - offset / range);
        }
    }
}
