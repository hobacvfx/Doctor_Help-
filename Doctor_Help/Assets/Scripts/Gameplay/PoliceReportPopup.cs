using System;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class PoliceReportPopup : MonoBehaviour
    {
        [SerializeField] private Button vampireButton, zombieButton, reportButton, outsideButton, closeButton;
        [SerializeField] private GameObject vampireCheck, zombieCheck;
        [SerializeField] private Text errorText;
        public event Action ReportRequested, CloseRequested;
        public PoliceReportReason SelectedReason { get; private set; }
        public bool IsConfigured => IsButton(vampireButton) && IsButton(zombieButton) && IsButton(reportButton) &&
            IsButton(outsideButton) && IsButton(closeButton) && vampireCheck != null && zombieCheck != null && errorText != null;
        private bool initialized;
        private bool IsButton(Button button) => button != null && button.targetGraphic != null && button.transform.IsChildOf(transform);
        private void Awake() => Initialize();
        private void Initialize()
        {
            if (initialized) return;
            if (!IsConfigured) throw new InvalidOperationException("Police report popup references are incomplete.");
            vampireButton.onClick.AddListener(SelectVampire);
            zombieButton.onClick.AddListener(SelectZombie);
            reportButton.onClick.AddListener(Report);
            outsideButton.onClick.AddListener(Close);
            closeButton.onClick.AddListener(Close);
            initialized = true;
        }
        public void ResetSelection()
        {
            Initialize();
            SelectedReason = PoliceReportReason.None;
            Refresh(); ShowError("");
        }
        public void ShowError(string message) { errorText.text = message; errorText.gameObject.SetActive(!string.IsNullOrEmpty(message)); }
        private void SelectVampire() => Select(PoliceReportReason.VampireInfected);
        private void SelectZombie() => Select(PoliceReportReason.ZombieInfected);
        private void Select(PoliceReportReason reason) { SelectedReason = reason; Refresh(); ShowError(""); }
        private void Refresh()
        {
            vampireCheck.SetActive(SelectedReason == PoliceReportReason.VampireInfected);
            zombieCheck.SetActive(SelectedReason == PoliceReportReason.ZombieInfected);
            reportButton.interactable = SelectedReason != PoliceReportReason.None;
        }
        private void Report() { if (SelectedReason != PoliceReportReason.None) ReportRequested?.Invoke(); }
        private void Close() => CloseRequested?.Invoke();
        private void OnDestroy()
        {
            if (!initialized) return;
            vampireButton.onClick.RemoveListener(SelectVampire); zombieButton.onClick.RemoveListener(SelectZombie);
            reportButton.onClick.RemoveListener(Report); outsideButton.onClick.RemoveListener(Close); closeButton.onClick.RemoveListener(Close);
        }
    }
}
