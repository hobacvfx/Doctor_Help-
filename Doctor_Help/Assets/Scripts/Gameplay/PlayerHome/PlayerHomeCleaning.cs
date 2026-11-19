using System;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    public sealed class PlayerHomeCleaning : MonoBehaviour
    {
        private static readonly int ScrubState = Animator.StringToHash("Base Layer.A_WallScrub");
        private const float StandingDistance = 1.2f;
        private const float ArrivalTolerance = 0.08f;
        private const float RouteTimeout = 30f;

        [SerializeField] private PlayerHomeMovement player;
        [SerializeField] private PlayerHomeElevator elevator;
        [SerializeField] private PlayerHomeDirt dirt;
        [SerializeField] private Button cleanButton;
        [SerializeField] private Animator mop;

        private ClinicWallStain target;
        private float? standX;
        private float routeEnds;
        private bool scrubbing;
        private bool configured;
        internal Button Button => cleanButton;
        internal ClinicWallStain GuidedStain { get; set; }
        internal event Action<ClinicWallStain> Started;

        private bool Unlocked => PlayerProfileService.Instance?.Clean == true;
        private bool CanStart => Unlocked && target == null && dirt.HasStains && player.isActiveAndEnabled && !player.IsRiding;

        private void Awake()
        {
            configured = player != null && elevator != null && dirt != null && cleanButton != null && mop != null;
            if (configured) return;
            Debug.LogError("Player Home cleaning requires the player, elevator, stains, button and mop.", this);
            enabled = false;
        }

        private void OnEnable()
        {
            if (!configured) return;
            cleanButton.onClick.AddListener(StartCleaning);
            RefreshButton();
        }

        private void StartCleaning()
        {
            if (!CanStart) return;
            target = GuidedStain ?? dirt.ClosestStain(elevator.PlayerFloor(), player.Position.x);
            if (GuidedStain != null && !dirt.TryGetStainPosition(GuidedStain, out _)) target = null;
            if (target == null) return;
            player.BeginAutomaticWalk();
            routeEnds = Time.time + RouteTimeout;
            cleanButton.interactable = false;
            Started?.Invoke(target);
        }

        private void Update()
        {
            if (target != null) AdvanceCleaning();
            RefreshButton();
        }

        private void RefreshButton()
        {
            if (cleanButton.gameObject.activeSelf != Unlocked) cleanButton.gameObject.SetActive(Unlocked);
            cleanButton.interactable = CanStart;
        }

        private void AdvanceCleaning()
        {
            if (!player.isActiveAndEnabled || !dirt.TryGetStainPosition(target, out var position))
            {
                EndCleaning();
                return;
            }
            if (scrubbing)
            {
                var state = mop.GetCurrentAnimatorStateInfo(0);
                if (state.fullPathHash != ScrubState || state.normalizedTime < 1f) return;
                dirt.CleanStain(target);
                EndCleaning();
                return;
            }
            if (Time.time >= routeEnds)
            {
                Debug.LogWarning("Could not reach the wall stain.", this);
                EndCleaning();
                return;
            }
            if (player.IsRiding) return;
            if (elevator.PlayerFloor() != target.floor)
            {
                player.SetAutomaticDestination(elevator.BoardingX);
                return;
            }

            var range = elevator.WalkingRange(target.floor);
            standX ??= Mathf.Clamp(position.x + (player.Position.x < position.x ? -StandingDistance : StandingDistance),
                range.x, range.y);
            if (Mathf.Abs(player.Position.x - standX.Value) > ArrivalTolerance)
            {
                player.SetAutomaticDestination(standX);
                return;
            }

            player.SetAutomaticDestination(null);
            if (player.TryGetComponent<PlayerHomeCharacterAnimation>(out var animation))
                animation.SetFacing(position.x >= player.Position.x);
            mop.transform.position = new Vector3(position.x, position.y, player.transform.position.z);
            mop.gameObject.SetActive(true);
            if (!mop.HasState(0, ScrubState))
            {
                Debug.LogError("Wall mop is missing A_WallScrub.", mop);
                EndCleaning();
                return;
            }
            scrubbing = true;
            mop.Play(ScrubState, 0, 0f);
        }

        private void EndCleaning()
        {
            target = null;
            standX = null;
            scrubbing = false;
            if (mop != null) mop.gameObject.SetActive(false);
            if (player != null) player.EndAutomaticWalk();
        }

        private void OnDisable()
        {
            if (!configured) return;
            if (cleanButton != null) cleanButton.onClick.RemoveListener(StartCleaning);
            if (target != null) EndCleaning();
        }
    }
}
