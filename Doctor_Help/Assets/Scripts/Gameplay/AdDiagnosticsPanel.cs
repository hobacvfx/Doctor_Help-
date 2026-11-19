using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    // Scene-owned prefab, available in experimental mobile builds including non-development builds.
    public sealed class AdDiagnosticsPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private GameObject panel;
        [SerializeField] private Button openButton, closeButton, copyButton;
        [SerializeField] private Text logText, feedback;
        [SerializeField] private ScrollRect scroll;
        private int displayedRevision = -1;

        private void Awake()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            copyButton.onClick.AddListener(Copy);
            panel.SetActive(false);
        }
        private void Update()
        {
            if (Screen.width > 0 && Screen.height > 0)
            {
                var area = ScreenSafeArea.Current;
                safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
                safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            }
            if (panel.activeSelf && displayedRevision != AdDiagnosticLog.Revision) Refresh();
        }
        private void Open() { panel.SetActive(true); feedback.text = "Copy includes the entire session log."; Refresh(); }
        private void Close() => panel.SetActive(false);
        private void Refresh()
        {
            displayedRevision = AdDiagnosticLog.Revision;
            string snapshot = AdDiagnosticLog.Snapshot();
            // Bound generated UI vertices; Copy always exports the full buffer.
            logText.text = snapshot.Length > 14000 ? "[Preview: latest entries; Copy includes all]\n" + snapshot.Substring(snapshot.Length - 14000) : snapshot;
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0;
        }
        private void Copy()
        {
            string snapshot = AdDiagnosticLog.Snapshot();
            GUIUtility.systemCopyBuffer = snapshot;
            feedback.text = $"Copied {snapshot.Length:N0} characters. Paste into chat.";
        }
        private void OnDestroy()
        {
            openButton.onClick.RemoveListener(Open);
            closeButton.onClick.RemoveListener(Close);
            copyButton.onClick.RemoveListener(Copy);
        }
    }
}
