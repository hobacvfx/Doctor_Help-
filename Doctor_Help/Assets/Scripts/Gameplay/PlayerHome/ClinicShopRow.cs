using System;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    [RequireComponent(typeof(LayoutElement))]
    public sealed class ClinicShopRow : MonoBehaviour
    {
        [SerializeField] private Image icon, poster;
        [SerializeField] private Text itemName, description, actionLabel, stockLabel;
        [SerializeField] private Button actionButton;
        private ClinicShopItem item;
        private Action<ClinicShopItem> action;
        private LayoutElement layout;

        internal float PreferredHeight
        {
            get => (layout != null ? layout : GetComponent<LayoutElement>()).preferredHeight;
            set => layout.preferredHeight = value;
        }

        private void Awake() => layout = GetComponent<LayoutElement>();
        private void OnEnable() => actionButton.onClick.AddListener(Activate);
        private void OnDisable() => actionButton.onClick.RemoveListener(Activate);
        private void Activate() => action?.Invoke(item);

        internal void Show(ClinicShopItem definition, string buttonText, bool actionable,
            string stock, Action<ClinicShopItem> onAction)
        {
            item = definition;
            action = onAction;
            itemName.text = item.DisplayName;
            description.text = item.Description;
            icon.sprite = item.Icon;
            icon.enabled = item.Icon != null;
            poster.sprite = item.Poster;
            poster.enabled = item.Poster != null;
            poster.rectTransform.anchoredPosition = item.PosterOffset;
            stockLabel.text = stock;
            actionLabel.text = buttonText;
            actionButton.interactable = actionable;
            actionButton.gameObject.SetActive(!string.IsNullOrEmpty(buttonText));
        }
    }
}
