using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    /// <summary>Shows the clinic clock's day/night period; never changes the clock or day flow.</summary>
    [DisallowMultipleComponent]
    public sealed class ClinicClockPeriodIcon : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Sprite sun;
        [SerializeField] private Sprite moon;
        [SerializeField, Range(0, 23)] private int dayStartsAtHour = 6;
        [SerializeField, Range(0, 23)] private int nightStartsAtHour = 18;

        public void SetMinute(int minuteOfDay)
        {
            if (icon == null) return;
            int minute = ((minuteOfDay % 1440) + 1440) % 1440;
            int start = dayStartsAtHour * 60, end = nightStartsAtHour * 60;
            bool daytime = start < end ? minute >= start && minute < end : minute >= start || minute < end;
            var sprite = daytime ? sun : moon;
            if (icon.sprite != sprite) icon.sprite = sprite;
            icon.enabled = sprite != null;
        }
    }
}
