using UnityEngine;
using UnityEngine.UI;
namespace DoctorHelp
{
    public sealed class StoreInventoryUI : MonoBehaviour
    {
        [SerializeField] private DeskActionButton inventoryButton;
        [SerializeField] private GameObject inventoryPopup;
        [SerializeField] private Button inventoryOutside, inventoryClose;
        [SerializeField] private ClinicShopPopup shopInventory;
        private void Awake()
        {
            if (inventoryButton != null) inventoryButton.Clicked += OpenInventory;
            if (inventoryOutside != null) inventoryOutside.onClick.AddListener(Close);
            if (inventoryClose != null) inventoryClose.onClick.AddListener(Close);
            Close();
        }
        private void OpenInventory()
        {
            if (shopInventory != null)
            {
                shopInventory.Open();
                return;
            }
            var loop = FindFirstObjectByType<DailyGameLoop>();
            if (inventoryPopup != null && ClinicShopPopup.CanInteract(loop))
                PopupAnimation.Show(inventoryPopup);
        }
        public void Close()
        {
            if (inventoryPopup != null) PopupAnimation.Hide(inventoryPopup);
            if (shopInventory != null) shopInventory.Close();
        }
        private void OnDestroy()
        {
            if (inventoryButton != null) inventoryButton.Clicked -= OpenInventory;
            if (inventoryOutside != null) inventoryOutside.onClick.RemoveListener(Close);
            if (inventoryClose != null) inventoryClose.onClick.RemoveListener(Close);
        }
    }
}
