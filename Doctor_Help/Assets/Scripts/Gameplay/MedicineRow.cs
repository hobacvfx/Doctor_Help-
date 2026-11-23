using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace DoctorHelp
{
    public sealed class MedicineRow : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private Sprite placeholderSprite;
        [SerializeField] private Text nameText, descriptionText, priceText;
        [SerializeField] private Color diseaseNameColor = new Color(.37f, .15f, .51f);
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject check;
        private string id;
        private Action<string> select;
        private SelectPillPopup treatment;
        internal Button SelectButton => selectButton;
        public void Bind(PillDefinition pill, Action<string> select)
        {
            id = pill.Id;
            this.select = select;
            treatment = GetComponentInParent<SelectPillPopup>(true);
            selectButton.onClick.AddListener(Select);
        }
        public void Refresh(PillDefinition pill, bool selected)
        {
            if (nameText != null) nameText.text = pill.DisplayName;
            if (descriptionText != null) descriptionText.supportRichText = true;
            string description = pill.Description ?? "";
            if (descriptionText != null) descriptionText.text = description.Replace("<disease>", $"<color=#{ColorUtility.ToHtmlStringRGB(diseaseNameColor)}><b>")
                .Replace("</disease>", "</b></color>");
            icon.sprite = pill.Sprite != null ? pill.Sprite : placeholderSprite;
            icon.enabled = icon.sprite != null;
            if (priceText != null) priceText.text = $"{pill.Price} gold";
            if (check != null) check.SetActive(selected);
        }
        private void Select() { if (treatment == null) select?.Invoke(id); }
        public void OnBeginDrag(PointerEventData data) => treatment?.BeginMedicineDrag(id, icon, data);
        public void OnDrag(PointerEventData data) => treatment?.MoveMedicineDrag(data);
        public void OnEndDrag(PointerEventData data) => treatment?.EndMedicineDrag(data);
        private void OnDestroy()
        {
            selectButton.onClick.RemoveListener(Select);
        }
    }
}
