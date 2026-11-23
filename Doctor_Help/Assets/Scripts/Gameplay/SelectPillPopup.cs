using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

namespace DoctorHelp
{
    // Proposes decisions; the day-flow state owns transactions and departure.
    public sealed class SelectPillPopup : MonoBehaviour
    {
        [SerializeField] private MedicineListView medicines;
        [SerializeField] private Button outsideButton, closeButton;
        [SerializeField] private GivenMedicineTray givenMedicines;
        [SerializeField] private Text errorText;
        [SerializeField] private CanvasGroup popupVisuals;
        [SerializeField] private Image dragIcon;
        [SerializeField] private PatientSpawnPoint patients;
        [SerializeField] private Camera patientCamera;
        [SerializeField] private ParticleSystem treatmentPuff;
        [SerializeField, Min(0)] private float touchLift = 70;
        [SerializeField, Min(1)] private float dragIconScale = 2.5f;
        public event Action GiveRequested, CloseRequested;
        public IReadOnlyList<string> SelectedMedicineIds => DraftIsCurrent
            ? draftPatient.StagedMedicineIds : Array.Empty<string>();
        internal MedicineListView MedicineList => medicines;
        public int RequiredSlotCount { get; private set; }
        internal GivenMedicineTray GivenMedicines => givenMedicines;
        internal bool IsDragging => pointerId != int.MinValue;
        public bool IsConfigured => medicines != null && outsideButton != null && outsideButton.targetGraphic != null &&
            closeButton != null && closeButton.targetGraphic != null && errorText != null &&
            popupVisuals != null && dragIcon != null && treatmentPuff != null &&
            givenMedicines != null && givenMedicines.IsConfigured;
        private bool initialized, submitting, emptyAttempted;
        private int pointerId = int.MinValue;
        private string medicineId, consultationId;
        private PatientExaminationProfile dragPatient;
        private PatientExaminationProfile draftPatient;
        private string draftConsultationId;
        private bool DraftIsCurrent => draftPatient != null && patients != null &&
            patients.CurrentPatient == draftPatient && draftPatient.ConsultationId == draftConsultationId;
        private bool CanEditDraft => isActiveAndEnabled && DraftIsCurrent && !patients.IsDeparting &&
            draftPatient.HasArrived && !draftPatient.HasReceivedMedicine && !submitting;
        private readonly List<RaycastResult> hits = new List<RaycastResult>();
        private void Awake() => Initialize();
        private void Initialize()
        {
            if (initialized) return;
            if (!IsConfigured) throw new InvalidOperationException("Treatment popup references are incomplete.");
            outsideButton.onClick.AddListener(Close);
            closeButton.onClick.AddListener(Close);
            initialized = true;
        }
        public void PrepareForPatient()
        {
            Initialize(); CancelDrag(); submitting = false;
            draftPatient = patients != null ? patients.CurrentPatient : null;
            draftConsultationId = draftPatient != null ? draftPatient.ConsultationId : null;
            RequiredSlotCount = draftPatient != null ? TreatmentSlotRequirements.Count(draftPatient) : 0;
            if (RequiredSlotCount == 0 && draftPatient != null) draftPatient.ClearStagedMedicines();
            ShowError(""); RefreshDraft();
        }
        private void OnEnable()
        {
            Initialize();
            emptyAttempted = false;
            RefreshDraft();
        }
        public void ShowError(string error)
        {
            submitting = false;
            errorText.text = error;
            errorText.gameObject.SetActive(!string.IsNullOrEmpty(error));
            RefreshDraft();
        }
        internal void BeginMedicineDrag(string id, Image source, PointerEventData data)
        {
            if (!CanEditDraft || IsDragging || SelectedMedicineIds.Count >= RequiredSlotCount || data.button != PointerEventData.InputButton.Left ||
                patients == null || patients.IsDeparting || patientCamera == null || source == null || source.sprite == null) return;
            var patient = patients.CurrentPatient;
            if (patient == null || !patient.HasArrived) return;
            dragPatient = patient; consultationId = patient.ConsultationId;
            medicineId = id; pointerId = data.pointerId;
            ShowError("");
            dragIcon.sprite = source.sprite;
            dragIcon.rectTransform.sizeDelta = source.rectTransform.rect.size * dragIconScale;
            dragIcon.gameObject.SetActive(true);
            RefreshDraft();
            MoveMedicineDrag(data);
        }
        internal void MoveMedicineDrag(PointerEventData data)
        {
            if (!IsDragging || data.pointerId != pointerId) return;
            var canvas = dragIcon.canvas.rootCanvas;
            Camera eye = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 point = data.position;
            bool touch = data is ExtendedPointerEventData extended
                ? extended.pointerType == UIPointerType.Touch : data.pointerId >= 0;
            if (touch) point.y += touchLift * canvas.scaleFactor;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)dragIcon.transform.parent,
                point, eye, out var local)) dragIcon.rectTransform.anchoredPosition = local;
        }
        internal void EndMedicineDrag(PointerEventData data)
        {
            if (!IsDragging || data.pointerId != pointerId) return;
            var patient = dragPatient;
            Vector3 point = default;
            bool valid = CanEditDraft && patient != null && patient == draftPatient && patients != null && !patients.IsDeparting && patients.CurrentPatient == patient &&
                patient.HasArrived && patient.ConsultationId == consultationId &&
                SelectedMedicineIds.Count < RequiredSlotCount &&
                TryDropPoint(patient, data.position, out point) && !BlockedByOtherPopup(data);
            string id = medicineId;
            CancelDrag();
            if (!valid) return;
            if (patient.Profile.AllSickness.PillProfile.Find(id) == null) return;
            if (patient.StageMedicine(id)) PlayPuff(patient, point);
            RefreshDraft();
            SubmitIfFull();
        }
        private void SubmitIfFull()
        {
            if (!CanEditDraft || IsDragging || SelectedMedicineIds.Count != RequiredSlotCount ||
                (RequiredSlotCount == 0 && !draftPatient.CanConfirmWithoutMedicine)) return;
            submitting = true;
            RefreshDraft();
            GiveRequested?.Invoke();
        }
        private void LateUpdate()
        {
            // A cleared Haunted-only case needs no medicine. Wait until the flow has
            // entered TreatmentDecision before submitting its empty treatment.
            if (RequiredSlotCount == 0 && !emptyAttempted) { emptyAttempted = true; SubmitIfFull(); }
        }
        private void RemoveMedicine(string id, Vector2 screen)
        {
            if (!CanEditDraft || IsDragging || !draftPatient.RemoveStagedMedicine(id)) return;
            ShowError("");
            Headbar.Current?.PlayUiPuff(screen);
            RefreshDraft();
        }
        private void RefreshDraft()
        {
            if (givenMedicines == null) return;
            givenMedicines.Refresh(SelectedMedicineIds, RequiredSlotCount, RemoveMedicine, CanEditDraft && !IsDragging);
        }
        private bool BlockedByOtherPopup(PointerEventData data)
        {
            if (EventSystem.current == null) return true;
            hits.Clear(); EventSystem.current.RaycastAll(data, hits);
            foreach (var hit in hits)
            {
                if (hit.gameObject.transform.IsChildOf(transform)) continue;
                return true;
            }
            return false;
        }
        private bool TryDropPoint(PatientExaminationProfile patient, Vector2 screen, out Vector3 point)
        {
            point = default;
            if (givenMedicines.ContainsScreenPoint(screen)) { point = patient.Heart.position; return true; }
            var ray = patientCamera.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.forward, patient.Heart.position);
            if (!plane.Raycast(ray, out float distance)) return false;
            point = ray.GetPoint(distance);
            // Visible body regions only: excludes dialogue, anatomy illustrations and ghosts.
            if (Contains(patient.TreasureBody, point)) return true;
            foreach (var sprite in patient.GetComponentsInChildren<SpriteRenderer>())
                if (sprite.name == "Head" && Contains(sprite, point)) return true;
            if (patient.TryGetComponent<NpcAppearanceProfile>(out var appearance))
                foreach (var slot in appearance.Slots)
                    if (slot != null && slot.id == "Outfit" && slot.attachPoint != null)
                        foreach (var sprite in slot.attachPoint.GetComponentsInChildren<SpriteRenderer>())
                            if (Contains(sprite, point)) return true;
            return false;
        }
        private static bool Contains(SpriteRenderer sprite, Vector3 point)
        {
            if (sprite == null || !sprite.enabled || !sprite.gameObject.activeInHierarchy || sprite.sprite == null) return false;
            var bounds = sprite.bounds;
            if (bounds.extents.x <= 0 || bounds.extents.y <= 0) return false;
            var normalized = ((Vector2)point - (Vector2)bounds.center) / (Vector2)bounds.extents;
            return normalized.sqrMagnitude <= 1;
        }
        private void PlayPuff(PatientExaminationProfile patient, Vector3 point)
        {
            var puff = Instantiate(treatmentPuff, point, treatmentPuff.transform.rotation, patients.transform);
            int order = int.MinValue, layer = 0;
            foreach (var sprite in patient.GetComponentsInChildren<SpriteRenderer>())
                if (sprite.enabled && sprite.sortingOrder >= order) { order = sprite.sortingOrder; layer = sprite.sortingLayerID; }
            foreach (var renderer in puff.GetComponentsInChildren<ParticleSystemRenderer>(true))
            { renderer.sortingLayerID = layer; renderer.sortingOrder = order == int.MinValue ? 1 : order + 1; }
            var main = puff.main; main.loop = false;
            puff.Play(true); Destroy(puff.gameObject, 5);
        }
        private void CancelDrag()
        {
            dragPatient = null; medicineId = consultationId = null; pointerId = int.MinValue;
            if (popupVisuals != null) popupVisuals.alpha = 1;
            if (dragIcon != null) dragIcon.gameObject.SetActive(false);
            RefreshDraft();
        }
        private void Close() { if (!IsDragging && !submitting) CloseRequested?.Invoke(); }
        private void OnDisable() => CancelDrag();
        private void OnApplicationPause(bool paused) { if (paused) CancelDrag(); }
        private void OnApplicationFocus(bool focused) { if (!focused) CancelDrag(); }
        private void OnDestroy()
        {
            if (!initialized) return;
            outsideButton.onClick.RemoveListener(Close); closeButton.onClick.RemoveListener(Close);
        }
    }
}
