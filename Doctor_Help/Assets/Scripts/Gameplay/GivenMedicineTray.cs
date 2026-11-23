using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    // Scene-owned receiving cards follow the consultation's chest while this popup is open.
    public sealed class GivenMedicineTray : MonoBehaviour
    {
        [SerializeField] private PillProfile pills;
        [SerializeField] private RectTransform content;
        [SerializeField] private GivenMedicineSlot slotPrefab;
        [SerializeField] private GridLayoutGroup medicineGrid;
        [SerializeField] private PatientSpawnPoint patients;
        [SerializeField] private Camera patientCamera;
        [SerializeField] private Vector2 chestOffset;
        [SerializeField, Min(0)] private float screenPadding = 16;
        private readonly List<GivenMedicineSlot> slots = new List<GivenMedicineSlot>();
        private readonly List<string> ids = new List<string>();
        private float lastSide = -1;
        private readonly Vector3[] corners = new Vector3[4];
        internal bool IsConfigured => pills != null && content != null && slotPrefab != null && medicineGrid != null && patients != null && patientCamera != null;
        internal Vector2 ScreenCenter => RectTransformUtility.WorldToScreenPoint(null, ((RectTransform)transform).TransformPoint(((RectTransform)transform).rect.center));
        internal Button TutorialRemoveButton(string id)
        {
            int index = ids.IndexOf(id);
            return index >= 0 && index < slots.Count ? slots[index].RemoveButton : null;
        }
        internal bool ContainsScreenPoint(Vector2 screen) => gameObject.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, screen);
        internal void Refresh(IReadOnlyList<string> medicineIds, int requiredCount, Action<string, Vector2> remove, bool editable)
        {
            ids.Clear();
            for (int i = 0; i < requiredCount; i++)
            {
                string id = i < medicineIds.Count ? medicineIds[i] : null;
                var pill = id != null ? pills.Find(id) : null;
                if (id != null && pill == null) throw new InvalidOperationException("Staged medicine no longer exists: " + id);
                if (i >= slots.Count) slots.Add(Instantiate(slotPrefab, content, false));
                slots[i].gameObject.SetActive(true);
                slots[i].Bind(id, pill?.Sprite, remove, editable);
                ids.Add(id);
            }
            for (int i = requiredCount; i < slots.Count; i++) slots[i].gameObject.SetActive(false);
            Resize();
            FollowPatient();
        }
        private void LateUpdate()
        {
            if (medicineGrid != null && !Mathf.Approximately(lastSide, CellSide())) Resize();
            FollowPatient();
        }
        private float CellSide() => medicineGrid.cellSize.x * Mathf.Abs(medicineGrid.transform.lossyScale.x / content.lossyScale.x);
        private void Resize()
        {
            if (medicineGrid == null) return;
            lastSide = CellSide();
            foreach (var slot in slots)
            {
                var layout = slot.GetComponent<LayoutElement>();
                layout.minWidth = layout.preferredWidth = layout.minHeight = layout.preferredHeight = lastSide;
            }
            float gap = content.GetComponent<HorizontalLayoutGroup>().spacing;
            ((RectTransform)transform).sizeDelta = new Vector2(ids.Count * lastSide + Mathf.Max(0, ids.Count - 1) * gap, lastSide);
            LayoutRebuilder.MarkLayoutForRebuild(content);
        }
        private void FollowPatient()
        {
            var patient = patients != null ? patients.CurrentPatient : null;
            if (patient == null || patient.Heart == null || patientCamera == null) return;
            var rect = (RectTransform)transform;
            var world = patient.Heart.position + (Vector3)chestOffset;
            if (patient.TreasureBody != null) world.x = patient.TreasureBody.bounds.center.x + chestOffset.x;
            Vector2 screen = patientCamera.WorldToScreenPoint(world);
            rect.GetWorldCorners(corners);
            float halfWidth = Mathf.Abs(corners[2].x - corners[0].x) * .5f;
            float halfHeight = Mathf.Abs(corners[2].y - corners[0].y) * .5f;
            Rect safe = Screen.safeArea;
            screen.x = Mathf.Clamp(screen.x, safe.xMin + halfWidth + screenPadding, safe.xMax - halfWidth - screenPadding);
            screen.y = Mathf.Clamp(screen.y, safe.yMin + halfHeight + screenPadding, safe.yMax - halfHeight - screenPadding);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rect.parent, screen, null, out var local)) rect.anchoredPosition = local;
        }
    }
}
