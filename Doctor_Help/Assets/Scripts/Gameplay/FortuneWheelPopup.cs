using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class FortuneWheelPopup : MonoBehaviour
    {
        [SerializeField] private FortuneWheelFace face;
        [SerializeField] private RewardedAdConfirmation confirmation;
        [SerializeField] private RectTransform safeArea, content;
        [SerializeField] private CanvasGroup presentation;
        [SerializeField] private Button paidSpin, adSpin, close, settingsButton;
        [SerializeField] private Text price, message;
        [SerializeField] private Headbar headbar;
        [SerializeField] private PlayerHomeMovement player;
        private PlayerProfileService profile;
        private CancellationTokenSource lifetime;
        private readonly List<Canvas> hidden = new();
        private bool ownsWorldLock, spinning, watchingAd, paused, unfocused, skipFrame;
        private float elapsed, startAngle, endAngle;
        private int selected;
        private Camera sceneCamera;
        private int sceneCullingMask;
        private bool cameraHidden;
        internal IFortuneWheelAd AdOverride { get; set; }
        internal bool IsBusy => spinning || watchingAd;
        internal int LastRewardIndex => selected;
        public bool IsConfigured => face != null && face.IsConfigured && safeArea != null && content != null &&
            paidSpin != null && adSpin != null && close != null && price != null && message != null &&
            headbar != null && presentation != null && confirmation != null;

        public void Open()
        {
            var service = PlayerProfileService.Instance;
            if (gameObject.activeSelf || !IsConfigured || service == null || !service.CanCommitImmediately ||
                DeskActionButton.WorldBlockers != 0) return;
            gameObject.SetActive(true);
        }

        private void OnEnable()
        {
            if (!IsConfigured) { Debug.LogError("Fortune wheel is missing prefab references.", this); return; }
            profile = PlayerProfileService.Instance;
            lifetime = new CancellationTokenSource();
            paidSpin.onClick.AddListener(SpinPaid);
            adSpin.onClick.AddListener(SpinWithAd);
            close.onClick.AddListener(Close);
            if (profile != null) profile.Changed += Refresh;
            presentation.alpha = 1;
            presentation.blocksRaycasts = true;
            message.text = "";
            spinning = watchingAd = false;
            face.Refresh();
            DeskActionButton.WorldBlockers++;
            ownsWorldLock = true;
            if (player == null) player = FindFirstObjectByType<PlayerHomeMovement>();
            player?.SetActionLocked(true);
            sceneCamera = Camera.main;
            if (sceneCamera != null)
            {
                sceneCullingMask = sceneCamera.cullingMask;
                sceneCamera.cullingMask = 0;
                cameraHidden = true;
            }
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.enabled || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace ||
                    canvas.gameObject.scene != gameObject.scene || canvas.transform.IsChildOf(transform) ||
                    canvas.GetComponentInChildren<FpsOverlay>(true) != null) continue;
                canvas.enabled = false;
                hidden.Add(canvas);
            }
            Layout();
            Refresh();
        }

        private void Refresh()
        {
            if (!IsConfigured) return;
            price.text = face.Catalog.SpinPrice.ToString();
            bool available = profile != null && profile.CanCommitImmediately;
            paidSpin.interactable = available && !IsBusy;
            adSpin.interactable = available && !IsBusy;
            close.interactable = !IsBusy;
            if (settingsButton != null) settingsButton.interactable = !IsBusy;
        }

        private void SpinPaid() => BeginSpin(false);

        private async void SpinWithAd()
        {
            if (IsBusy || profile == null || !profile.CanCommitImmediately) return;
            if (profile.FortuneAdSpins > 0) { BeginSpin(true); return; }
            watchingAd = true;
            message.text = "";
            Refresh();
            bool earned = false;
            try
            {
                if (!await confirmation.AskAsync(lifetime.Token)) return;
                IFortuneWheelAd advertisement = AdOverride ?? FindFirstObjectByType<FortuneWheelRewardedAd>();
                if (advertisement == null || !advertisement.IsReady)
                { message.text = "Advertisement is loading. Please try again."; return; }
                presentation.alpha = 0;
                presentation.blocksRaycasts = false;
                earned = await advertisement.ShowAsync(RewardedPlacement.FortuneWheel, lifetime.Token);
            }
            catch (OperationCanceledException) { return; }
            catch (Exception error) { Debug.LogException(error); }
            finally
            {
                if (this != null)
                {
                    watchingAd = false;
                    presentation.alpha = 1;
                    presentation.blocksRaycasts = true;
                    Refresh();
                }
            }
            if (this == null || !isActiveAndEnabled) return;
            if (earned && profile.FortuneAdSpins > 0) BeginSpin(true);
            else message.text = "No spin earned. Complete the rewarded ad to spin.";
        }

        private void BeginSpin(bool useAd)
        {
            if (IsBusy || profile == null || !profile.CanCommitImmediately) return;
            int result = UnityEngine.Random.Range(0, FortuneWheelCatalog.SectorCount);
            if (!profile.TrySpinFortune(face.Catalog, result, useAd, out var error))
            { message.text = error; return; }
            selected = result;
            var reward = face.Catalog.Rewards[selected];
            headbar.Hold(reward.kind, reward.amount);
            startAngle = Mathf.Repeat(face.Rotor.localEulerAngles.z, 360);
            float target = FortuneWheelCatalog.TargetAngle(selected, UnityEngine.Random.Range(-9f, 9f));
            endAngle = startAngle - 360 * face.Catalog.FullTurns - Mathf.Repeat(startAngle - target, 360);
            elapsed = 0;
            spinning = true;
            message.text = "";
            Refresh();
        }

        private void Update()
        {
            if (!spinning || paused || unfocused || skipFrame) { skipFrame = false; return; }
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / face.Catalog.SpinSeconds);
            float eased = 1 - Mathf.Pow(1 - t, 3);
            face.Rotor.localRotation = Quaternion.Euler(0, 0, Mathf.LerpUnclamped(startAngle, endAngle, eased));
            if (t < 1) return;
            spinning = false;
            face.Rotor.localRotation = Quaternion.Euler(0, 0, Mathf.Repeat(endAngle, 360));
            var reward = face.Catalog.Rewards[selected];
            message.text = reward.Description;
            Vector2 origin = face.RewardScreenPoint(selected);
            headbar.PlayUiPuff(origin);
            headbar.FlyReward(reward.kind, reward.amount, origin, alreadyHeld: true);
            Refresh();
        }

        private void Layout()
        {
            ScreenSafeArea.ApplyTo(safeArea);
            float scale = Mathf.Min(safeArea.rect.width / 1080f, Mathf.Max(1, safeArea.rect.height - 180) / 1740f);
            content.localScale = Vector3.one * Mathf.Min(1.5f, scale);
            // The shared Headbar is authored in 540-wide HUD units; this popup uses 1080-wide units.
            if (headbar != null) ((RectTransform)headbar.transform).sizeDelta = safeArea.rect.size * .5f;
        }
        private void LateUpdate() { if (safeArea != null && content != null) Layout(); }
        private void OnApplicationPause(bool value) { paused = value; skipFrame = true; }
        private void OnApplicationFocus(bool focused) { unfocused = !focused; skipFrame = true; }
        public void Close()
        {
            if (!IsBusy) gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            lifetime?.Cancel();
            lifetime?.Dispose();
            lifetime = null;
            if (paidSpin != null) paidSpin.onClick.RemoveListener(SpinPaid);
            if (adSpin != null) adSpin.onClick.RemoveListener(SpinWithAd);
            if (close != null) close.onClick.RemoveListener(Close);
            if (profile != null) profile.Changed -= Refresh;
            spinning = watchingAd = false;
            if (headbar != null) headbar.StopRewardFlights();
            if (ownsWorldLock)
            {
                DeskActionButton.WorldBlockers = Mathf.Max(0, DeskActionButton.WorldBlockers - 1);
                ownsWorldLock = false;
                player?.SetActionLocked(false);
            }
            foreach (var canvas in hidden) if (canvas != null) canvas.enabled = true;
            hidden.Clear();
            if (cameraHidden && sceneCamera != null) sceneCamera.cullingMask = sceneCullingMask;
            cameraHidden = false;
        }
    }
}
