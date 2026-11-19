using UnityEngine;

namespace DoctorHelp
{
    public sealed class ClinicFurnitureSlot : MonoBehaviour
    {
        [SerializeField] private string slotId;
        [SerializeField] private GameObject[] defaultVisuals;
        [SerializeField] private ClinicShopItem[] variants;
        [SerializeField] private ClinicShopItem defaultItem;
        [SerializeField] private Transform movingPartParent;
        private PlayerProfileService profile;
        private ClinicShopItem shown;
        private GameObject instance;
        private Transform movingPart;
        internal GameObject EquippedVisual => instance;

        private void OnEnable()
        {
            profile = PlayerProfileService.Instance;
            if (profile != null) profile.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (profile != null) profile.Changed -= Refresh;
        }

        private void Refresh()
        {
            string id = profile != null && profile.IsLoaded ? profile.EquippedFurnitureId(slotId) : null;
            ClinicShopItem next = defaultItem;
            if (id != null)
                foreach (var variant in variants)
                    if (variant != null && variant.FurnitureSlot == slotId && variant.Id == id)
                    {
                        next = variant;
                        break;
                    }

            if (next != shown)
            {
                if (movingPart != null)
                {
                    movingPart.gameObject.SetActive(false);
                    Destroy(movingPart.gameObject);
                }
                if (instance != null)
                {
                    instance.SetActive(false);
                    Destroy(instance);
                }
                instance = next != null ? Instantiate(next.EquippedVisual, transform, false) : null;
                movingPart = null;
                if (movingPartParent != null && instance != null &&
                    instance.TryGetComponent<ClinicShopItem>(out var visual))
                {
                    movingPart = visual.MovingPart;
                    if (movingPart != null) movingPart.SetParent(movingPartParent, false);
                }
                shown = next;
            }
            foreach (var visual in defaultVisuals) visual.SetActive(next == null);
        }
    }
}
