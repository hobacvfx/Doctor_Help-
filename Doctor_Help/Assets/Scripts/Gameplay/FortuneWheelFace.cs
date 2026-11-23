using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class FortuneWheelFace : MonoBehaviour
    {
        [SerializeField] private FortuneWheelCatalog catalog;
        [SerializeField] private RectTransform rotor;
        [SerializeField] private Text[] amounts;
        [SerializeField] private Image[] icons;
        public RectTransform Rotor => rotor;
        public FortuneWheelCatalog Catalog => catalog;
        public bool IsConfigured => catalog != null && catalog.IsConfigured && rotor != null &&
            amounts != null && icons != null && amounts.Length == FortuneWheelCatalog.SectorCount &&
            icons.Length == FortuneWheelCatalog.SectorCount &&
            System.Array.TrueForAll(amounts, text => text != null) && System.Array.TrueForAll(icons, icon => icon != null);

        private void OnEnable() => Refresh();
        public void Refresh()
        {
            if (!IsConfigured) return;
            for (int i = 0; i < amounts.Length; i++)
            {
                amounts[i].text = catalog.Rewards[i].amount.ToString();
                icons[i].sprite = catalog.Rewards[i].icon;
            }
        }
        internal Vector2 RewardScreenPoint(int index) =>
            RectTransformUtility.WorldToScreenPoint(null, icons[index].rectTransform.position);
    }
}
