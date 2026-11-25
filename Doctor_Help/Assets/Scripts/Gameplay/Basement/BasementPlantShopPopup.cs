using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    // Plant Shop for one shelf: a vertical, drag-scrolled list of cards with no scrollbar. Owned plants in storage
    // come first (Equip, free); then every playable catalog plant (Buy, clean water). Opened from the + of an
    // empty shelf, or from Swap (title "Swap Plant": the shelf's current plant goes to storage).
    public sealed class BasementPlantShopPopup : MonoBehaviour
    {
        [SerializeField] private RectTransform safeArea, panel;
        [Tooltip("Width and maximum height; the height shrinks to the Safe Area on short screens.")]
        [SerializeField] private Vector2 panelSize = new Vector2(500f, 800f);
        [SerializeField] private Button outside, close;
        [SerializeField] private Text title, waterBalance, message, empty;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private BasementPlantCard cardPrefab;
        [SerializeField] private string shopTitle = "Plant Shop";
        [SerializeField] private string swapTitle = "Swap Plant";
        [SerializeField] private string ownedFormat = "In storage · Lv. {0}";
        [SerializeField] private string equipText = "Equip";
        [SerializeField] private string buyText = "Buy";

        private readonly List<BasementPlantCard> cards = new List<BasementPlantCard>();
        private readonly List<BasementOwnedPlant> stored = new List<BasementOwnedPlant>();
        private BasementProtectionFlow flow;
        private string slotId;
        private int openedFrame;
        private string shownKey;
        private readonly System.Text.StringBuilder keyBuilder = new System.Text.StringBuilder();

        internal void Open(string slot, bool swapMode, BasementProtectionFlow owner)
        {
            slotId = slot;
            flow = owner;
            title.text = swapMode ? swapTitle : shopTitle;
            shownKey = null;
            transform.SetAsLastSibling();
            PopupAnimation.Show(gameObject);
            openedFrame = Time.frameCount;
            message.text = flow != null && !flow.CanChangePlants ? flow.UnavailableMessage : "";
            Refresh();
            scroll.StopMovement();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            scroll.verticalNormalizedPosition = 1f;
        }

        internal void Close() => PopupAnimation.Hide(gameObject);

        private void OnEnable()
        {
            openedFrame = Time.frameCount;
            outside.onClick.AddListener(CloseFromOutside);
            close.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            outside.onClick.RemoveListener(CloseFromOutside);
            close.onClick.RemoveListener(Close);
        }

        private void CloseFromOutside()
        {
            if (Time.frameCount != openedFrame) Close();
        }

        private void LateUpdate()
        {
            BasementPopupLayout.Fit(safeArea, panel, panelSize, 16f, 56f, 54f, true, message.rectTransform);
            Refresh();
        }

        private void Refresh()
        {
            var service = flow != null ? flow.Service : null;
            var profile = PlayerProfileService.Instance;
            bool loaded = service != null && profile != null && profile.IsLoaded;
            bool canChange = flow != null && flow.CanChangePlants;
            if (loaded) service.StoredPlants(stored);
            else stored.Clear();
            var key = keyBuilder.Clear();
            key.Append(loaded ? profile.Water : -1).Append('|').Append(canChange);
            foreach (var plant in stored) key.Append('|').Append(plant.InstanceId).Append(':').Append(plant.Level);
            if (key.ToString() == shownKey) return;
            shownKey = key.ToString();

            waterBalance.text = loaded ? profile.Water.ToString("N0") : "—";
            int visible = 0;
            if (loaded)
            {
                foreach (var plant in stored)
                {
                    var owned = plant;
                    Card(visible++).Show(owned.Kind, owned.Level, string.Format(ownedFormat, owned.Level), equipText,
                        null, canChange, () => Equip(owned));
                }
                foreach (var kind in service.Catalog.Playable)
                {
                    var bought = kind;
                    // Not enough water stays clickable: the player is told, and nothing changes.
                    Card(visible++).Show(bought, 1, "", buyText, bought.BuyCost, canChange, () => Buy(bought));
                }
            }
            for (int i = visible; i < cards.Count; i++) cards[i].gameObject.SetActive(false);
            empty.gameObject.SetActive(visible == 0);
        }

        private BasementPlantCard Card(int index)
        {
            if (index == cards.Count) cards.Add(Instantiate(cardPrefab, scroll.content, false));
            var card = cards[index];
            card.gameObject.SetActive(true);
            return card;
        }

        private void Buy(BasementPlantProfile kind)
        {
            if (flow == null) return;
            Finish(flow.Buy(slotId, kind, out bool done), done);
        }

        private void Equip(BasementOwnedPlant plant)
        {
            if (flow == null) return;
            Finish(flow.Equip(slotId, plant, out bool done), done);
        }

        // Success closes the shop and shows the result on the shelf's Protection popup.
        private void Finish(string text, bool done)
        {
            if (text == null) return; // Ignored repeated tap.
            shownKey = null;
            if (!done) { message.text = text; return; }
            Close();
            var protection = FindProtectionPopup();
            if (protection != null && protection.IsOpen && protection.SlotId == slotId) protection.SetMessage(text);
        }

        private BasementProtectionPopup FindProtectionPopup() =>
            transform.parent != null ? transform.parent.GetComponentInChildren<BasementProtectionPopup>(true) : null;
    }
}
