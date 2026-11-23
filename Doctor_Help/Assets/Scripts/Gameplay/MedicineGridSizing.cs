using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class MedicineGridSizing : MonoBehaviour
    {
        private Vector2 lastSize;
        private void OnEnable() => Resize();
        private void OnRectTransformDimensionsChange() => Resize();
        private void Resize()
        {
            var rect = (RectTransform)transform;
            var grid = GetComponent<GridLayoutGroup>();
            if (grid == null || (lastSize == rect.rect.size && grid.cellSize.x > 0)) return;
            lastSize = rect.rect.size;
            int columns = Mathf.Max(1, grid.constraintCount);
            float side = Mathf.Max(1, (rect.rect.width - grid.padding.horizontal - grid.spacing.x * (columns - 1)) / columns);
            grid.cellSize = new Vector2(side, side);
        }
    }
}
