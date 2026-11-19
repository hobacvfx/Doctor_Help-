using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class OpeningComicView : MonoBehaviour
    {
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private Button tapSurface;
        [SerializeField] private CanvasGroup[] reveals;
        [SerializeField] private CanvasGroup firstPage;
        [SerializeField, Min(0)] private float fadeSeconds = .4f;
        private bool advance;
        internal bool IsConfigured => reveals != null && reveals.Length == 4 && Array.TrueForAll(reveals, reveal => reveal != null) &&
            firstPage != null && safeArea != null && tapSurface != null;

        internal async Task PlayAsync(CancellationToken token)
        {
            foreach (var reveal in reveals) reveal.alpha = 0;
            firstPage.alpha = 1;
            tapSurface.onClick.AddListener(Advance);
            gameObject.SetActive(true);
            ScreenSafeArea.ApplyTo(safeArea);
            try
            {
                for (int index = 0; index < reveals.Length; index++)
                {
                    if (index == reveals.Length - 1) firstPage.alpha = 0;
                    tapSurface.interactable = false;
                    await FadeInAsync(reveals[index], token);

                    advance = false;
                    tapSurface.interactable = true;
                    while (!advance) await Awaitable.NextFrameAsync(token);
                }
            }
            finally
            {
                if (this != null)
                {
                    tapSurface.onClick.RemoveListener(Advance);
                    gameObject.SetActive(false);
                }
            }
        }

        private async Task FadeInAsync(CanvasGroup reveal, CancellationToken token)
        {
            float elapsed = 0;
            while (elapsed < fadeSeconds)
            {
                await Awaitable.NextFrameAsync(token);
                elapsed += Time.unscaledDeltaTime;
                reveal.alpha = Mathf.Clamp01(elapsed / fadeSeconds);
            }
            reveal.alpha = 1;
        }

        private void Advance() => advance = true;
        private void LateUpdate() => ScreenSafeArea.ApplyTo(safeArea);
    }
}
