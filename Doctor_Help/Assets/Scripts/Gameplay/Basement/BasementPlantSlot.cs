using UnityEngine;
using UnityEngine.Serialization;

namespace DoctorHelp
{
    // One shelf in the basement that can hold a single protective plant.
    // Slot ID is saved, so never rename it once players have plants placed.
    [DisallowMultipleComponent]
    public sealed class BasementPlantSlot : MonoBehaviour
    {
        [Tooltip("Stable save ID of this shelf.")]
        [SerializeField] private string slotId;
        [Tooltip("Where the plant pot stands; its rotation/scale also orient the plant (negative X scale faces left).")]
        [SerializeField] private Transform plantAnchor;
        [Tooltip("Center of the zone where this shelf's plant can reach a rat (its mouth in Idle).")]
        [SerializeField] private Transform mouthZone;
        [Tooltip("Reach of the plant around Mouth Zone, in world units. Rats on the pipe inside it can be caught.")]
        [SerializeField, Min(.05f)] private float mouthRadius = 1.6f;
        [Tooltip("World button of this shelf: 'Add protection' while empty, 'Protection' with a plant. Opens the popup.")]
        [FormerlySerializedAs("addProtectionButton")]
        [SerializeField] private DeskActionButton protectionButton;

        internal string SlotId => slotId;
        internal Transform PlantAnchor => plantAnchor != null ? plantAnchor : transform;
        internal Vector3 MouthCenter => mouthZone != null ? mouthZone.position : PlantAnchor.position;
        internal float MouthRadius => mouthRadius;
        internal DeskActionButton ProtectionButton => protectionButton;

        // The plant shown on this shelf, created by BasementDefenseView from the saved state:
        // its root object, and the RatEater (swallow) or DefensePlantAnimation (ranged) on it.
        internal GameObject PlantObject { get; set; }
        internal RatEater Plant { get; set; }
        internal DefensePlantAnimation Ranged { get; set; }
        internal int PlantInstanceId { get; set; }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(.3f, 1f, .35f, .8f);
            Gizmos.DrawWireSphere(MouthCenter, mouthRadius);
        }
    }
}
