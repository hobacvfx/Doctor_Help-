using Spine;
using Spine.Unity;
using UnityEngine;

namespace DoctorHelp
{
    /// <summary>Owns the plant's animation cycle; rat targeting and consumption are separate gameplay responsibilities.
    /// In the basement, BasementDefenseService owns timing (Eat, bite, the plant profile's cooldown) so the result is the same in
    /// every scene; BasementDefenseView then drives this component through ShowSimulatedState and its own
    /// TryEat/cooldown are not used. TryEat and the cooldown below remain for standalone previews.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SkeletonAnimation))]
    public sealed class RatEater : MonoBehaviour
    {
        public enum Activity { Idle, Eating, Cooldown }

        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [Tooltip("Standalone preview cooldown. Basement gameplay uses BasementDefenseSettings.")]
        [SerializeField, Min(0f)] private float cooldownSeconds = 30f;
        [Tooltip("Mouth centre in head-bone space, in Spine skeleton units. A caught rat is drawn into this point.")]
        [SerializeField] private Vector2 mouthInHead = new Vector2(330f, 10f);

        private TrackEntry eatingTrack;
        private bool initialized, simulated;
        private Bone headBone;
        public Activity State { get; private set; }
        public float CooldownRemaining { get; private set; }
        public bool CanEat => initialized && !simulated && isActiveAndEnabled && State == Activity.Idle;
        public float CooldownSeconds => cooldownSeconds;

        private void Reset() => skeletonAnimation = GetComponent<SkeletonAnimation>();

        private void OnEnable()
        {
            if (!InitializeAnimation()) return;
            PlayIdle();
        }

        private bool InitializeAnimation()
        {
            if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();
            if (skeletonAnimation == null || skeletonAnimation.SkeletonDataAsset == null)
            {
                initialized = false;
                Debug.LogError("RatEater requires a configured SkeletonAnimation and SkeletonDataAsset.", this);
                return false;
            }
            skeletonAnimation.Initialize(false);
            var data = skeletonAnimation.SkeletonDataAsset.GetSkeletonData(false);
            initialized = data != null && data.FindAnimation("Idle") != null &&
                data.FindAnimation("Eat") != null && data.FindAnimation("Chew") != null;
            if (!initialized)
                Debug.LogError("RatEater requires Spine animations Idle, Eat and Chew.", this);
            return initialized;
        }

        /// <summary>Begins one bite when ready. Cooldown starts after the complete Eat animation.</summary>
        public bool TryEat()
        {
            if (!CanEat) return false;
            State = Activity.Eating;
            CooldownRemaining = 0f;
            eatingTrack = skeletonAnimation.AnimationState.SetAnimation(0, "Eat", false);
            eatingTrack.Complete += OnEatComplete;
            return true;
        }

        private void OnEatComplete(TrackEntry entry)
        {
            if (entry != eatingTrack || State != Activity.Eating) return;
            ReleaseEatingTrack();
            State = Activity.Cooldown;
            CooldownRemaining = Mathf.Max(0f, cooldownSeconds);
            skeletonAnimation.AnimationState.SetAnimation(0, "Chew", true);
            if (CooldownRemaining <= 0f) PlayIdle();
        }

        private void Update() => AdvanceCooldown(Time.deltaTime);

        private void AdvanceCooldown(float deltaTime)
        {
            if (!initialized || simulated || State != Activity.Cooldown) return;
            CooldownRemaining = Mathf.Max(0f, CooldownRemaining - Mathf.Max(0f, deltaTime));
            if (CooldownRemaining <= 0f) PlayIdle();
        }

        private void PlayIdle()
        {
            ReleaseEatingTrack();
            State = Activity.Idle;
            CooldownRemaining = 0f;
            skeletonAnimation.AnimationState.SetAnimation(0, "Idle", true);
        }

        private void ReleaseEatingTrack()
        {
            if (eatingTrack == null) return;
            eatingTrack.Complete -= OnEatComplete;
            eatingTrack = null;
        }

        /// <summary>Shows a state decided by the basement simulation: Idle, Eat at a time into the clip, or Chew
        /// looping through the cooldown. Only resyncs playback when it drifts, so normal frames keep playing.</summary>
        public void ShowSimulatedState(Activity activity, float secondsInActivity)
        {
            if (!initialized) return;
            simulated = true;
            ReleaseEatingTrack();
            string clip = activity == Activity.Eating ? "Eat" : activity == Activity.Cooldown ? "Chew" : "Idle";
            var track = skeletonAnimation.AnimationState.GetTrack(0);
            if (track == null || track.Animation.Name != clip)
            {
                track = skeletonAnimation.AnimationState.SetAnimation(0, clip, activity != Activity.Eating);
                if (activity != Activity.Idle) track.TrackTime = Mathf.Max(0f, secondsInActivity);
            }
            else if (activity != Activity.Idle && Mathf.Abs(track.TrackTime - secondsInActivity) > .15f)
                track.TrackTime = Mathf.Max(0f, secondsInActivity);
            State = activity;
            CooldownRemaining = 0f;
        }

        /// <summary>World position of the mouth centre in the current pose (follows the Eat lunge).</summary>
        public bool TryGetMouthPosition(out Vector3 position)
        {
            position = transform.position;
            if (!initialized) return false;
            if (headBone == null) headBone = skeletonAnimation.Skeleton.FindBone("head");
            if (headBone == null) return false;
            float scale = skeletonAnimation.SkeletonDataAsset.scale;
            headBone.AppliedPose.LocalToWorld(mouthInHead.x * scale, mouthInHead.y * scale, out float x, out float y);
            position = skeletonAnimation.transform.TransformPoint(x, y, 0f);
            return true;
        }

        private void OnDisable()
        {
            ReleaseEatingTrack();
            initialized = false;
            simulated = false;
            headBone = null;
            State = Activity.Idle;
            CooldownRemaining = 0f;
        }

        [ContextMenu("Preview/Eat (Play Mode)")]
        private void PreviewEat()
        {
            if (Application.isPlaying) TryEat();
        }

        [ContextMenu("Preview/Reset Idle (Play Mode)")]
        private void PreviewIdle()
        {
            if (Application.isPlaying && initialized) PlayIdle();
        }
    }
}

