using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class ClinicRankingRow : MonoBehaviour
    {
        [SerializeField] private Text rank, clinicName, gold, rating;
        [SerializeField] private Image logo, background;
        [SerializeField] private Color alternateColor, playerColor;

        internal void Show(int position, ClinicRankingsCatalog.Standing standing)
        {
            rank.text = position.ToString(CultureInfo.InvariantCulture);
            clinicName.text = standing.Name;
            gold.text = standing.Gold.ToString("N0", CultureInfo.InvariantCulture);
            rating.text = standing.Rating.ToString("0.0", CultureInfo.InvariantCulture);
            logo.sprite = standing.Logo;
            logo.enabled = logo.sprite != null;
            background.color = standing.IsPlayer ? playerColor : (position % 2 == 1 ? alternateColor : Color.clear);
        }
    }
}
