using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace DoctorHelp
{
    public enum DartRewardKind { Gold, Water, Electricity }

    [Serializable]
    public sealed class DartReward
    {
        public DartRewardKind kind;
        [Min(0)] public int amount;
        public Sprite card;
        public string label;

        internal int Gold => kind == DartRewardKind.Gold ? amount : 0;
        internal int Water => kind == DartRewardKind.Water ? amount : 0;
        internal int Electricity => kind == DartRewardKind.Electricity ? amount : 0;
        internal CurrencyKind Currency => kind switch
        {
            DartRewardKind.Water => CurrencyKind.Water,
            DartRewardKind.Electricity => CurrencyKind.Electricity,
            _ => CurrencyKind.Gold
        };
        internal string Description => $"+{amount:N0} {label}";
    }

    // Balloon dart minigame opened from the Street. Its world is drawn by its own camera far away
    // from the Street; the Street camera stops drawing while the minigame is open.
    // Throwing mirrors the Office ghost rock: drag from the throw button, dashed arc preview,
    // scale by distance. Wind bends the real throw but never the preview.
    [DisallowMultipleComponent]
    public sealed class DartMinigame : MonoBehaviour, IRockAim
    {
        private const int StuckDartOrder = 4;
        private const int FlyingDartOrder = 250;

        [Header("World")]
        [SerializeField] private Transform world;
        [SerializeField] private Camera gameCamera;
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private Transform stuckDarts;
        [SerializeField] private Vector3 worldPosition = new Vector3(0f, 2000f, 0f);
        [SerializeField, Min(1f)] private float baseOrthographicSize = 5f;
        [Tooltip("Narrow screens zoom out so this many world units always fit horizontally.")]
        [SerializeField, Min(1f)] private float minimumVisibleWidth = 5.6f;

        [Header("Balloons (balloon index = reward index)")]
        [SerializeField] private DartBalloon[] balloons = Array.Empty<DartBalloon>();
        [SerializeField] private DartReward[] rewards = Array.Empty<DartReward>();
        [Tooltip("Each balloon floats at a random speed in this range (world units per second).")]
        [SerializeField] private Vector2 balloonSpeedRange = new Vector2(.25f, .55f);
        [Tooltip("Vertical range of the string ends, relative to the screen center.")]
        [SerializeField] private Vector2 balloonHeightRange = new Vector2(-1.6f, 1.1f);
        [SerializeField, Min(0f)] private float balloonAreaMaxHalfWidth = 3.4f;
        [SerializeField, Min(0f)] private float balloonEdgeMargin = .4f;
        [Tooltip("How far a balloon may wander inside its lane, as a fraction of the lane width.")]
        [SerializeField, Range(0f, .5f)] private float balloonLaneWander = .2f;
        [SerializeField] private ParticleSystem puffPrefab;

        [Header("Throw")]
        [SerializeField] private StreetRockAimInput aimInput;
        [SerializeField] private Button throwButton;
        [SerializeField] private DartModel dartPrefab;
        [SerializeField] private GameObject aimMarkerPrefab;
        [SerializeField] private Vector2 aimStartViewport = new Vector2(.5f, .6f);
        [SerializeField] private Vector2 launchViewport = new Vector2(.5f, -.04f);
        [Tooltip("Multiplies the dart prefab scale.")]
        [SerializeField, Min(.1f)] private float dartSize = 1.8f;
        [SerializeField, Min(.05f)] private float flightSeconds = .32f;
        [SerializeField, Range(.1f, 1f)] private float distanceScale = .35f;
        [SerializeField, Min(0f)] private float arcHeight = .45f;
        [SerializeField, Min(0f)] private float cooldownSeconds = .5f;
        [Tooltip("How much screen movement tilts the dart away from pointing straight into the board.")]
        [SerializeField, Min(0f)] private float tiltPerSpeed = .04f;
        [SerializeField] private float flightRollDegrees = 220f;

        [Header("Wind")]
        [Tooltip("The child VFX_Wind particle system; its Velocity over Lifetime X/Y is the wind.")]
        [SerializeField] private ParticleSystem wind;
        [SerializeField, Min(.1f)] private float windChangeSeconds = 3f;
        [Tooltip("Velocity over Lifetime X and Y are each rolled in [-max, max].")]
        [SerializeField, Min(0f)] private float maxWindAxisSpeed = 4f;
        [Tooltip("Landing offset at the strongest possible wind (world units); no wind gives 0.")]
        [SerializeField, Min(0f)] private float maxWindDeviation = 1.875f;
        [SerializeField, Min(0f)] private float windBlendSeconds = .5f;

        [Header("UI")]
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private Text dartCount;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private Image rewardCard;
        [SerializeField] private Text rewardText;
        [SerializeField] private Button rewardConfirm;
        [SerializeField] private GameObject missPopup;
        [SerializeField] private Button missConfirm;
        [SerializeField, Min(0f)] private float rewardPopupDelay = .8f;
        [SerializeField, Min(0f)] private float missPopupDelay = .45f;

        internal static DartMinigame Current { get; private set; }

        private PlayerProfileService profile;
        private Camera sceneCamera;
        private int sceneCullingMask;
        private GameObject marker;
        private RockAimPreview aimPreview;
        private DartModel flyingDart;
        private Vector3 aimOrigin, aimPoint, flightStart, flightTarget, dartScale;
        private Vector2 flightDrift, windCurrent, windFrom, windTo;
        private float flightTime, readyAt, popupAt, windChangeAt, windBlendStart;
        private int pendingReward = -1;
        private DartReward shownReward;
        private Headbar headbar;
        private bool aiming, missPending, started, sceneCameraHidden;
        private Vector2Int screenSize;
        private int[] lanes = Array.Empty<int>();
        private readonly List<Canvas> hiddenCanvases = new List<Canvas>();

        private bool PopupOpen => rewardPopup.activeSelf || missPopup.activeSelf;
        private bool Busy => flyingDart != null || pendingReward >= 0 || missPending || PopupOpen;
        private int DartsLeft => profile != null ? profile.DartsLeft : 0;
        private bool CanAim => !Busy && DartsLeft > 0 && AnyBalloonLeft && Time.time >= readyAt;
        private float BoardDepth => -gameCamera.transform.localPosition.z;

        private bool AnyBalloonLeft
        {
            get
            {
                foreach (var balloon in balloons)
                    if (balloon != null && balloon.IsAvailable) return true;
                return false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Current = null;

        internal static void Open(DartMinigame prefab, Camera streetCamera)
        {
            if (Current != null || prefab == null) return;
            var game = Instantiate(prefab);
            game.sceneCamera = streetCamera;
        }

        private void Awake()
        {
            Current = this;
            world.position = worldPosition;
            aimInput.Throwing = this;
            rewardPopup.SetActive(false);
            missPopup.SetActive(false);
            exitButton.onClick.AddListener(Close);
            rewardConfirm.onClick.AddListener(CloseReward);
            missConfirm.onClick.AddListener(Close);
        }

        private void Start()
        {
            profile = PlayerProfileService.Instance;
            if (profile != null) profile.Changed += RefreshUi;
            if (sceneCamera == null) sceneCamera = Camera.main;
            if (sceneCamera != null && sceneCamera != gameCamera)
            {
                sceneCullingMask = sceneCamera.cullingMask;
                sceneCamera.cullingMask = 0;
                sceneCameraHidden = true;
            }
            HideSceneCanvases();

            lanes = new int[balloons.Length];
            for (int index = 0; index < lanes.Length; index++) lanes[index] = index;
            for (int index = lanes.Length - 1; index > 0; index--)
            {
                int swap = Random.Range(0, index + 1);
                (lanes[index], lanes[swap]) = (lanes[swap], lanes[index]);
            }

            Layout();
            for (int index = 0; index < balloons.Length; index++)
            {
                var balloon = balloons[index];
                if (index < rewards.Length) balloon.CardSprite = rewards[index].card;
                bool popped = profile == null || index >= rewards.Length || profile.IsDartBalloonPopped(index);
                balloon.gameObject.SetActive(!popped);
                if (!popped) balloon.Place(BalloonArea(index), balloonSpeedRange, KillY);
            }

            RollWind(immediate: true);
            if (wind != null) wind.Play();
            started = true;
            RefreshUi();
        }

        private void Update()
        {
            if (!started) return;
            if (screenSize.x != Screen.width || screenSize.y != Screen.height) Layout();
            UpdateWind();
            if (flyingDart != null) AdvanceDart();
            if (pendingReward >= 0 && Time.time >= popupAt) ShowReward();
            if (missPending && Time.time >= popupAt)
            {
                missPending = false;
                missPopup.SetActive(true);
            }
            RefreshThrowButton();
        }

        private void LateUpdate() => ScreenSafeArea.ApplyTo(safeArea);

        // The minigame brings its own Headbar; the scene HUD would otherwise draw on top of the board.
        private void HideSceneCanvases()
        {
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas || !canvas.enabled || canvas.renderMode == RenderMode.WorldSpace ||
                    canvas.gameObject.scene != gameObject.scene || canvas.transform.IsChildOf(transform) ||
                    canvas.GetComponentInChildren<FpsOverlay>(true) != null) continue; // Keep the FPS readout.
                canvas.enabled = false;
                hiddenCanvases.Add(canvas);
            }
        }

        // Fits the camera and background to the screen: the background covers it without stretching.
        private void Layout()
        {
            screenSize = new Vector2Int(Screen.width, Screen.height);
            float aspect = Mathf.Max(.1f, gameCamera.aspect);
            gameCamera.orthographicSize = Mathf.Max(baseOrthographicSize, minimumVisibleWidth / (2f * aspect));
            float height = gameCamera.orthographicSize * 2f, width = height * aspect;
            if (background != null && background.sprite != null)
            {
                var size = background.sprite.bounds.size;
                background.transform.localScale = Vector3.one * Mathf.Max(height / size.y, width / size.x);
            }
            for (int index = 0; index < balloons.Length; index++)
                if (balloons[index] != null) balloons[index].SetArea(BalloonArea(index), KillY);
        }

        private float KillY => world.position.y - gameCamera.orthographicSize - 2f;

        private Rect BalloonArea(int index)
        {
            float width = gameCamera.orthographicSize * 2f * gameCamera.aspect;
            float half = Mathf.Max(.1f, Mathf.Min(width * .5f - balloonEdgeMargin, balloonAreaMaxHalfWidth));
            float lane = 2f * half / Mathf.Max(1, balloons.Length);
            float center = -half + lane * (lanes[index] + .5f);
            float wander = lane * balloonLaneWander;
            return Rect.MinMaxRect(center - wander, Mathf.Min(balloonHeightRange.x, balloonHeightRange.y),
                center + wander, Mathf.Max(balloonHeightRange.x, balloonHeightRange.y));
        }

        private void RollWind(bool immediate)
        {
            windFrom = windCurrent;
            windTo = new Vector2(Random.Range(-maxWindAxisSpeed, maxWindAxisSpeed),
                Random.Range(-maxWindAxisSpeed, maxWindAxisSpeed));
            windBlendStart = Time.time;
            windChangeAt = Time.time + windChangeSeconds;
            if (immediate) windFrom = windCurrent = windTo;
            ApplyWind();
        }

        private void UpdateWind()
        {
            if (Time.time >= windChangeAt) RollWind(immediate: false);
            if (windCurrent == windTo) return;
            float progress = windBlendSeconds > 0f ? Mathf.Clamp01((Time.time - windBlendStart) / windBlendSeconds) : 1f;
            windCurrent = progress >= 1f ? windTo : Vector2.Lerp(windFrom, windTo, Mathf.SmoothStep(0f, 1f, progress));
            ApplyWind();
        }

        private void ApplyWind()
        {
            if (wind == null) return;
            var velocity = wind.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = windCurrent.x;
            velocity.y = windCurrent.y;
            velocity.z = 0f;
        }

        // Strongest wind (both axes at max) moves the landing by maxWindDeviation; no wind by 0.
        private Vector2 WindDeviation()
        {
            float strongest = maxWindAxisSpeed * Mathf.Sqrt(2f);
            float strength = windCurrent.magnitude;
            if (strongest <= 0f || strength <= .0001f) return Vector2.zero;
            return windCurrent / strength * Mathf.Lerp(0f, maxWindDeviation, strength / strongest);
        }

        private Vector3 LaunchPoint
        {
            get
            {
                var point = gameCamera.ViewportToWorldPoint(new Vector3(launchViewport.x, launchViewport.y, BoardDepth));
                point.z = world.position.z;
                return point;
            }
        }

        public bool TryBeginAim()
        {
            if (!started || aiming || !CanAim) return false;
            aiming = true;
            aimOrigin = gameCamera.ViewportToScreenPoint(aimStartViewport);
            if (marker == null)
            {
                marker = Instantiate(aimMarkerPrefab, world);
                var circle = marker.GetComponent<LineRenderer>();
                aimPreview = new RockAimPreview(world, circle.sharedMaterial,
                    circle.sortingLayerID, circle.sortingOrder - 1, circle.startWidth, circle.startColor);
            }
            marker.SetActive(true);
            UpdateAim(Vector2.zero);
            return true;
        }

        public void UpdateAim(Vector2 drag)
        {
            if (!aiming) return;
            var viewport = gameCamera.pixelRect;
            var screen = aimOrigin + (Vector3)drag;
            screen.x = Mathf.Clamp(screen.x, viewport.xMin, viewport.xMax);
            screen.y = Mathf.Clamp(screen.y, viewport.yMin, viewport.yMax);
            screen.z = BoardDepth;
            aimPoint = gameCamera.ScreenToWorldPoint(screen);
            aimPoint.z = world.position.z;
            marker.transform.position = aimPoint;
            aimPreview.ShowOffice(LaunchPoint, aimPoint, arcHeight); // The preview ignores wind.
        }

        public void ReleaseAim(Vector2 drag)
        {
            if (!aiming) return;
            if (!CanAim) { CancelAim(); return; }
            UpdateAim(drag);
            CancelAim();
            if (!profile.TryUseDart()) return; // Saved right away, so leaving keeps the count.
            flightStart = LaunchPoint;
            flightTarget = aimPoint;
            flightDrift = WindDeviation();
            flyingDart = Instantiate(dartPrefab, world);
            flyingDart.SetSorting(FlyingDartOrder);
            dartScale = flyingDart.transform.localScale * dartSize;
            flightTime = 0f;
            PlaceDart(0f);
            readyAt = Time.time + cooldownSeconds;
            RefreshUi();
        }

        public void CancelAim()
        {
            aiming = false;
            if (marker != null) marker.SetActive(false);
            aimPreview?.Hide();
            if (aimInput != null) aimInput.ForgetPointer();
        }

        private void AdvanceDart()
        {
            flightTime += Time.deltaTime;
            float progress = Mathf.Clamp01(flightTime / flightSeconds);
            PlaceDart(progress);
            if (progress >= 1f) LandDart(showFeedback: true);
        }

        // Wind drift grows during the flight so the dart visibly bends off the previewed arc.
        private void PlaceDart(float progress)
        {
            var dart = flyingDart.transform;
            dart.position = RockAimPreview.OfficePosition(flightStart, flightTarget, arcHeight, progress) +
                (Vector3)(flightDrift * (progress * progress));
            dart.localScale = dartScale * Mathf.Lerp(1f, distanceScale, progress);
            var velocity = flightTarget - flightStart;
            velocity.y += 4f * arcHeight * (1f - 2f * progress);
            velocity += (Vector3)(flightDrift * (2f * progress));
            velocity *= tiltPerSpeed / flightSeconds;
            var forward = new Vector3(velocity.x, velocity.y, 1f).normalized;
            dart.rotation = Quaternion.LookRotation(forward, Vector3.up) *
                Quaternion.Euler(0f, 0f, flightRollDegrees * progress);
        }

        private void LandDart(bool showFeedback)
        {
            var dart = flyingDart;
            flyingDart = null;
            if (dart == null) return;
            dart.transform.SetParent(stuckDarts, true);
            dart.SetSorting(StuckDartOrder);

            int hit = FindBalloon(dart.transform.position);
            if (hit >= 0 && profile != null && profile.TryClaimDartBalloon(hit, rewards[hit].Gold,
                rewards[hit].Water, rewards[hit].Electricity))
            {
                balloons[hit].Pop();
                if (showFeedback)
                {
                    PlayPuff(balloons[hit].BodyCenter);
                    // The bar keeps showing the old amount until the reward flies in from the popup.
                    if (Bar != null) Bar.Hold(rewards[hit].Currency, rewards[hit].amount);
                    pendingReward = hit;
                    popupAt = Time.time + rewardPopupDelay;
                }
            }
            else if (showFeedback && DartsLeft <= 0)
            {
                missPending = true;
                popupAt = Time.time + missPopupDelay;
            }
            RefreshUi();
        }

        private int FindBalloon(Vector2 point)
        {
            for (int index = 0; index < balloons.Length && index < rewards.Length; index++)
                if (balloons[index] != null && balloons[index].Contains(point)) return index;
            return -1;
        }

        private void PlayPuff(Vector3 position)
        {
            if (puffPrefab == null) return;
            var puff = Instantiate(puffPrefab, position, puffPrefab.transform.rotation, world);
            var settings = puff.main;
            settings.stopAction = ParticleSystemStopAction.Destroy;
            puff.Play();
        }

        private void ShowReward()
        {
            var reward = rewards[pendingReward];
            pendingReward = -1;
            rewardCard.sprite = reward.card;
            rewardText.text = reward.Description;
            shownReward = reward;
            rewardPopup.SetActive(true);
        }

        // Confirming the reward: VFX_Puff on the card, then the reward's icons fly into the minigame's Headbar.
        private void CloseReward()
        {
            var reward = shownReward;
            shownReward = null;
            if (reward != null && Bar != null)
            {
                var card = rewardCard.rectTransform;
                Vector2 origin = RectTransformUtility.WorldToScreenPoint(null, card.TransformPoint(card.rect.center));
                Bar.PlayRewardPuff(origin, gameCamera);
                Bar.FlyReward(reward.Currency, reward.amount, origin, alreadyHeld: true);
            }
            rewardPopup.SetActive(false);
        }

        private Headbar Bar => headbar != null ? headbar : headbar = GetComponentInChildren<Headbar>(true);

        private void RefreshUi()
        {
            if (dartCount != null) dartCount.text = "x" + DartsLeft;
            RefreshThrowButton();
        }

        private void RefreshThrowButton()
        {
            bool ready = aiming || CanAim;
            if (throwButton != null && throwButton.interactable != ready) throwButton.interactable = ready;
        }

        // A dart already in the air still lands and pays out when the player leaves.
        internal void Close()
        {
            CancelAim();
            if (flyingDart != null)
            {
                PlaceDart(1f);
                LandDart(showFeedback: false);
            }
            Destroy(gameObject);
        }

        private void OnApplicationFocus(bool focused) { if (!focused) CancelAim(); }
        private void OnApplicationPause(bool paused) { if (paused) CancelAim(); }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            if (profile != null) profile.Changed -= RefreshUi;
            if (aimInput != null) aimInput.Throwing = null;
            if (sceneCameraHidden && sceneCamera != null) sceneCamera.cullingMask = sceneCullingMask;
            foreach (var canvas in hiddenCanvases)
                if (canvas != null) canvas.enabled = true;
            exitButton.onClick.RemoveListener(Close);
            rewardConfirm.onClick.RemoveListener(CloseReward);
            missConfirm.onClick.RemoveListener(Close);
        }
    }
}
