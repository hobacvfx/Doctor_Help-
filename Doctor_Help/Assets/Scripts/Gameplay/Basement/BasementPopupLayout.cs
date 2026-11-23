using UnityEngine;

namespace DoctorHelp
{
    // Keeps a basement popup panel inside the Safe Area on any screen: the authored size is used when it fits,
    // otherwise the whole panel scales down uniformly (text and sprites keep their proportions).
    internal static class BasementPopupLayout
    {
        // reservedHeight: room kept below the panel inside the Safe Area (feedback line), which is placed there.
        // topOverhang: art standing above the panel's top edge (the brass clip), kept on screen too.
        internal static void Fit(RectTransform safeArea, RectTransform panel, Vector2 size, float margin,
            float reservedHeight, float topOverhang, bool stretchHeight, RectTransform below)
        {
            ScreenSafeArea.ApplyTo(safeArea);
            if (safeArea == null || panel == null) return;
            var area = safeArea.rect;
            if (area.width <= 0f || area.height <= 0f) return;
            float availableWidth = area.width - 2f * margin;
            float availableHeight = area.height - 2f * margin - reservedHeight - topOverhang;
            var target = size;
            if (stretchHeight) target.y = Mathf.Max(Mathf.Min(size.y, availableHeight), size.y * .5f);
            float scale = Mathf.Min(1f, availableWidth / target.x, availableHeight / target.y);
            if (panel.sizeDelta != target) panel.sizeDelta = target;
            var scaled = new Vector3(scale, scale, 1f);
            if (panel.localScale != scaled) panel.localScale = scaled;
            var position = new Vector2(0f, (reservedHeight - topOverhang * scale) * .5f);
            if (panel.anchoredPosition != position) panel.anchoredPosition = position;
            if (below == null) return;
            var line = new Vector2(0f, position.y - target.y * scale * .5f - reservedHeight * .5f);
            if (below.anchoredPosition != line) below.anchoredPosition = line;
        }
    }
}
