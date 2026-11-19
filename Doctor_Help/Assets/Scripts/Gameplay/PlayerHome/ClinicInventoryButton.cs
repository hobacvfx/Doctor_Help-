using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class ClinicInventoryButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private ClinicShopPopup popup;

        private void OnEnable() => button.onClick.AddListener(popup.Open);
        private void OnDisable() => button.onClick.RemoveListener(popup.Open);
    }
}
