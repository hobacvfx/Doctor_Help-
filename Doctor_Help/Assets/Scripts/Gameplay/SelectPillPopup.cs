using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class SelectPillPopup : MonoBehaviour
    {
        [Serializable]
        public sealed class PillOption { public string medicineId; public Button button; }
        [SerializeField] private Button giveButton;
        [SerializeField] private Text selectedPillText;
        [SerializeField] private Text errorText;
        [SerializeField] private string selectedTextPrefix = "Đã chọn: ";
        [SerializeField] private PillOption[] options;
        public event Action GiveRequested;
        public string SelectedMedicineId { get; private set; }
        private readonly List<UnityAction> selectionHandlers = new List<UnityAction>();
        [NonSerialized] private bool initialized;
        private string initialSelectionText;
        public bool IsConfigured
        {
            get
            {
                if (!IsLiveButton(giveButton) || selectedPillText == null || errorText == null || options == null || options.Length == 0) return false;
                var ids = new HashSet<string>(StringComparer.Ordinal);
                var buttons = new HashSet<Button>();
                foreach (var option in options)
                    if (option == null || !IsLiveButton(option.button) || string.IsNullOrWhiteSpace(option.medicineId) ||
                        !ids.Add(option.medicineId) || !buttons.Add(option.button)) return false;
                return true;
            }
        }
        private bool IsLiveButton(Button button) => button != null && button.gameObject != null &&
            button.targetGraphic != null && button.transform.IsChildOf(transform);
        private void Awake() => Initialize();
        private void OnEnable() => Initialize();
        private void Initialize()
        {
            if (initialized) return;
            if (!IsConfigured) throw new InvalidOperationException("SelectPillPopup has invalid references or duplicate options.");
            initialSelectionText = selectedPillText.text;
            giveButton.onClick.AddListener(OnGive);
            foreach (var option in options)
            {
                string id = option.medicineId;
                UnityAction handler = () => Select(id);
                selectionHandlers.Add(handler);
                option.button.onClick.AddListener(handler);
            }
            initialized = true;
        }
        public void ResetSelection()
        {
            Initialize(); SelectedMedicineId = null; selectedPillText.text = initialSelectionText;
            giveButton.interactable = false; errorText.gameObject.SetActive(false);
        }
        private void Select(string id)
        {
            SelectedMedicineId = id; selectedPillText.text = selectedTextPrefix + id; giveButton.interactable = true;
        }
        private void OnGive() { if (SelectedMedicineId != null) GiveRequested?.Invoke(); }
        private void OnDestroy()
        {
            if (!initialized) return;
            if (giveButton != null) giveButton.onClick.RemoveListener(OnGive);
            for (int i = 0; i < selectionHandlers.Count; i++)
                if (options[i].button != null) options[i].button.onClick.RemoveListener(selectionHandlers[i]);
        }
    }
}
