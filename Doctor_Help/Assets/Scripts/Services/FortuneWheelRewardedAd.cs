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
    internal enum RewardedPlacement { FortuneWheel, DartRefill }
    internal interface IFortuneWheelAd
    {
        bool IsReady { get; }
        Task<bool> ShowAsync(RewardedPlacement placement, CancellationToken cancellation);
    }

    // Uses the existing SDK initialization and Google's rewarded TEST unit, like the project's
    // experimental interstitial. Fortune Wheel and Dart own their UI; only this ad service persists.
    public sealed class FortuneWheelRewardedAd : MonoBehaviour, IFortuneWheelAd
    {
        public const string AndroidSampleRewardedId = "ca-app-pub-3940256099942544/5224354917";
        private readonly ConcurrentQueue<Action> callbacks = new();
        private PlayerProfileService profile;
        private DayEndInterstitialService initialization;
        private volatile bool disposed;
        private long profileRevision;
#if UNITY_ANDROID || UNITY_EDITOR
        private RewardedAd ready, showing;
        private bool loading;
        private float retryIn, retryDelay = 5;
        private double loadedAt, loadAt;
        private int loadGeneration;
        private Attempt attempt;
        private sealed class Attempt
        {
            internal readonly TaskCompletionSource<bool> completion = new();
            internal bool earned, credited, invalidated, opened;
            internal double closedAt = -1;
            internal float openingSeconds;
            internal long profileRevision;
            internal RewardedPlacement placement;
        }
#endif
        bool IFortuneWheelAd.IsReady
        {
            get
            {
#if UNITY_ANDROID || UNITY_EDITOR
                return !disposed && attempt == null && ready != null &&
                    Time.realtimeSinceStartupAsDouble - loadedAt < 3300 && ready.CanShowAd();
#else
                return false;
#endif
            }
        }
        private void Start()
        {
            profile = PlayerProfileService.Instance;
            if (profile != null) profile.ProfileReset += Invalidate;
            initialization = GetComponent<DayEndInterstitialService>();
        }
        private void Invalidate()
        {
            profileRevision++;
#if UNITY_ANDROID || UNITY_EDITOR
            if (attempt != null) attempt.invalidated = true;
#endif
        }
        private void Update()
        {
            while (callbacks.TryDequeue(out var callback)) callback();
#if UNITY_ANDROID || UNITY_EDITOR
            if (disposed) return;
            if (attempt != null)
            {
                // Reward and close callbacks can be delivered in the same batch in either order.
                if (attempt.closedAt >= 0 && Time.realtimeSinceStartupAsDouble - attempt.closedAt >= .2)
                    Finish();
                else if (!attempt.opened && Application.isFocused)
                {
                    attempt.openingSeconds += Time.unscaledDeltaTime;
                    if (attempt.openingSeconds > 15) Finish();
                }
            }
            if (ready != null && Time.realtimeSinceStartupAsDouble - loadedAt >= 3300)
            { ready.Destroy(); ready = null; }
            if (loading && Time.realtimeSinceStartupAsDouble - loadAt > 45)
            { loadGeneration++; loading = false; retryIn = retryDelay; retryDelay = Mathf.Min(60, retryDelay * 2); }
            if (initialization == null || !initialization.IsInitialized || loading || ready != null || attempt != null) return;
            retryIn -= Time.unscaledDeltaTime;
            if (retryIn <= 0) Load();
#endif
        }
#if UNITY_ANDROID || UNITY_EDITOR
        private void Load()
        {
            loading = true;
            loadAt = Time.realtimeSinceStartupAsDouble;
            int request = ++loadGeneration;
            try
            {
                RewardedAd.Load(AndroidSampleRewardedId, new AdRequest(), (ad, error) =>
                {
                    if (disposed) { ad?.Destroy(); return; }
                    callbacks.Enqueue(() =>
                    {
                        if (disposed || request != loadGeneration) { ad?.Destroy(); return; }
                        loading = false;
                        if (error != null || ad == null)
                        {
                            ad?.Destroy();
                            retryIn = retryDelay;
                            retryDelay = Mathf.Min(60, retryDelay * 2);
                            Debug.LogWarning("[Test Ads] Rewarded ad unavailable: " + error);
                            return;
                        }
                        ready = ad;
                        loadedAt = Time.realtimeSinceStartupAsDouble;
                        retryDelay = 5;
                    });
                });
            }
            catch (Exception error)
            {
                loading = false;
                retryIn = retryDelay;
                retryDelay = Mathf.Min(60, retryDelay * 2);
                Debug.LogWarning("[Test Ads] Rewarded load failed: " + error);
            }
        }

        private void Earn(Attempt current)
        {
            if (disposed || current.invalidated || current.earned || current.profileRevision != profileRevision) return;
            current.earned = true; // Multiple native reward callbacks still grant exactly one placement reward.
            current.credited = profile != null && (current.placement == RewardedPlacement.DartRefill ? profile.TryEarnDartAdReward() : profile.TryEarnFortuneAdSpin());
        }
        private void Finish()
        {
            var current = attempt;
            attempt = null;
            showing?.Destroy();
            showing = null;
            current?.completion.TrySetResult(current.credited && !current.invalidated);
            retryIn = 0;
        }
#endif
        async Task<bool> IFortuneWheelAd.ShowAsync(RewardedPlacement placement, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
#if UNITY_ANDROID || UNITY_EDITOR
            if (!((IFortuneWheelAd)this).IsReady) return false;
            var current = new Attempt { profileRevision = profileRevision, placement = placement };
            attempt = current;
            showing = ready;
            ready = null;
            showing.OnAdFullScreenContentOpened += () => callbacks.Enqueue(() => current.opened = true);
            showing.OnAdFullScreenContentClosed += () => callbacks.Enqueue(() => current.closedAt = Time.realtimeSinceStartupAsDouble);
            showing.OnAdFullScreenContentFailed += error => callbacks.Enqueue(() =>
            {
                Debug.LogWarning("[Test Ads] Rewarded show failed: " + error);
                current.closedAt = Time.realtimeSinceStartupAsDouble;
            });
            try { showing.Show(_ => callbacks.Enqueue(() => Earn(current))); }
            catch (Exception error)
            {
                Debug.LogWarning("[Test Ads] Rewarded show failed: " + error);
                Finish();
            }
            using (cancellation.Register(() => current.completion.TrySetCanceled()))
                return await current.completion.Task;
#else
            await Task.CompletedTask;
            return false;
#endif
        }
        private void OnDestroy()
        {
            disposed = true;
            if (profile != null) profile.ProfileReset -= Invalidate;
#if UNITY_ANDROID || UNITY_EDITOR
            attempt?.completion.TrySetCanceled();
            attempt = null;
            ready?.Destroy(); ready = null;
            showing?.Destroy(); showing = null;
#endif
            while (callbacks.TryDequeue(out var callback)) callback();
        }
    }
}
