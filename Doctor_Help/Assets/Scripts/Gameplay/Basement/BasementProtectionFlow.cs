using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    // How protection gets onto a shelf, in three separate steps:
    //  a) RequestProtection — a shelf's world button opens the Protection popup for that shelf;
    //  b) the player confirms in the popups: Buy (Plant Shop), Equip (owned plant from storage) or Upgrade;
    //  c) BasementDefenseService binds the result (TryBuyPlant / TryEquipPlant / TryUpgradePlant) and saves it.
    // Every change obeys the existing spending rule: only during Preparation, with the profile saving immediately.
    public sealed class BasementProtectionFlow : MonoBehaviour
    {
        [SerializeField] private BasementProtectionPopup protectionPopup;
        [SerializeField] private BasementPlantShopPopup shopPopup;
        [Header("Feedback")]
        [SerializeField] private string notEnoughWater = "Not enough clean water.";
        [SerializeField] private string unavailable = "Plants can be changed only before the clinic opens.";
        [SerializeField] private string maxLevel = "Max Level reached.";
        [SerializeField] private string invalid = "This plant is no longer available.";
        [Tooltip("Ignores repeated taps on Buy/Equip/Upgrade for this long, so one double click is one transaction.")]
        [SerializeField, Min(0f)] private float repeatGuardSeconds = .35f;

        private Coroutine pendingOpen;
        private float nextActionTime;

        internal BasementDefenseService Service => BasementDefenseService.Instance;

        // True when buying, swapping and upgrading are allowed right now (same rule as the clinic shops).
        internal bool CanChangePlants
        {
            get
            {
                var service = Service;
                return service != null && service.IsRunning && service.CanSpend &&
                    ClinicShopPopup.CanInteract(FindFirstObjectByType<DailyGameLoop>());
            }
        }

        internal string UnavailableMessage => unavailable;

        // Step a.
        internal void RequestProtection(string slotId)
        {
            if (protectionPopup == null || string.IsNullOrEmpty(slotId)) return;
            if (pendingOpen != null) StopCoroutine(pendingOpen);
            pendingOpen = StartCoroutine(OpenAfterWorldClick(slotId));
        }

        private IEnumerator OpenAfterWorldClick(string slotId)
        {
            // Let the EventSystem finish the click on the world button before showing the modal.
            yield return null;
            pendingOpen = null;
            if (shopPopup != null) shopPopup.Close();
            protectionPopup.Open(slotId, this);
        }

        internal void OpenShop(string slotId, bool swap)
        {
            if (shopPopup != null) shopPopup.Open(slotId, swap, this);
        }

        // Step b → c. Each returns the feedback line to show, or null for a repeated tap that was ignored.
        internal string Buy(string slotId, BasementPlantProfile kind, out bool done)
        {
            done = false;
            if (!TakeAction()) return null;
            if (!CanChangePlants) return unavailable;
            var result = Service.TryBuyPlant(slotId, kind != null ? kind.PlantId : null);
            done = result == BasementPlantResult.Done;
            return done ? $"{kind.DisplayName} planted." : Message(result);
        }

        internal string Equip(string slotId, BasementOwnedPlant plant, out bool done)
        {
            done = false;
            if (!TakeAction()) return null;
            if (!CanChangePlants) return unavailable;
            var result = Service.TryEquipPlant(slotId, plant.InstanceId);
            done = result == BasementPlantResult.Done;
            return done ? $"{plant.Kind.DisplayName} Lv.{plant.Level} placed on the shelf." : Message(result);
        }

        internal string Upgrade(string slotId)
        {
            if (!TakeAction()) return null;
            var service = Service;
            var shelf = service != null ? service.ShelfPlant(slotId) : default;
            if (shelf.Kind == null) return invalid;
            if (!shelf.Kind.CanUpgradeFrom(shelf.Level)) return maxLevel;
            if (!CanChangePlants) return unavailable;
            var result = service.TryUpgradePlant(slotId);
            if (result != BasementPlantResult.Done) return Message(result);
            var plant = service.ShelfPlant(slotId);
            return $"{plant.Kind.DisplayName} reached Lv.{plant.Level}.";
        }

        private bool TakeAction()
        {
            if (Time.unscaledTime < nextActionTime) return false;
            nextActionTime = Time.unscaledTime + repeatGuardSeconds;
            return true;
        }

        private string Message(BasementPlantResult result) => result switch
        {
            BasementPlantResult.NotEnoughWater => notEnoughWater,
            BasementPlantResult.Unavailable => unavailable,
            BasementPlantResult.MaxLevel => maxLevel,
            _ => invalid
        };

        internal void StoredPlants(List<BasementOwnedPlant> result)
        {
            if (Service != null) Service.StoredPlants(result);
            else result.Clear();
        }

        private void OnDisable()
        {
            if (pendingOpen != null) StopCoroutine(pendingOpen);
            pendingOpen = null;
        }
    }
}
