using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_ANDROID || UNITY_EDITOR
using GoogleMobileAds.Api;
#endif

namespace DoctorHelp
{
    public interface IDayEndAdvertisement
    {
        Task ShowAfterResultsAsync(int completedDay, CancellationToken token);
    }

    // Experimental builds deliberately have no production IDs or production-mode switch.
    public sealed class DayEndInterstitialService : MonoBehaviour, IDayEndAdvertisement
    {
        public const string AndroidSampleAppId = "ca-app-pub-3940256099942544~3347511713";
        public const string AndroidSampleInterstitialId = "ca-app-pub-3940256099942544/1033173712";
        private int lastAttemptedDay;
        internal bool IsInitialized
        {
            get
            {
#if UNITY_ANDROID || UNITY_EDITOR
                return initialized;
#else
                return false;
#endif
            }
        }
#if UNITY_ANDROID || UNITY_EDITOR
        private readonly ConcurrentQueue<Action> callbacks = new ConcurrentQueue<Action>();
        private InterstitialAd readyAd, showingAd;
        private TaskCompletionSource<bool> dismissal;
        private bool initialized, loading, paused, unfocused, opened;
        private volatile bool disposed;
        private float retryIn, retryDelay = 5, openingSeconds;
        private double loadedAt, initializeAt, loadAt;
        private bool initializationPending, initializationStallLogged, loadStallLogged;
        private const float PlacementWaitSeconds = 4;

        private void Start()
        {
            // Marshal callbacks ourselves so a dismiss/failure callback can be received
            // even when the native fullscreen activity has paused the Unity player.
            MobileAds.RaiseAdEventsOnUnityMainThread = false;
            initializationPending = true; initializeAt = Time.realtimeSinceStartupAsDouble;
            try
            {
                Debug.Log($"[Test Ads] Initializing Google Mobile Ads: platform={Application.platform}, editor={Application.isEditor}, sampleUnit={AndroidSampleInterstitialId}.");
                MobileAds.Initialize(status => callbacks.Enqueue(() =>
                {
                    if (disposed) return;
                    initializationPending = false;
                    if (status == null)
                    {
                        Debug.LogWarning("[Test Ads] SDK initialization returned no status.");
                        return;
                    }
                    initialized = true;
                    foreach (var adapter in status.getAdapterStatusMap())
                        Debug.Log($"[Test Ads] Adapter {adapter.Key}: state={adapter.Value.InitializationState}, latency={adapter.Value.Latency}, description={adapter.Value.Description}");
                    Debug.Log("[Test Ads] SDK initialized; preloading sample interstitial.");
                    Load();
                }));
            }
            catch (Exception error) { initializationPending = false; Debug.LogWarning("[Test Ads] Initialization failed: " + error); }
        }

        private void Update()
        {
            while (callbacks.TryDequeue(out var callback)) callback();
            if (initializationPending && !initializationStallLogged && Time.realtimeSinceStartupAsDouble - initializeAt > 30)
            { initializationStallLogged = true; Debug.LogWarning("[Test Ads] Still waiting for SDK initialization callback after 30s."); }
            if (loading && !loadStallLogged && Time.realtimeSinceStartupAsDouble - loadAt > 30)
            { loadStallLogged = true; Debug.LogWarning("[Test Ads] Still waiting for interstitial load callback after 30s."); }
            if (!initialized || disposed || paused || unfocused) return;
            if (dismissal != null && !opened)
            {
                openingSeconds += Time.unscaledDeltaTime;
                if (openingSeconds >= 10)
                {
                    Debug.LogWarning("[Test Ads] Fullscreen open callback timed out.");
                    FinishShowing();
                }
            }
            if (readyAd != null && Time.realtimeSinceStartupAsDouble - loadedAt >= 3300)
            { readyAd.Destroy(); readyAd = null; }
            if (readyAd == null && showingAd == null && !loading)
            {
                retryIn -= Time.unscaledDeltaTime;
                if (retryIn <= 0) Load();
            }
        }

        private void Load()
        {
            if (!initialized || loading || disposed || readyAd != null || showingAd != null) return;
            loading = true; loadAt = Time.realtimeSinceStartupAsDouble; loadStallLogged = false;
            Debug.Log($"[Test Ads] Requesting sample interstitial; network={Application.internetReachability}.");
            try
            {
                InterstitialAd.Load(AndroidSampleInterstitialId, new AdRequest(), (ad, error) =>
                {
                    // A late native callback must release its ad even after this service is destroyed.
                    if (disposed) { ad?.Destroy(); return; }
                    callbacks.Enqueue(() =>
                    {
                        loading = false;
                        if (disposed) { ad?.Destroy(); return; }
                        if (error != null || ad == null)
                        {
                            ad?.Destroy();
                            retryIn = retryDelay;
                            retryDelay = Mathf.Min(60, retryDelay * 2);
                            Debug.LogWarning("[Test Ads] Interstitial unavailable; gameplay continues. retryIn=" + retryIn + "s; " + error);
                            return;
                        }
                        readyAd = ad;
                        loadedAt = Time.realtimeSinceStartupAsDouble;
                        retryDelay = 5;
                        Debug.Log("[Test Ads] Sample interstitial loaded. Response: " + ad.GetResponseInfo());
                    });
                });
            }
            catch (Exception error)
            {
                loading = false; retryIn = retryDelay; retryDelay = Mathf.Min(60, retryDelay * 2);
                Debug.LogWarning("[Test Ads] Load failed: " + error.ToString());
            }
        }

        private void FullScreenOpened() => callbacks.Enqueue(() =>
        {
            if (disposed || dismissal == null) return;
            opened = true;
            Debug.Log("[Test Ads] Interstitial opened.");
        });
        private void FullScreenClosed() => callbacks.Enqueue(() =>
        {
            if (disposed || dismissal == null) return;
            Debug.Log("[Test Ads] Interstitial closed.");
            FinishShowing();
        });
        private void FullScreenFailed(AdError error)
        {
            callbacks.Enqueue(() =>
            {
                if (disposed || dismissal == null) return;
                Debug.LogWarning("[Test Ads] Show failed: " + error);
                FinishShowing();
            });
        }
        private void FinishShowing()
        {
            var completion = dismissal;
            dismissal = null;
            ReleaseShowingAd();
            completion?.TrySetResult(true);
            retryIn = 0;
        }
        private void ReleaseShowingAd()
        {
            if (showingAd == null) return;
            showingAd.OnAdFullScreenContentOpened -= FullScreenOpened;
            showingAd.OnAdFullScreenContentClosed -= FullScreenClosed;
            showingAd.OnAdFullScreenContentFailed -= FullScreenFailed;
            showingAd.Destroy(); showingAd = null;
        }
        private void OnApplicationPause(bool value) { paused = value; Debug.Log($"[Test Ads] Application pause={value}."); }
        private void OnApplicationFocus(bool value) { unfocused = !value; Debug.Log($"[Test Ads] Application focus={value}."); }
        private void OnDestroy()
        {
            disposed = true;
            dismissal?.TrySetCanceled(); dismissal = null;
            readyAd?.Destroy(); readyAd = null;
            ReleaseShowingAd();
            while (callbacks.TryDequeue(out var callback)) callback();
        }
#endif

        public async Task ShowAfterResultsAsync(int completedDay, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (completedDay <= lastAttemptedDay)
            {
                Debug.Log($"[Test Ads] Day {completedDay}: placement already attempted; ignoring duplicate.");
                return;
            }
            // Consume the placement once. A late load never interrupts Preparation.
            lastAttemptedDay = completedDay;
#if UNITY_ANDROID || UNITY_EDITOR
            Debug.Log($"[Test Ads] Day {completedDay}: results dismissed, checking interstitial.");
            if (disposed || dismissal != null) return;
            if (readyAd != null &&
                (Time.realtimeSinceStartupAsDouble - loadedAt >= 3300 || !readyAd.CanShowAd()))
            { readyAd.Destroy(); readyAd = null; }
            // Give the first in-flight load a brief chance to finish at the natural
            // transition. Respect the existing backoff after a known load failure.
            double deadline = Time.realtimeSinceStartupAsDouble + PlacementWaitSeconds;
            while (!disposed && Time.realtimeSinceStartupAsDouble < deadline &&
                   (paused || unfocused || (readyAd == null && (!initialized || loading || retryIn <= 0))))
                await Awaitable.NextFrameAsync(token);
            token.ThrowIfCancellationRequested();
            if (disposed || paused || unfocused || readyAd == null || !readyAd.CanShowAd())
            {
                Debug.Log($"[Test Ads] Day {completedDay}: skipped (initialized={initialized}, loading={loading}, ready={readyAd != null}, paused={paused}, unfocused={unfocused}, disposed={disposed}).");
                return;
            }
            showingAd = readyAd; readyAd = null;
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            dismissal = completion; opened = false; openingSeconds = 0;
            showingAd.OnAdFullScreenContentOpened += FullScreenOpened;
            showingAd.OnAdFullScreenContentClosed += FullScreenClosed;
            showingAd.OnAdFullScreenContentFailed += FullScreenFailed;
            using (token.Register(() => completion.TrySetCanceled()))
            {
                try { Debug.Log("[Test Ads] Calling SDK Show()."); showingAd.Show(); }
                catch (Exception error)
                { Debug.LogWarning("[Test Ads] Show failed: " + error.ToString()); FinishShowing(); }
                await completion.Task;
            }
#else
            Debug.Log("[Test Ads] Unsupported platform; interstitial disabled.");
            await Task.CompletedTask;
#endif
        }
    }
}
