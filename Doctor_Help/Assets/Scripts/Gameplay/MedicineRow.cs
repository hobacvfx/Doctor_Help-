using System;
using UnityEngine;
using UnityEngine.UI;
namespace DoctorHelp
{
    public sealed class MedicineRow : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Sprite placeholderSprite;
        [SerializeField] private Text nameText, descriptionText, priceText;
        [SerializeField] private Color diseaseNameColor = new Color(.37f, .15f, .51f);
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject check;
        private string id;
        private Action<string> select;
        internal Button SelectButton => selectButton;
        public void Bind(PillDefinition pill, Action<string> select)
        {
            id = pill.Id;
            this.select = select;
            selectButton.onClick.AddListener(Select);
        }
        public void Refresh(PillDefinition pill, bool selected)
        {
            nameText.text = pill.DisplayName;
            descriptionText.supportRichText = true;
            string description = pill.Description ?? "";
            descriptionText.text = description.Replace("<disease>", $"<color=#{ColorUtility.ToHtmlStringRGB(diseaseNameColor)}><b>")
                .Replace("</disease>", "</b></color>");
            icon.sprite = pill.Sprite != null ? pill.Sprite : placeholderSprite;
            icon.enabled = icon.sprite != null;
            priceText.text = $"{pill.Price} gold";
            check.SetActive(selected);
        }
        private void Select() => select?.Invoke(id);
        private void OnDestroy()
        {
            selectButton.onClick.RemoveListener(Select);
        }
    }
}
