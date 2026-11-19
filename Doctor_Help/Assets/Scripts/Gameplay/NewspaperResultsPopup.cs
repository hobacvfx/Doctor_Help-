using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class NewspaperResultsPopup : MonoBehaviour
    {
        [SerializeField] private Image paper;
        [SerializeField] private Text edition;
        [SerializeField] private Text article;
        [SerializeField] private Text dirtyClinicFeedback;
        [SerializeField, TextArea(2, 6)] private string articleContent =
            "Yesterday in London, several people were rushed to hospital with suspected poisoning...";
        [SerializeField, TextArea(2, 6)] private string successArticleContent =
            "Recently, many people have been miraculously saved. An anonymous heroic doctor is quietly helping those in need...";
        [SerializeField] private string morningEdition;
        [SerializeField, TextArea(2, 6)] private string morningArticle;
        [SerializeField, TextArea(2, 6)] private string emptyDayArticle;
        [SerializeField] private ScrollRect list;
        [SerializeField] private NewspaperPatientRow rowPrefab;
        [SerializeField] private NewspaperPortraitCatalog portraits;
        [SerializeField] private Button continueButton;
        [SerializeField] private NewspaperSettlement settlement;
        private readonly List<NewspaperPatientRow> rows = new List<NewspaperPatientRow>();
        private TaskCompletionSource<bool> dismissal;
        private CancellationTokenSource presentation;
        private Vector2? patientListAnchorMax;
        public bool IsConfigured => paper != null && paper.sprite != null && edition != null && article != null && list != null && list.content != null &&
            dirtyClinicFeedback != null && rowPrefab != null && portraits != null &&
            continueButton != null && settlement != null && settlement.IsConfigured;

        // A story system may provide a different article before showing the next report.
        public string ArticleContent { get => articleContent; set => articleContent = value ?? ""; }
        public string SuccessArticleContent { get => successArticleContent; set => successArticleContent = value ?? ""; }

        public Task ShowAsync(DayTreatmentResult result, CancellationToken token) =>
            PresentAsync(result ?? throw new ArgumentNullException(nameof(result)), true, token);

        public Task ShowLatestAsync(DayTreatmentResult latest, CancellationToken token) => PresentAsync(latest, false, token);

        private async Task PresentAsync(DayTreatmentResult result, bool showSettlement, CancellationToken token)
        {
            if (!IsConfigured) throw new InvalidOperationException("Newspaper popup references are incomplete.");
            if (dismissal != null) throw new InvalidOperationException("Newspaper is already open.");
            token.ThrowIfCancellationRequested();
            var pending = new TaskCompletionSource<bool>();
            dismissal = pending;
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token, destroyCancellationToken);
            presentation = lifetime;
            try
            {
                ClearRows();
                edition.text = result == null ? morningEdition : $"LONDON • DAY {result.Day} REPORT";
                bool showPatients = result != null && result.HasCompletedConsultations;
                bool animate = showSettlement && showPatients;
                article.text = result == null ? morningArticle : !showPatients ? emptyDayArticle :
                    result.IsSuccess ? successArticleContent : articleContent;
                ShowDirtFeedback(showPatients && result.HasDirtyClinicFeedback);
                list.gameObject.SetActive(showPatients);
                if (showPatients)
                {
                    foreach (var patient in result.Patients)
                    {
                        var row = Instantiate(rowPrefab, list.content, false);
                        rows.Add(row);
                        row.Bind(patient, portraits, list.viewport, animate);
                    }
                }
                gameObject.SetActive(true);
                settlement.Configure(result, showSettlement);
                continueButton.interactable = !animate;
                continueButton.onClick.AddListener(Continue);
                if (showPatients)
                {
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(list.content);
                    list.StopMovement(); list.verticalNormalizedPosition = 1;
                }
                if (animate)
                {
                    await settlement.PlayAsync(result, rows, list, lifetime.Token);
                    continueButton.interactable = true;
                }
                using (lifetime.Token.Register(() => pending.TrySetCanceled())) await pending.Task;
                lifetime.Token.ThrowIfCancellationRequested();
            }
            finally
            {
                if (continueButton != null) continueButton.onClick.RemoveListener(Continue);
                dismissal = null;
                presentation = null;
                if (this != null) { gameObject.SetActive(false); ClearRows(); }
            }
        }
        private void Continue()
        {
            Debug.Log("[Test Ads] Newspaper Continue clicked; releasing results presentation.");
            continueButton.interactable = false; dismissal?.TrySetResult(true);
        }

        private void ShowDirtFeedback(bool visible)
        {
            dirtyClinicFeedback.gameObject.SetActive(visible);
            var listRect = (RectTransform)list.transform;
            var anchor = patientListAnchorMax ??= listRect.anchorMax;
            if (visible)
                anchor.y = Mathf.Min(anchor.y, dirtyClinicFeedback.rectTransform.anchorMin.y - .006f);
            listRect.anchorMax = anchor;
        }
        private void ClearRows()
        {
            foreach (var row in rows) if (row != null) { row.gameObject.SetActive(false); Destroy(row.gameObject); }
            rows.Clear();
        }
        private void OnDisable() => presentation?.Cancel();
    }
}
