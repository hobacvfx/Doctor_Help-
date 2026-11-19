using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace DoctorHelp
{
    public sealed class MedicineListView : MonoBehaviour
    {
        [SerializeField] private PillProfile pills;
        [SerializeField] private Transform content;
        [SerializeField] private MedicineRow rowPrefab;
        [SerializeField] private UnityEngine.UI.Text emptyText;
        public event Action SelectionChanged;
        private readonly List<string> selected = new List<string>();
        private readonly Dictionary<string, MedicineRow> rows = new Dictionary<string, MedicineRow>();
        public IReadOnlyList<string> SelectedIds => selected.AsReadOnly();
        internal long SelectedCost => pills.TryGetCost(selected, out long cost) ? cost : 0;
        internal MedicineRow TutorialRow(string id) =>
            rows.TryGetValue(id, out var row) && row != null && row.gameObject.activeInHierarchy ? row : null;

        internal Button TutorialButton(string id, bool reveal)
        {
            var row = TutorialRow(id);
            if (row == null) return null;
            var button = row.SelectButton;
            if (!reveal) return button;

            Canvas.ForceUpdateCanvases();
            var scroll = content.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.viewport == null) return button;
            var viewport = scroll.viewport;
            var target = (RectTransform)row.transform;
            float rowBottom = viewport.InverseTransformPoint(target.TransformPoint(target.rect.min)).y;
            float rowTop = viewport.InverseTransformPoint(target.TransformPoint(target.rect.max)).y;
            float extent = scroll.content.rect.height - viewport.rect.height;
            if (extent > 0 && (rowBottom < viewport.rect.yMin || rowTop > viewport.rect.yMax))
            {
                float rowCenter = (rowBottom + rowTop) * .5f;
                float current = (1 - Mathf.Clamp01(scroll.verticalNormalizedPosition)) * extent;
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 1 - Mathf.Clamp01((current + viewport.rect.center.y - rowCenter) / extent);
                Canvas.ForceUpdateCanvases();
            }
            return button;
        }
        private void OnEnable() => Refresh();
        public void ClearSelection() { selected.Clear(); Refresh(); SelectionChanged?.Invoke(); }
        private void Refresh()
        {
            if (pills == null) return;
            var valid = new HashSet<string>();
            bool changed = selected.RemoveAll(id => pills.Find(id) == null) > 0;
            foreach (var pill in pills.Pills)
            {
                if (pill == null || !valid.Add(pill.Id)) continue;
                if (!rows.TryGetValue(pill.Id, out var row))
                {
                    row = Instantiate(rowPrefab, content, false);
                    row.Bind(pill, Toggle);
                    rows.Add(pill.Id, row);
                }
                row.Refresh(pill, selected.Contains(pill.Id));
            }
            var removed = new List<string>();
            foreach (var pair in rows) if (!valid.Contains(pair.Key)) { Destroy(pair.Value.gameObject); removed.Add(pair.Key); }
            foreach (var id in removed) rows.Remove(id);
            if (emptyText != null) emptyText.gameObject.SetActive(valid.Count == 0);
            if (changed) SelectionChanged?.Invoke();
        }
        private void Toggle(string id)
        {
            if (!selected.Remove(id)) selected.Add(id);
            Refresh(); SelectionChanged?.Invoke();
        }
    }
}
