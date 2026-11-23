using Spine.Unity;
using UnityEngine;

namespace DoctorHelp
{
    /// <summary>Controls the generator's visual state. Electricity production and wiring are separate systems.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SkeletonAnimation))]
    public sealed class GeneratorAnimationController : MonoBehaviour
    {
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private bool operating;

        public bool IsOperating => operating;

        private void Reset() => skeletonAnimation = GetComponent<SkeletonAnimation>();
        private void OnEnable() => ApplyAnimation();

        public void SetOperating(bool value)
        {
            operating = value;
            if (isActiveAndEnabled) ApplyAnimation();
        }

        private void ApplyAnimation()
        {
            if (skeletonAnimation == null) skeletonAnimation = GetComponent<SkeletonAnimation>();
            if (skeletonAnimation == null || skeletonAnimation.SkeletonDataAsset == null)
            {
                Debug.LogError("Generator requires a configured SkeletonAnimation.", this);
                return;
            }
            skeletonAnimation.Initialize(false);
            var data = skeletonAnimation.SkeletonDataAsset.GetSkeletonData(false);
            string name = operating ? "Operating" : "Idle";
            if (data == null || data.FindAnimation(name) == null)
            {
                Debug.LogError("Generator is missing Spine animation: " + name, this);
                return;
            }
            var current = skeletonAnimation.AnimationState.GetTrack(0);
            if (current != null && current.Animation.Name == name && current.Loop) return;
            skeletonAnimation.AnimationState.SetAnimation(0, name, true);
        }

        private void OnValidate()
        {
            if (Application.isPlaying && isActiveAndEnabled) ApplyAnimation();
        }

        [ContextMenu("Preview/Start Operating (Play Mode)")]
        private void PreviewStart()
        {
            if (Application.isPlaying) SetOperating(true);
        }

        [ContextMenu("Preview/Stop (Play Mode)")]
        private void PreviewStop()
        {
            if (Application.isPlaying) SetOperating(false);
        }
    }
}

