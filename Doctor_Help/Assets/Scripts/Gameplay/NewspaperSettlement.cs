using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class NewspaperSettlement : MonoBehaviour
    {
        [SerializeField] private GameObject summary, reportHeadbar;
        [SerializeField] private Text totalLoss, totalEarn, balance, day;
        [SerializeField] private RectTransform flightLayer;
        [SerializeField] private Text flyingGoldPrefab;
        [SerializeField] private Color gainBalanceColor = new Color(.59f, .94f, .48f);
        [SerializeField] private Color lossBalanceColor = new Color(1f, .42f, .4f);
        [SerializeField, Min(0)] private float revealDelay = .18f, countSeconds = .65f, flightSeconds = .42f, balanceSeconds = .7f;

        private Color neutralBalanceColor;
        private bool paused, unfocused;

        public bool IsConfigured => summary != null && reportHeadbar != null && totalLoss != null && totalEarn != null &&
            balance != null && day != null && flightLayer != null && flyingGoldPrefab != null;

        private void Awake() => neutralBalanceColor = balance.color;

        public void Configure(DayTreatmentResult result, bool showHeadbar)
        {
            summary.SetActive(result != null);
            reportHeadbar.SetActive(showHeadbar);
            if (result == null) return;

            totalLoss.text = $"Total Loss  {result.TotalLoss}";
            totalEarn.text = $"Total Earn  {result.TotalEarn}";
            if (showHeadbar)
            {
                balance.text = result.GoldBefore.ToString();
                balance.color = neutralBalanceColor;
                day.text = $"DAY {result.Day + 1}";
            }
        }

        public async Task PlayAsync(DayTreatmentResult result, IReadOnlyList<NewspaperPatientRow> rows, ScrollRect scroll, CancellationToken token)
        {
            var scrollbar = scroll.verticalScrollbar;
            bool scrollbarWasInteractable = scrollbar != null && scrollbar.interactable;
            bool wasVertical = scroll.vertical;
            var flights = new List<(Text label, Text source, Vector3 origin)>();
            scroll.StopMovement();
            if (scrollbar != null) scrollbar.interactable = false;
            scroll.vertical = false;
            try
            {
                await AnimateAsync(revealDelay, null, token);
                await AnimateAsync(countSeconds, fraction =>
                {
                    foreach (var row in rows) row.ShowGold(Interpolate(0, row.GoldDelta, fraction));
                }, token);

                // Reveal each group so every amount, including those below the fold, has a visible origin.
                int first = 0;
                while (first < rows.Count)
                {
                    RevealAmount(scroll, rows[first].GoldAmount);
                    int next = first;
                    while (next < rows.Count)
                    {
                        var amount = rows[next].GoldAmount;
                        var origin = TextCenter(amount);
                        var viewportPoint = scroll.viewport.InverseTransformPoint(origin);
                        if (next != first && !scroll.viewport.rect.Contains(viewportPoint)) break;
                        var fly = Instantiate(flyingGoldPrefab, flightLayer, false);
                        fly.text = amount.text;
                        fly.color = rows[next].GoldDelta > 0 ? gainBalanceColor : lossBalanceColor;
                        fly.rectTransform.position = origin;
                        amount.canvasRenderer.SetAlpha(0);
                        flights.Add((fly, amount, origin));
                        next++;
                    }

                    var target = TextCenter(balance);
                    await AnimateAsync(flightSeconds, fraction =>
                    {
                        foreach (var flight in flights)
                            flight.label.rectTransform.position = Vector3.Lerp(flight.origin, target, fraction);
                    }, token);
                    ClearFlights(flights);
                    first = next;
                }
                scroll.verticalNormalizedPosition = 1;

                if (result.NetGold != 0)
                {
                    balance.color = result.NetGold > 0 ? gainBalanceColor : lossBalanceColor;
                    await AnimateAsync(balanceSeconds,
                        fraction => balance.text = Interpolate(result.GoldBefore, result.GoldAfter, fraction).ToString(), token);
                }
            }
            finally
            {
                ClearFlights(flights);
                if (scroll != null) scroll.vertical = wasVertical;
                if (scrollbar != null) scrollbar.interactable = scrollbarWasInteractable;
                if (balance != null) balance.text = result.GoldAfter.ToString();
            }
        }

        private static void RevealAmount(ScrollRect scroll, Text amount)
        {
            Canvas.ForceUpdateCanvases();
            var viewport = scroll.viewport;
            var origin = viewport.InverseTransformPoint(TextCenter(amount));
            float extent = scroll.content.rect.height - viewport.rect.height;
            if (extent <= 0 || viewport.rect.Contains(origin)) return;
            float currentOffset = (1 - Mathf.Clamp01(scroll.verticalNormalizedPosition)) * extent;
            float targetY = viewport.rect.yMax - amount.rectTransform.rect.height * .5f - 5;
            scroll.verticalNormalizedPosition = 1 - Mathf.Clamp01((currentOffset + targetY - origin.y) / extent);
            Canvas.ForceUpdateCanvases();
        }

        private static Vector3 TextCenter(Text text)
        {
            var rect = text.rectTransform;
            float width = Mathf.Min(text.preferredWidth, rect.rect.width);
            return rect.TransformPoint(new Vector3(rect.rect.xMin + width * .5f, rect.rect.center.y));
        }

        private async Task AnimateAsync(float seconds, Action<float> draw, CancellationToken token)
        {
            for (float elapsed = 0; elapsed < seconds;)
            {
                draw?.Invoke(elapsed / seconds);
                await Awaitable.NextFrameAsync(token);
                if (!paused && !unfocused) elapsed += Time.unscaledDeltaTime;
            }
            token.ThrowIfCancellationRequested();
            draw?.Invoke(1);
        }

        private static int Interpolate(int from, int to, float t) =>
            (int)Math.Round(from + (to - (double)from) * t, MidpointRounding.AwayFromZero);

        private static void ClearFlights(List<(Text label, Text source, Vector3 origin)> flights)
        {
            foreach (var flight in flights)
            {
                if (flight.source != null) flight.source.canvasRenderer.SetAlpha(1);
                if (flight.label != null)
                {
                    flight.label.gameObject.SetActive(false);
                    Destroy(flight.label.gameObject);
                }
            }
            flights.Clear();
        }

        private void OnApplicationPause(bool value) => paused = value;
        private void OnApplicationFocus(bool focused) => unfocused = !focused;
    }
}
