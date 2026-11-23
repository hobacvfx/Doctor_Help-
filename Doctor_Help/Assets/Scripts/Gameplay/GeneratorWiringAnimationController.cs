using Spine;
using Spine.Unity;
using UnityEngine;

namespace DoctorHelp
{
    /// <summary>Visual wire damage sequence; does not change player electricity or generator operation.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SkeletonAnimation))]
    public sealed class GeneratorWiringAnimationController : MonoBehaviour
    {
        public enum Activity { Intact, Breaking, Gnawing }

        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private bool playOnEnable;
        private TrackEntry attackTrack;
        private bool initialized;

        public Activity State { get; private set; }
        public bool IsBroken { get; private set; }

        private void Reset() => skeletonAnimation = GetComponent<SkeletonAnimation>();

        private void OnEnable()
        {
            if (!InitializeAnimation()) return;
            ResetIntact();
            if (playOnEnable) TryBreakWire();
        }

        private bool InitializeAnimation()
        {
            if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();
            if (skeletonAnimation == null || skeletonAnimation.SkeletonDataAsset == null)
            {
                Debug.LogError("Generator wiring requires a configured SkeletonAnimation.", this);
                return initialized = false;
            }
            skeletonAnimation.Initialize(false);
            var data = skeletonAnimation.SkeletonDataAsset.GetSkeletonData(false);
            initialized = data != null && data.FindAnimation("BreakWire") != null && data.FindAnimation("Gnaw") != null;
            if (!initialized) Debug.LogError("Generator wiring requires BreakWire and Gnaw animations.", this);
            return initialized;
        }

        public bool TryBreakWire()
        {
            if (!initialized || !isActiveAndEnabled || State != Activity.Intact) return false;
            State = Activity.Breaking;
            attackTrack = skeletonAnimation.AnimationState.SetAnimation(0, "BreakWire", false);
            attackTrack.MixDuration = 0f;
            attackTrack.Event += OnSpineEvent;
            attackTrack.Complete += OnAttackComplete;
            return true;
        }

        private void OnSpineEvent(TrackEntry entry, Spine.Event spineEvent)
        {
            if (entry == attackTrack && spineEvent.Data.Name == "wire_break") IsBroken = true;
        }

        private void OnAttackComplete(TrackEntry entry)
        {
            if (entry != attackTrack || State != Activity.Breaking) return;
            ReleaseAttackTrack();
            IsBroken = true;
            State = Activity.Gnawing;
            var gnaw = skeletonAnimation.AnimationState.SetAnimation(0, "Gnaw", true);
            gnaw.MixDuration = 0f;
        }

        /// <summary>Shows the basement simulation's wire state without replaying it: intact, a time into
        /// BreakWire, or the Gnaw loop. A live break still goes through TryBreakWire exactly once.</summary>
        public void ShowSimulatedState(BasementWireState wire, float seconds)
        {
            if (!initialized) return;
            switch (wire)
            {
                case BasementWireState.Intact:
                    if (State != Activity.Intact) ResetIntact();
                    break;
                case BasementWireState.Breaking:
                    // Gnaw already showing near the end is just Spine finishing a frame early; early in the
                    // attack it means the wire was repaired and bitten again within one frame.
                    if (State == Activity.Gnawing && seconds < 1f) ResetIntact();
                    if (State == Activity.Intact) TryBreakWire();
                    if (attackTrack != null && Mathf.Abs(attackTrack.TrackTime - seconds) > .15f)
                        attackTrack.TrackTime = Mathf.Max(0f, seconds);
                    break;
                case BasementWireState.Broken:
                    if (State != Activity.Gnawing) ShowGnawing();
                    break;
            }
        }

        private void ShowGnawing()
        {
            ReleaseAttackTrack();
            IsBroken = true;
            State = Activity.Gnawing;
            var gnaw = skeletonAnimation.AnimationState.SetAnimation(0, "Gnaw", true);
            gnaw.MixDuration = 0f;
        }

        public void ResetIntact()
        {
            if (!initialized) return;
            ReleaseAttackTrack();
            skeletonAnimation.AnimationState.ClearTracks();
            skeletonAnimation.Skeleton.SetupPose();
            skeletonAnimation.Update(0f);
            State = Activity.Intact;
            IsBroken = false;
        }

        private void ReleaseAttackTrack()
        {
            if (attackTrack == null) return;
            attackTrack.Event -= OnSpineEvent;
            attackTrack.Complete -= OnAttackComplete;
            attackTrack = null;
        }

        private void OnDisable()
        {
            ReleaseAttackTrack();
            initialized = false;
        }

        [ContextMenu("Preview/Break Wire (Play Mode)")]
        private void PreviewBreak()
        {
            if (!Application.isPlaying) return;
            ResetIntact();
            TryBreakWire();
        }

        [ContextMenu("Preview/Reset Intact (Play Mode)")]
        private void PreviewReset()
        {
            if (Application.isPlaying) ResetIntact();
        }
    }
}

