using UnityEngine;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(20)] // Wait for a new model's initial Spine mesh before anchoring once.
    public sealed class GeneratorUpgradeView : MonoBehaviour
    {
        [SerializeField] private ClinicFurnitureSlot furniture;
        [SerializeField] private GeneratorAnimationController legacyGenerator;
        [SerializeField] private ClinicShopEntrance upgradeEntrance;
        [SerializeField] private DeskActionButton repairButton;
        [SerializeField, Min(0f)] private float actionTopSpacing = .65f;
        private GameObject positionedVisual;
        private Renderer generatorRenderer;
        private bool actionPositioned;

        private void OnEnable()
        {
            if (legacyGenerator != null) legacyGenerator.gameObject.SetActive(false);
            if (upgradeEntrance != null) upgradeEntrance.SetAvailable(false);
            if (repairButton != null) repairButton.gameObject.SetActive(false);
        }

        internal void SetOperating(bool operating)
        {
            // Hide the previous action before revealing its replacement.
            if (operating && repairButton != null) repairButton.gameObject.SetActive(false);
            if (upgradeEntrance != null) upgradeEntrance.SetAvailable(operating);
            if (!operating && repairButton != null) repairButton.gameObject.SetActive(true);
            var visual = furniture != null ? furniture.EquippedVisual : null;
            if (visual != null && visual.TryGetComponent<GeneratorAnimationController>(out var generator))
                generator.SetOperating(operating);
        }

        private void LateUpdate()
        {
            var visual = furniture != null ? furniture.EquippedVisual : null;
            if (visual != positionedVisual)
            {
                positionedVisual = visual;
                generatorRenderer = visual != null ? visual.GetComponent<Renderer>() : null;
                actionPositioned = false;
            }
            if (actionPositioned || generatorRenderer == null) return;
            var bounds = generatorRenderer.bounds;
            if (bounds.size.sqrMagnitude == 0f) return;
            var position = new Vector3(bounds.center.x,
                bounds.max.y + actionTopSpacing * Mathf.Abs(transform.lossyScale.y), transform.position.z);
            if (upgradeEntrance != null && upgradeEntrance.ActionButton != null)
                upgradeEntrance.ActionButton.transform.position = position;
            if (repairButton != null) repairButton.transform.position = position;
            // Animated rotor/body bounds must never animate the action buttons.
            actionPositioned = true;
        }
    }
}
