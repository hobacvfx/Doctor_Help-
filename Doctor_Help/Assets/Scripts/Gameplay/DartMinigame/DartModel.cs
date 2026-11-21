using UnityEngine;

namespace DoctorHelp
{
    // The 3D dart. Its pivot is the tip and local +Z points from the flights to the tip.
    [DisallowMultipleComponent]
    public sealed class DartModel : MonoBehaviour
    {
        [Tooltip("Drawn in this order; the flights come last because they face the camera.")]
        [SerializeField] private Renderer[] renderers = System.Array.Empty<Renderer>();

        internal void SetSorting(int order)
        {
            for (int index = 0; index < renderers.Length; index++)
                if (renderers[index] != null) renderers[index].sortingOrder = order + index;
        }
    }
}
