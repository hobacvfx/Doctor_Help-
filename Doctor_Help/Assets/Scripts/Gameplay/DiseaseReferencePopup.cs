using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class DiseaseReferencePopup : MonoBehaviour
    {
        [SerializeField] private AllSickness medicalData;
        [SerializeField] private SymptomReferenceArt referenceArt;
        [SerializeField] private DiseaseReferenceRow rowPrefab;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private VerticalLayoutGroup listLayout;
        [SerializeField] private Button closeButton, outsideButton;
        [SerializeField] private Text countText, errorText;
        [SerializeField, Min(6)] private int visibleRows = 6;
        [SerializeField] private string countFormat = "{0} conditions  •  Scroll to browse";
        [SerializeField] private string unavailableMessage = "The casebook is unavailable.";
        private readonly List<DiseaseReferenceRow> rows = new List<DiseaseReferenceRow>();
        private bool initialized;
        private Vector2 viewportSize;
        private float lastScrollPosition = 1;
        public bool IsOpen => gameObject.activeSelf;
        internal Button CloseButton => closeButton;
        internal RectTransform ReadingArea => scroll.viewport;

        internal RectTransform TutorialDisease(string id, bool reveal)
        {
            if (!IsOpen || !scroll.gameObject.activeInHierarchy) return null;
            for (int i = 0; i < rows.Count && i < medicalData.Sicknesses.Count; i++)
            {
                if (!rows[i].gameObject.activeInHierarchy || medicalData.Sicknesses[i].Id != id) continue;
                var row = (RectTransform)rows[i].transform;
                if (reveal)
                {
                    Canvas.ForceUpdateCanvases();
                    scroll.StopMovement();
                    float travel = scroll.content.rect.height - scroll.viewport.rect.height;
                    if (travel > 0)
                    {
                        float center = scroll.viewport.InverseTransformPoint(row.TransformPoint(row.rect.center)).y;
                        scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition +
                            (center - scroll.viewport.rect.center.y) / travel);
                        Canvas.ForceUpdateCanvases();
                    }
                }
                return row;
            }
            return null;
        }

        private void Awake() => Initialize();
        private void Initialize()
        {
            if (initialized) return;
            if (medicalData == null || medicalData.SymptomProfile == null || referenceArt == null || rowPrefab == null ||
                scroll == null || scroll.content == null || scroll.viewport == null || listLayout == null ||
                closeButton == null || closeButton.targetGraphic == null || outsideButton == null ||
                outsideButton.targetGraphic == null || countText == null || errorText == null)
                throw new InvalidOperationException("DiseaseReferencePopup requires complete scene/prefab references.");
            closeButton.onClick.AddListener(Close);
            outsideButton.onClick.AddListener(Close);
            if (rows.Count == 0) rows.AddRange(scroll.content.GetComponentsInChildren<DiseaseReferenceRow>(true));
            initialized = true;
        }

        public void Open()
        {
            if (gameObject.activeSelf) Refresh();
            else gameObject.SetActive(true);
        }
        public void Close() => gameObject.SetActive(false);
        private void OnEnable() => Refresh();
        private void OnDisable()
        {
            if (scroll != null) { lastScrollPosition = scroll.verticalNormalizedPosition; scroll.StopMovement(); }
        }
        public void Refresh()
        {
            try
            {
                Initialize(); referenceArt.ValidateFor(medicalData.SymptomProfile);
                var seen = new HashSet<string>();
                for (int i = 0; i < medicalData.Sicknesses.Count; i++)
                {
                    var disease = medicalData.Sicknesses[i];
                    if (disease == null || string.IsNullOrWhiteSpace(disease.Id) || !seen.Add(disease.Id))
                        throw new InvalidOperationException("Casebook disease catalogue contains an empty or duplicate disease ID.");
                    if (i >= rows.Count) rows.Add(Instantiate(rowPrefab, scroll.content, false));
                    rows[i].gameObject.SetActive(true);
                    rows[i].Bind(disease, i + 1, medicalData.SymptomProfile, referenceArt);
                }
                for (int i = medicalData.Sicknesses.Count; i < rows.Count; i++) rows[i].gameObject.SetActive(false);
                scroll.gameObject.SetActive(true); errorText.gameObject.SetActive(false);
                countText.text = string.Format(countFormat, medicalData.Sicknesses.Count);
                Canvas.ForceUpdateCanvases(); FitRows(); Canvas.ForceUpdateCanvases();
                scroll.StopMovement(); scroll.verticalNormalizedPosition = lastScrollPosition;
            }
            catch (Exception exception)
            {
                if (scroll != null) scroll.gameObject.SetActive(false);
                if (errorText != null) { errorText.text = unavailableMessage; errorText.gameObject.SetActive(true); }
                Debug.LogException(exception, this);
            }
        }

        private void LateUpdate()
        {
            if (scroll != null && scroll.viewport != null && scroll.viewport.rect.size != viewportSize) FitRows();
        }
        private void FitRows()
        {
            viewportSize = scroll.viewport.rect.size;
            float height = Mathf.Max(1, (viewportSize.y - listLayout.padding.vertical - listLayout.spacing * (visibleRows - 1)) / Mathf.Max(6, visibleRows));
            foreach (var row in rows)
            {
                var element = row.GetComponent<LayoutElement>();
                element.minHeight = element.preferredHeight = height;
                element.flexibleHeight = 0;
            }
            LayoutRebuilder.MarkLayoutForRebuild(scroll.content);
        }
        private void OnDestroy()
        {
            if (!initialized) return;
            closeButton.onClick.RemoveListener(Close); outsideButton.onClick.RemoveListener(Close);
        }
    }
}
