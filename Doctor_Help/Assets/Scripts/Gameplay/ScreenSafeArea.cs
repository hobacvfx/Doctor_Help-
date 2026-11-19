using UnityEngine;

namespace DoctorHelp
{
    public static class ScreenSafeArea
    {
        internal static void ApplyTo(RectTransform target)
        {
            int width = Screen.width, height = Screen.height;
            if (target == null || width <= 0 || height <= 0) return;
            Rect area = Current;
            var min = new Vector2(area.xMin / width, area.yMin / height);
            var max = new Vector2(area.xMax / width, area.yMax / height);
            if (target.anchorMin != min) target.anchorMin = min;
            if (target.anchorMax != max) target.anchorMax = max;
        }

        public static Rect Current
        {
            get
            {
                var full = new Rect(0, 0, Screen.width, Screen.height);
                var safe = Screen.safeArea;
                float left = Mathf.Clamp(safe.xMin, 0, Screen.width), right = Mathf.Clamp(safe.xMax, 0, Screen.width);
                float bottom = Mathf.Clamp(safe.yMin, 0, Screen.height), top = Mathf.Clamp(safe.yMax, 0, Screen.height);
                // Simulator/device changes may briefly expose the previous screen's rectangle.
                return right > left && top > bottom ? Rect.MinMaxRect(left, bottom, right, top) : full;
            }
        }
    }
}
