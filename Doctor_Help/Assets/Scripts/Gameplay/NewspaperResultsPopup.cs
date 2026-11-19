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
        // Report warnings share the dirt line's font, size and color; extra lines are cloned from it.
        private Text coldClinicFeedback, clientLimitFeedback;
        private const string ColdClinicFeedbackText = "The clinic is cold! (ghost)";
        private const string ClientLimitFeedbackText = "Patient limit reached! (low rating)";
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
                ShowFeedback(showPatients && result.HasDirtyClinicFeedback, showPatients && result.HasColdClinicFeedback,
                    showPatients && result.ClientLimitReached);
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

        // Visible warning lines stack downward from the dirt line's slot; the patient list starts below the last one.
        private void ShowFeedback(bool dirt, bool cold, bool clientLimit)
        {
            var source = dirtyClinicFeedback.rectTransform;
            float lineHeight = source.anchorMax.y - source.anchorMin.y;
            int shown = 0;
            Text lowest = null;
            PlaceFeedback(dirtyClinicFeedback, dirt, lineHeight, ref shown, ref lowest);
            if (cold) coldClinicFeedback ??= CloneFeedback("ColdClinicFeedback", ColdClinicFeedbackText);
            if (coldClinicFeedback != null) PlaceFeedback(coldClinicFeedback, cold, lineHeight, ref shown, ref lowest);
            if (clientLimit) clientLimitFeedback ??= CloneFeedback("ClientLimitFeedback", ClientLimitFeedbackText);
            if (clientLimitFeedback != null) PlaceFeedback(clientLimitFeedback, clientLimit, lineHeight, ref shown, ref lowest);

            var listRect = (RectTransform)list.transform;
            var anchor = patientListAnchorMax ??= listRect.anchorMax;
            if (lowest != null)
                anchor.y = Mathf.Min(anchor.y, lowest.rectTransform.anchorMin.y - .006f);
            listRect.anchorMax = anchor;
        }

        private void PlaceFeedback(Text line, bool visible, float lineHeight, ref int shown, ref Text lowest)
        {
            line.gameObject.SetActive(visible);
            if (!visible) return;
            var source = dirtyClinicFeedback.rectTransform;
            var offset = new Vector2(0f, lineHeight * shown);
            line.rectTransform.anchorMin = dirtySlotMin ??= source.anchorMin;
            line.rectTransform.anchorMax = dirtySlotMax ??= source.anchorMax;
            line.rectTransform.anchorMin -= offset;
            line.rectTransform.anchorMax -= offset;
            shown++;
            lowest = line;
        }
        private Vector2? dirtySlotMin, dirtySlotMax;

        private Text CloneFeedback(string name, string message)
        {
            dirtySlotMin ??= dirtyClinicFeedback.rectTransform.anchorMin;
            dirtySlotMax ??= dirtyClinicFeedback.rectTransform.anchorMax;
            var line = Instantiate(dirtyClinicFeedback, dirtyClinicFeedback.transform.parent, false);
            line.name = name;
            line.text = message;
            return line;
        }
        private void ClearRows()
        {
            foreach (var row in rows) if (row != null) { row.gameObject.SetActive(false); Destroy(row.gameObject); }
            rows.Clear();
        }
        private void OnDisable() => presentation?.Cancel();
    }
}
