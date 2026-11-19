using UnityEngine;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    internal sealed class FirstDayGuide : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer arrow;
        [SerializeField, Min(0.01f)] private float radius = 0.5f;
        [SerializeField, Min(0.01f), Tooltip("Uniform scale of the arrow sprite. 1 uses the original size.")]
        private float arrowSize = 1f;

        private PlayerHomeMovement player;
        private DeskActionButton firstDayDestination, receptionistDestination;
        private PlayerProfileService profile;
        private DailyGameLoop loop;

        internal void Configure(PlayerHomeMovement player, DeskActionButton firstDayDestination, DeskActionButton receptionistDestination)
        {
            this.player = player;
            this.firstDayDestination = firstDayDestination;
            this.receptionistDestination = receptionistDestination;
            profile = PlayerProfileService.Instance;
            loop = profile != null ? profile.GetComponent<DailyGameLoop>() : null;
        }

        private void LateUpdate()
        {
            if (player == null || player.IsTutorialLocked || profile == null || !profile.IsLoaded ||
                loop == null || loop.Step != DayStep.Preparation || loop.IsTransitioning)
            {
                arrow.enabled = false;
                return;
            }

            var destination = profile.CurrentMainObjective switch
            {
                MainObjective.ClearFirstDay => firstDayDestination,
                MainObjective.UpgradeReceptionist => receptionistDestination,
                _ => null
            };
            if (destination == null || !destination.isActiveAndEnabled || destination.HitArea == null)
            {
                arrow.enabled = false;
                return;
            }
            Vector3 chest = player.GuideChestPosition;
            Vector2 direction = (Vector2)(destination.HitArea.bounds.center - chest);
            float distance = direction.magnitude;
            arrow.enabled = distance > 0.001f;
            if (!arrow.enabled) return;

            arrow.transform.localScale = Vector3.one * arrowSize;

            // Keep the pivot strictly between both points even inside the normal orbit.
            float offset = Mathf.Min(radius, distance * 0.99f);
            var rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            arrow.transform.SetPositionAndRotation(chest + (Vector3)(direction * (offset / distance)), rotation);
        }

        private void OnDisable() => arrow.enabled = false;
    }
}
