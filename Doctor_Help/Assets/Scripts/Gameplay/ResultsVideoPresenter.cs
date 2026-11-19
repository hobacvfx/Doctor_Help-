using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace DoctorHelp
{
    public sealed class ResultsVideoPresenter : MonoBehaviour
    {
        [SerializeField] private VideoPlayer player;
        [SerializeField] private RawImage screen;
        [SerializeField] private AspectRatioFitter aspect;
        [SerializeField] private CanvasGroup videoGroup;
        [SerializeField] private CanvasGroup dayEndedText, laterThatNightText, newDayText;
        [SerializeField] private VideoClip successClip, failClip;
        [SerializeField] private NewspaperResultsPopup newspaper;
        [SerializeField, Min(0)] private float textFadeSeconds = 1;
        [SerializeField, Min(0)] private float narrationSeconds = 3;
        [SerializeField, Min(0)] private float videoFadeSeconds = 1;
        [SerializeField, Min(0), Tooltip("Total time of the 'new day has started' line, fades included.")]
        private float newDaySeconds = 3;
        [SerializeField, Min(1)] private float preparationTimeout = 20;
        private TaskCompletionSource<bool> preparation, playback;
        private CancellationTokenSource presentationLifetime;
        private bool applicationPaused, applicationUnfocused, videoStarted, skipFrame;
        public bool IsConfigured => player != null && screen != null && aspect != null && successClip != null && failClip != null &&
            newspaper != null && newspaper.IsConfigured && videoGroup != null && dayEndedText != null && laterThatNightText != null && newDayText != null &&
            player.renderMode == VideoRenderMode.RenderTexture && player.targetTexture != null && screen.texture == player.targetTexture;
        private bool Suspended => applicationPaused || applicationUnfocused;
        private float ForegroundDelta => Suspended || skipFrame ? 0 : Time.unscaledDeltaTime;

        public async Task PlayAsync(DayTreatmentResult result, CancellationToken token)
        {
            if (!IsConfigured) throw new InvalidOperationException("Home result cutscene references are incomplete.");
            if (presentationLifetime != null) throw new InvalidOperationException("A result cutscene is already playing.");
            token.ThrowIfCancellationRequested();
            var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
            presentationLifetime = lifetime;
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            playback = completion; preparation = ready;
            Coroutine watchdog = null;
            bool musicPaused = false;
            ResetVisuals();
            player.Stop(); videoStarted = false;
            player.prepareCompleted += Prepared;
            player.loopPointReached += Finished;
            player.errorReceived += Failed;
            try
            {
                if (!result.HasCompletedConsultations)
                {
                    await ShowTextAsync(dayEndedText, narrationSeconds, false, lifetime.Token);
                    await newspaper.ShowAsync(result, lifetime.Token);
                    await ShowTextAsync(newDayText, NewDaySeconds, false, lifetime.Token);
                    return;
                }

                using (lifetime.Token.Register(() => { ready.TrySetCanceled(); completion.TrySetCanceled(); }))
                {
                    var target = player.targetTexture;
                    if (!target.IsCreated()) target.Create();
                    var previous = RenderTexture.active;
                    try { RenderTexture.active = target; GL.Clear(true, true, Color.black); }
                    finally { RenderTexture.active = previous; }
                    player.clip = result.IsSuccess ? successClip : failClip;
                    player.controlledAudioTrackCount = (ushort)Math.Min(1, (int)player.clip.audioTrackCount);
                    if (player.controlledAudioTrackCount > 0) player.EnableAudioTrack(0, true);
                    aspect.aspectRatio = player.clip.height > 0 ? (float)player.clip.width / player.clip.height : 1;
                    player.Prepare();
                    watchdog = StartCoroutine(WatchPreparation(ready));
                    if (!await ShowTextAsync(dayEndedText, narrationSeconds, true, lifetime.Token))
                        await ShowTextAsync(laterThatNightText, narrationSeconds, true, lifetime.Token);
                    await ready.Task;
                    await WaitAsync(0, lifetime.Token);
                    musicPaused = player.controlledAudioTrackCount > 0;
                    if (musicPaused) GameAudio.Instance?.PauseMusic(true);
                    screen.enabled = true; videoStarted = true; player.Play();
                    await FadeAsync(videoGroup, 1, videoFadeSeconds, lifetime.Token);
                    await completion.Task;
                    if (musicPaused)
                    {
                        GameAudio.Instance?.PauseMusic(false);
                        musicPaused = false;
                    }
                    // Keep the final decoded frame visible while fading to black.
                    await FadeAsync(videoGroup, 0, videoFadeSeconds, lifetime.Token);
                    screen.enabled = false;
                    await newspaper.ShowAsync(result, lifetime.Token);
                    await ShowTextAsync(newDayText, NewDaySeconds, false, lifetime.Token);
                }
            }
            finally
            {
                if (musicPaused) GameAudio.Instance?.PauseMusic(false);
                if (watchdog != null && this != null) StopCoroutine(watchdog);
                if (player != null)
                {
                    player.prepareCompleted -= Prepared; player.loopPointReached -= Finished; player.errorReceived -= Failed;
                    player.Stop();
                }
                completion.TrySetCanceled(); ready.TrySetCanceled();
                videoStarted = false; playback = null; preparation = null;
                presentationLifetime = null;
                ResetVisuals(); lifetime.Dispose();
            }
        }

        private float NewDaySeconds => newDaySeconds;

        private async Task<bool> ShowTextAsync(CanvasGroup message, float duration, bool canSkip, CancellationToken token)
        {
            message.alpha = 0; message.gameObject.SetActive(true);
            float fade = Mathf.Min(textFadeSeconds, duration / 2);
            float elapsed = 0;
            try
            {
                while (elapsed < duration)
                {
                    await Awaitable.NextFrameAsync(token);
                    if (canSkip && !Suspended && !skipFrame &&
                        (Mouse.current?.leftButton.wasPressedThisFrame == true ||
                         Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true))
                        return true;
                    elapsed += ForegroundDelta;
                    message.alpha = fade > 0 ? Mathf.Clamp01(Mathf.Min(elapsed, duration - elapsed) / fade) : 1;
                }
                return false;
            }
            finally
            {
                if (message != null) message.gameObject.SetActive(false);
            }
        }
        private async Task FadeAsync(CanvasGroup group, float target, float seconds, CancellationToken token)
        {
            float start = group.alpha, elapsed = 0;
            while (elapsed < seconds)
            {
                await Awaitable.NextFrameAsync(token);
                elapsed += ForegroundDelta;
                group.alpha = Mathf.Lerp(start, target, seconds > 0 ? elapsed / seconds : 1);
            }
            token.ThrowIfCancellationRequested(); group.alpha = target;
        }
        private async Task WaitAsync(float seconds, CancellationToken token)
        {
            float elapsed = 0;
            while (elapsed < seconds || Suspended)
            {
                await Awaitable.NextFrameAsync(token);
                elapsed += ForegroundDelta;
            }
            token.ThrowIfCancellationRequested();
        }
        private IEnumerator WatchPreparation(TaskCompletionSource<bool> ready)
        {
            float elapsed = 0;
            while (!ready.Task.IsCompleted)
            {
                elapsed += ForegroundDelta;
                if (elapsed >= preparationTimeout)
                { ready.TrySetException(new TimeoutException("Không thể chuẩn bị video kết quả. Bấm Thử lại.")); yield break; }
                yield return null;
            }
        }
        private void Prepared(VideoPlayer source) => preparation?.TrySetResult(true);
        private void Finished(VideoPlayer source) => playback?.TrySetResult(true);
        private void Failed(VideoPlayer source, string message)
        {
            var error = new InvalidOperationException("Không thể phát video kết quả: " + message);
            if (preparation != null && !preparation.Task.IsCompleted) preparation.TrySetException(error);
            else playback?.TrySetException(error);
        }
        private void OnApplicationPause(bool paused)
        { if (applicationPaused != paused) skipFrame = true; applicationPaused = paused; UpdatePlayback(); }
        private void OnApplicationFocus(bool focused)
        { if (applicationUnfocused == focused) skipFrame = true; applicationUnfocused = !focused; UpdatePlayback(); }
        private void LateUpdate() => skipFrame = false;
        private void UpdatePlayback()
        {
            if (!videoStarted || playback == null || playback.Task.IsCompleted || player == null) return;
            if (Suspended) player.Pause(); else if (player.isPrepared) player.Play();
        }
        private void ResetVisuals()
        {
            if (screen != null) screen.enabled = false;
            if (videoGroup != null) videoGroup.alpha = 0;
            foreach (var message in new[] { dayEndedText, laterThatNightText, newDayText })
                if (message != null) { message.alpha = 0; message.gameObject.SetActive(false); }
        }
        public void StopPlayback()
        {
            presentationLifetime?.Cancel();
            if (player != null) player.Stop();
            ResetVisuals();
        }
        private void OnDisable() => StopPlayback();
    }
}

