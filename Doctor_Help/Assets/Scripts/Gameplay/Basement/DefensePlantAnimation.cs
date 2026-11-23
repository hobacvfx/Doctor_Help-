using System;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace DoctorHelp
{
    /// <summary>Plant presentation. Fire events are hooks, not damage transactions.
    /// Standalone preview: Attack ends in Idle, or in Reload then Idle when the rig has a Reload clip.
    /// In the basement, BasementDefenseView drives it from the saved simulation every frame (ShowStrike /
    /// ShowMagazine): clip, time and petals then mirror the plant instance exactly, nothing plays on its own,
    /// and re-enabling or rebuilding the view never restores petals the instance has not reloaded.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(SkeletonAnimation))]
    [DefaultExecutionOrder(50)]
    public sealed class DefensePlantAnimation : MonoBehaviour
    {
        private const float ResyncSeconds = .08f;

        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private string muzzleBoneName = "muzzle";
        private Bone muzzle;
        private TrackEntry action;
        private bool initialized;
        // Driven by the simulation: no automatic Attack → Reload → Idle chain.
        private bool simulated;
        public event Action<int> Fired;
        public bool IsBusy => action != null;

        private void OnEnable()
        {
            if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();
            skeletonAnimation.Initialize(false);
            var data = skeletonAnimation.SkeletonDataAsset != null ? skeletonAnimation.SkeletonDataAsset.GetSkeletonData(false) : null;
            initialized = data != null && data.FindAnimation("Idle") != null && data.FindAnimation("Attack") != null;
            if (!initialized) { Debug.LogError("Defense plant requires configured Idle and Attack Spine clips.", this); return; }
            muzzle = skeletonAnimation.Skeleton.FindBone(muzzleBoneName);
            skeletonAnimation.AnimationState.Event += OnEvent;
            if (!simulated) PlayIdle(); // A simulated plant gets its real state from the view in the same frame.
        }

        /// <summary>Presentation anchor for target-directed projectile/beam code. Does not pick an aim direction.</summary>
        public bool TryGetMuzzlePosition(out Vector3 position)
        {
            position = transform.position;
            if (!initialized || muzzle == null) return false;
            muzzle.AppliedPose.LocalToWorld(0, 0, out float x, out float y);
            position = skeletonAnimation.transform.TransformPoint(x, y, 0);
            return true;
        }

        /// <summary>Preview only: starts a complete authored volley. No targeting, health, damage or inventory changes.</summary>
        public bool TryAttack()
        {
            if (!initialized || !isActiveAndEnabled || IsBusy || simulated) return false;
            StartAction("Attack");
            return true;
        }

        public void PlayIdle()
        {
            if (!initialized) return;
            ReleaseAction();
            // Restore petals after a cancelled preview volley.
            skeletonAnimation.AnimationState.ClearTracks();
            skeletonAnimation.Skeleton.SetupPose();
            var idle = skeletonAnimation.AnimationState.SetAnimation(0, "Idle", true);
            idle.MixDuration = 0f;
        }

        /// <summary>Applies the current pose now (bone world positions, e.g. the muzzle, are then current).</summary>
        public void RefreshPose() { if (initialized) Apply(); }

        public float ClipSeconds(string clip)
        {
            var data = initialized ? skeletonAnimation.Skeleton.Data.FindAnimation(clip) : null;
            return data != null ? data.Duration : 0f;
        }

        /// <summary>Strike plant (volley/arc): the Attack clip at elapsed × speed while the strike plays, else Idle.</summary>
        public void ShowStrike(bool attacking, float elapsedSeconds, float speed)
        {
            if (!Simulate()) return;
            float clipTime = elapsedSeconds * Mathf.Max(.01f, speed);
            if (attacking && clipTime < ClipSeconds("Attack")) Show("Attack", clipTime, speed, false);
            else ShowIdle();
        }

        /// <summary>Petal magazine: Idle when full; the Attack pose of the next petal while petals are spent (the
        /// head turns between petals at the authored rate, scaled to the current interval, and holds without a
        /// target); Reload time-scaled to the reload length. Visible petals always equal the ammo left.</summary>
        public void ShowMagazine(int ammo, int magazine, bool reloading, float reloadElapsed, float reloadSeconds,
            float sinceShot, float interval)
        {
            if (!Simulate()) return;
            if (reloading)
            {
                float clip = ClipSeconds("Reload");
                if (clip <= 0f) { ShowIdle(); return; }
                float speed = clip / Mathf.Max(.01f, reloadSeconds);
                Show("Reload", Mathf.Min(reloadElapsed * speed, clip), speed, false);
                return;
            }
            if (ammo >= magazine) { ShowIdle(); return; }
            // Authored: petal k vanishes at k × step (step = Attack / magazine). After `spent` petals the pose
            // stays inside [ (spent − 1) × step, spent × step ) so exactly `spent` petals are hidden.
            float step = ClipSeconds("Attack") / Mathf.Max(1, magazine);
            int spent = Mathf.Clamp(magazine - ammo, 1, magazine);
            float progress = Mathf.Clamp01(sinceShot / Mathf.Max(.0001f, interval));
            Show("Attack", (spent - 1) * step + progress * step * .995f, 0f, true);
        }

        private bool Simulate()
        {
            if (!initialized) return false;
            if (!simulated)
            {
                simulated = true;
                ReleaseAction();
            }
            return true;
        }

        private void ShowIdle()
        {
            var current = skeletonAnimation.AnimationState.GetTrack(0);
            if (current != null && current.Animation.Name == "Idle") return;
            skeletonAnimation.Skeleton.SetupPose();
            var idle = skeletonAnimation.AnimationState.SetAnimation(0, "Idle", true);
            idle.MixDuration = 0f;
            Apply();
        }

        // exact: set the time every frame (pose computed from saved state); otherwise play at speed and only
        // correct drift, so motion stays smooth.
        private void Show(string clip, float time, float speed, bool exact)
        {
            var current = skeletonAnimation.AnimationState.GetTrack(0);
            bool changed = current == null || current.Animation.Name != clip;
            if (changed)
            {
                skeletonAnimation.Skeleton.SetupPose();
                current = skeletonAnimation.AnimationState.SetAnimation(0, clip, false);
                current.MixDuration = 0f;
            }
            current.TimeScale = speed;
            if (changed || exact || Mathf.Abs(current.TrackTime - time) > ResyncSeconds)
            {
                current.TrackTime = time;
                Apply();
            }
        }

        // Pose now, so a rebuilt or re-enabled plant never shows one wrong frame (e.g. full petals).
        private void Apply() => skeletonAnimation.Update(0f);

        private void StartAction(string clip)
        {
            ReleaseAction();
            action = skeletonAnimation.AnimationState.SetAnimation(0, clip, false);
            action.MixDuration = 0f;
            action.Complete += OnComplete;
        }

        private void OnComplete(TrackEntry entry)
        {
            if (entry != action || simulated) return;
            bool reload = entry.Animation.Name == "Attack" && skeletonAnimation.Skeleton.Data.FindAnimation("Reload") != null;
            if (reload) StartAction("Reload"); else PlayIdle();
        }

        private void OnEvent(TrackEntry entry, Spine.Event ev)
        {
            if (ev.Data.Name != "fire" || (!simulated && entry != action)) return;
            Fired?.Invoke(ev.Int); // Presentation hook only; gameplay never depends on it.
        }

        private void ReleaseAction() { if (action != null) action.Complete -= OnComplete; action = null; }
        private void OnDisable()
        {
            ReleaseAction(); muzzle = null;
            if (initialized && skeletonAnimation != null && skeletonAnimation.AnimationState != null)
                skeletonAnimation.AnimationState.Event -= OnEvent;
            initialized = false;
        }

        [ContextMenu("Preview/Attack Volley (Play Mode)")]
        private void PreviewAttack() { if (Application.isPlaying) TryAttack(); }
        [ContextMenu("Preview/Reset Idle (Play Mode)")]
        private void PreviewIdle() { if (Application.isPlaying) PlayIdle(); }
    }
}
