using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class SelectPillPopup : MonoBehaviour
    {
        [Serializable]
        public sealed class PillOption
        {
            public string medicineId;
            public Button button;
        }

        [SerializeField] private Button giveButton;
        [SerializeField] private Text selectedPillText;
        [SerializeField] private Text errorText;
        [SerializeField] private string selectedTextPrefix = "Đã chọn: ";
        [SerializeField] private PillOption[] options;

        public bool IsConfigured => giveButton != null && selectedPillText != null && errorText != null &&
            options != null && options.Length > 0 && Array.TrueForAll(options,
                option => option != null && option.button != null && !string.IsNullOrWhiteSpace(option.medicineId));

        public void Bind(Action<string> select, UnityAction give)
        {
            if (!IsConfigured) throw new InvalidOperationException("SelectPillPopup has missing Inspector references.");
            giveButton.interactable = false;
            giveButton.onClick.AddListener(give);
            errorText.gameObject.SetActive(false);
            foreach (var option in options)
            {
                var entry = option;
                entry.button.onClick.AddListener(() =>
                {
                    select(entry.medicineId);
                    selectedPillText.text = selectedTextPrefix + entry.medicineId;
                    giveButton.interactable = true;
                });
            }
        }

        public void ShowSaveError() => errorText.gameObject.SetActive(true);
    }
}
