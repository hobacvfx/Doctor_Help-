using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    // References are scene-owned, including instances of the editable UI prefabs.
    public sealed class SceneGameplayUI : MonoBehaviour
    {
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private Text statusText;
        [SerializeField] private Text errorText;
        [SerializeField] private Button openButton;
        [SerializeField] private Button givePillsButton;
        [SerializeField] private SelectPillPopup pillPopup;
        [SerializeField] private GameObject resultsPopup;

        public Button OpenButton => openButton;
        public Button GivePillsButton => givePillsButton;
        public SelectPillPopup PillPopup => pillPopup;
        public bool IsConfigured => safeArea != null && statusText != null && errorText != null &&
            (gameObject.scene.name == "Home"
                ? openButton != null && resultsPopup != null
                : givePillsButton != null && pillPopup != null && pillPopup.IsConfigured);

        private void Awake() => HideAll();

        private void Update()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            Rect area = Screen.safeArea;
            safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        }

        public void HideAll()
        {
            if (openButton != null) openButton.gameObject.SetActive(false);
            if (givePillsButton != null) givePillsButton.gameObject.SetActive(false);
            if (pillPopup != null) pillPopup.gameObject.SetActive(false);
            if (resultsPopup != null) resultsPopup.SetActive(false);
            errorText.gameObject.SetActive(false);
            statusText.gameObject.SetActive(false);
        }

        public void ShowPreparation(int day)
        {
            HideAll(); Status(day, "Chuẩn bị mở cửa"); openButton.gameObject.SetActive(true);
        }

        public void ShowExamination(int day)
        {
            HideAll(); Status(day, "Khám bệnh"); givePillsButton.gameObject.SetActive(true);
        }

        public void ShowPillSelection(int day)
        {
            HideAll(); Status(day, "Cho thuốc"); pillPopup.gameObject.SetActive(true);
        }

        public void ShowResults() { HideAll(); resultsPopup.SetActive(true); }

        public void ShowError(string message)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }

        private void Status(int day, string step)
        {
            statusText.text = $"Ngày {day} — {step}";
            statusText.gameObject.SetActive(true);
        }
    }
}
