using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    [DefaultExecutionOrder(100)]
    public sealed class TutorialSceneView : MonoBehaviour
    {
        private const double RequiredHoldSeconds = 3;

        [SerializeField] private TutorialOverlay overlay;
        [SerializeField] private SceneGameplayUI gameplay;
        [SerializeField] private ExaminationToolSelection examination;
        [SerializeField] private PatientSpawnPoint patients;
        [SerializeField] private TutorialStep hauntedThrow = new TutorialStep
        {
            Instruction = "Hold Throw Rock, drag to aim at a ghost, then release. Avoid hitting the patient."
        };
        private DiseaseReferencePopup book;
        private MagnifierLens lens;
        private TutorialStep holdStep;
        private double? holdStartedAt;
        internal bool HoldComplete { get; private set; }

        private void Awake()
        {
            if (TryGetComponent<DoctorBookController>(out var controller)) book = controller.Popup;
            if (examination != null && examination.Magnifier != null)
                lens = examination.Magnifier.GetComponent<MagnifierLens>();
        }

        internal void Show(Rect bounds, TutorialStep step, string medicineId)
        {
            Rect? readingArea = step.Action == TutorialAction.CloseDoctorBook ? ScreenBounds(book.ReadingArea) : null;
            var row = step.Action == TutorialAction.SelectMedicine ? gameplay.PillPopup?.MedicineList.TutorialRow(medicineId) : null;
            Rect? captionAvoidanceArea = row != null ? ScreenBounds((RectTransform)row.transform) : null;
            if (step.Action == TutorialAction.UseTool) holdStep = step;
            if (step.Action == TutorialAction.SelectMedicine && gameplay.PillPopup.IsDragging)
                bounds = new Rect(0, 0, Screen.width, Screen.height);
            Vector2? dragTo = null;
            if (step.Action == TutorialAction.SelectMedicine && patients.CurrentPatient != null)
            {
                dragTo = gameplay.PillPopup.GivenMedicines.ScreenCenter;
            }
            overlay.Present(bounds, step, readingArea, captionAvoidanceArea,
                showHand: step.Action != TutorialAction.ReviewSymptoms && !gameplay.PillPopup.IsDragging,
                dragTo: dragTo);
        }
        internal void Hide()
        {
            if (overlay == null) return;
            ResetHold();
            overlay.gameObject.SetActive(false);
        }
        internal void BlockWhileWaiting() => overlay.Block();
        internal void ResetHold()
        {
            holdStep = null;
            holdStartedAt = null;
            HoldComplete = false;
            if (overlay != null) overlay.HideHoldProgress();
        }

        internal bool ShowHauntedThrow(OfficeHaunting haunting)
        {
            if (overlay == null || !Ui(haunting.AimInput.GetComponent<Button>(), out var bounds)) return false;
            overlay.Present(bounds, hauntedThrow, showHand: !haunting.IsAiming, handScale: .45f);
            return true;
        }

        internal bool IsComplete(TutorialStep step, DailyGameLoop loop, string medicineId)
        {
            switch (step.Action)
            {
                case TutorialAction.SelectTool: return examination.IsSelected(step.Tool);
                case TutorialAction.OpenDoctorBook: return book != null && book.IsOpen;
                case TutorialAction.ReviewSymptoms: return overlay.ReviewConfirmed;
                case TutorialAction.CloseDoctorBook: return book != null && !book.IsOpen;
                case TutorialAction.OpenTreatment: return gameplay.PillPopup.gameObject.activeInHierarchy;
                case TutorialAction.SelectMedicine: return gameplay.PillPopup.SelectedMedicineIds.Contains(medicineId) ||
                    (patients.CurrentPatient != null && patients.CurrentPatient.HasReceivedMedicine && loop.SelectedPills?.Contains(medicineId) == true);
                case TutorialAction.RemoveMedicine: return !gameplay.PillPopup.SelectedMedicineIds.Contains(medicineId);
                case TutorialAction.GiveMedicine:
                    return !gameplay.PillPopup.gameObject.activeInHierarchy && loop.SelectedPills?.Contains(medicineId) == true;
                default: return false;
            }
        }

        private void LateUpdate()
        {
            if (holdStep == null) return;

            // The cloth controller moves the lens in LateUpdate at the default execution order.
            if (!examination.IsSelected(holdStep.Tool) || !examination.IsUsingTool)
            {
                holdStartedAt = null;
                HoldComplete = false;
                overlay.HideHoldProgress();
                return;
            }

            double now = Time.unscaledTimeAsDouble;
            if (holdStep.RequireWormInLens && !IsWormInLens())
                holdStartedAt = null;
            else
                holdStartedAt ??= now;

            double elapsed = now - (holdStartedAt ?? now);
            overlay.ShowHoldProgress(examination.ScreenPosition, examination.TouchMagnifierOffset,
                Mathf.Clamp01((float)(elapsed / RequiredHoldSeconds)));
            HoldComplete = elapsed >= RequiredHoldSeconds;
        }

        private bool IsWormInLens()
        {
            var patient = patients.CurrentPatient;
            if (lens == null || !lens.isActiveAndEnabled || patient == null ||
                !patient.TryGetComponent<WormInfestationAppearance>(out var appearance)) return false;
            var worm = appearance.SpawnedWorm;
            return worm != null && worm.enabled && worm.gameObject.activeInHierarchy &&
                lens.Covers(examination.TargetCamera, worm.bounds.center);
        }

        internal bool TryGetTarget(TutorialStep step, DailyGameLoop loop, TutorialPlan plan, bool reveal,
            out Rect bounds, out Component worldTarget)
        {
            bounds = default;
            worldTarget = null;
            switch (step.Action)
            {
                case TutorialAction.SelectTool:
                case TutorialAction.OpenDoctorBook:
                    if (examination == null || !examination.IsAvailable || loop.IsTransitioning) return false;
                    var tool = examination.GetTool(step.Action == TutorialAction.OpenDoctorBook ? ExaminationToolKind.Book : step.Tool);
                    if (tool == null || !tool.isActiveAndEnabled) return false;
                    worldTarget = tool;
                    return WorldBounds(tool.HitArea, examination.TargetCamera, out bounds);
                case TutorialAction.UseTool:
                    if (examination == null || !examination.IsAvailable || patients.CurrentPatient == null) return false;
                    Vector2 point = examination.TargetCamera.WorldToScreenPoint(patients.CurrentPatient.Heart.position);
                    var size = Vector2.Scale(step.HoldArea, new Vector2(Screen.width, Screen.height));
                    var offset = Vector2.Scale(step.HoldAreaOffset, new Vector2(Screen.width, Screen.height));
                    bounds = new Rect(point + offset - size * .5f, size);
                    return true;
                case TutorialAction.OpenTreatment:
                    return Desk(gameplay.GivePillsButton, out bounds, out worldTarget);
                case TutorialAction.CloseDoctorBook:
                    return Ui(book != null ? book.CloseButton : null, out bounds);
                case TutorialAction.ReviewSymptoms:
                    var disease = book != null ? book.TutorialDisease(plan.SicknessId, reveal) : null;
                    if (disease == null) return false;
                    bounds = ScreenBounds(disease);
                    return bounds.width > 0 && bounds.height > 0;
                case TutorialAction.SelectMedicine:
                    return Ui(gameplay.PillPopup?.MedicineList.TutorialButton(plan.MedicineId, reveal), out bounds);
                case TutorialAction.GiveMedicine:
                    return false; // Legacy ID retained; completion now happens on the last drop.
                case TutorialAction.RemoveMedicine:
                    return Ui(gameplay.PillPopup?.GivenMedicines.TutorialRemoveButton(plan.MedicineId), out bounds);
                default: return false;
            }
        }

        private static bool Desk(DeskActionButton button, out Rect bounds, out Component target)
        {
            bounds = default;
            target = button;
            return button != null && button.isActiveAndEnabled && button.Interactable &&
                WorldBounds(button.HitArea, button.TargetCamera, out bounds);
        }

        private static bool WorldBounds(Collider2D collider, Camera camera, out Rect result)
        {
            result = default;
            if (collider == null || !collider.isActiveAndEnabled || camera == null) return false;
            var bounds = collider.bounds;
            Vector2 min = camera.WorldToScreenPoint(bounds.min);
            Vector2 max = camera.WorldToScreenPoint(bounds.max);
            result = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return result.width > 0 && result.height > 0;
        }

        private static bool Ui(Button button, out Rect bounds)
        {
            bounds = default;
            if (button == null || !button.isActiveAndEnabled || !button.IsInteractable()) return false;
            bounds = ScreenBounds((RectTransform)button.transform);
            return bounds.width > 0 && bounds.height > 0;
        }

        private static Rect ScreenBounds(RectTransform rect)
        {
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.min));
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.max));
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
