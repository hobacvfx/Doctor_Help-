using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DoctorHelp
{
    public sealed class SceneTransitionFade : MonoBehaviour
    {
        [SerializeField] private CanvasGroup cover;
        [SerializeField, Min(0)] private float fadeSeconds = 1;
        private CancellationTokenSource fadeLifetime;
        private bool paused, unfocused, skipFrame;
        public bool IsConfigured => cover != null;
        public Task CoverAsync(CancellationToken token) => FadeAsync(1, token);
        public Task RevealAsync(CancellationToken token) => FadeAsync(0, token);
        public void Clear()
        {
            fadeLifetime?.Cancel();
            cover.alpha = 0; cover.blocksRaycasts = false;
        }
        private async Task FadeAsync(float target, CancellationToken token)
        {
            fadeLifetime?.Cancel();
            var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
            fadeLifetime = lifetime;
            cover.blocksRaycasts = true;
            float start = cover.alpha, elapsed = 0;
            try
            {
                while (elapsed < fadeSeconds && !Mathf.Approximately(start, target))
                {
                    await Awaitable.NextFrameAsync(lifetime.Token);
                    if (!paused && !unfocused && !skipFrame) elapsed += Time.unscaledDeltaTime;
                    cover.alpha = Mathf.Lerp(start, target, fadeSeconds > 0 ? elapsed / fadeSeconds : 1);
                }
                lifetime.Token.ThrowIfCancellationRequested();
                cover.alpha = target;
                cover.blocksRaycasts = target > 0;
            }
            finally
            {
                if (ReferenceEquals(fadeLifetime, lifetime)) fadeLifetime = null;
                lifetime.Dispose();
            }
        }
        private void OnApplicationPause(bool value) { if (paused != value) skipFrame = true; paused = value; }
        private void OnApplicationFocus(bool focused) { if (unfocused == focused) skipFrame = true; unfocused = !focused; }
        private void LateUpdate() => skipFrame = false;
        private void OnDisable() => fadeLifetime?.Cancel();
    }
}
