using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class ClinicStreetSign : MonoBehaviour
    {
        [SerializeField] private Text label;
        private PlayerProfileService profile;

        private void Start()
        {
            profile = PlayerProfileService.Instance;
            if (profile != null) profile.Changed += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            if (label == null) return;
            string name = profile != null && profile.IsLoaded ? profile.ClinicName.ToUpperInvariant() : "";
            if (label.text == name) return;
            label.text = name;
            // Keep a single line within the plate, including twenty very wide letters.
            int maximum = label.resizeTextMaxSize;
            label.resizeTextForBestFit = false;
            label.fontSize = maximum;
            float width = label.preferredWidth;
            if (width > label.rectTransform.rect.width && width > 0)
                label.fontSize = Mathf.Clamp(Mathf.FloorToInt(maximum * label.rectTransform.rect.width / width),
                    label.resizeTextMinSize, maximum);
        }

        private void OnDestroy()
        {
            if (profile != null) profile.Changed -= Refresh;
        }
    }
}
