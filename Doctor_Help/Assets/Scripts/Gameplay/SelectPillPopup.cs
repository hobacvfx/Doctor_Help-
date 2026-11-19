using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace DoctorHelp
{
    public sealed class SelectPillPopup : MonoBehaviour
    {
        [SerializeField] private MedicineListView medicines;
        [SerializeField] private Button giveButton, declineButton, outsideButton, closeButton;
        [SerializeField] private Text selectedPillText, errorText;
        public event Action GiveRequested, DeclineRequested, CloseRequested;
        public IReadOnlyList<string> SelectedMedicineIds => medicines.SelectedIds;
        internal MedicineListView MedicineList => medicines;
        internal Button GiveButton => giveButton;
        public bool IsConfigured => medicines != null && giveButton != null && giveButton.targetGraphic != null &&
            declineButton != null && declineButton.targetGraphic != null && outsideButton != null && closeButton != null &&
            selectedPillText != null && errorText != null;
        private bool initialized;
        private bool allowEmpty;
        private void Awake() => Initialize();
        private void Initialize()
        {
            if (initialized) return;
            if (!IsConfigured) throw new InvalidOperationException("Treatment popup references are incomplete.");
            medicines.SelectionChanged += UpdateSelection;
            giveButton.onClick.AddListener(Give); outsideButton.onClick.AddListener(Close); closeButton.onClick.AddListener(Close);
            declineButton.onClick.AddListener(Decline);
            initialized = true;
        }
        public void ResetSelection(bool allowEmpty = false)
        {
            Initialize();
            this.allowEmpty = allowEmpty;
            medicines.ClearSelection();
            UpdateSelection();
        }
        public void ShowError(string error) { errorText.text = error; errorText.gameObject.SetActive(!string.IsNullOrEmpty(error)); }
        private void UpdateSelection()
        {
            ShowError("");
            selectedPillText.text = $"Total: {medicines.SelectedCost} gold";
            giveButton.interactable = allowEmpty || SelectedMedicineIds.Count > 0;
        }
        private void Give() { if (giveButton.interactable) GiveRequested?.Invoke(); }
        private void Decline() => DeclineRequested?.Invoke();
        private void Close() => CloseRequested?.Invoke();
        private void OnDestroy()
        {
            if (!initialized) return;
            medicines.SelectionChanged -= UpdateSelection;
            giveButton.onClick.RemoveListener(Give); outsideButton.onClick.RemoveListener(Close); closeButton.onClick.RemoveListener(Close);
            declineButton.onClick.RemoveListener(Decline);
        }
    }
}
