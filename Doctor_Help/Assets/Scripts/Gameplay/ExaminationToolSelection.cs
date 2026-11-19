using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace DoctorHelp
{
    // Owns one selection and one pointer gesture for the entire examination scene.
    [DefaultExecutionOrder(-100)]
    public sealed class ExaminationToolSelection : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private ExaminationTool[] tools;
        [SerializeField] private Transform magnifier;
        [Tooltip("A new X-ray hold must start above this desk sprite. Defaults to the SpriteRenderer on this object. Once started, the hold can move over the desk and buttons until release.")]
        [SerializeField] private SpriteRenderer deskSurface;
        [Tooltip("Touch-only offset of the magnifier and scan together, in pixels at a 1920px screen height. Positive Y lifts the lens above the finger.")]
        [SerializeField] private Vector2 touchMagnifierOffset = new Vector2(0, 240);
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private bool available;
        private bool pointerWasHeld;
        private bool useGesture;
        private int pointerId = int.MinValue;
        public event System.Action<ExaminationToolKind> ToolSelected;
        public ExaminationTool Selected { get; private set; }
        public bool IsAvailable => available;
        public Transform Magnifier => magnifier;
        internal Camera TargetCamera => targetCamera;
        internal ExaminationTool GetTool(ExaminationToolKind kind) =>
            tools == null ? null : System.Array.Find(tools, tool => tool != null && tool.Kind == kind);
        public Vector2 ScreenPosition { get; private set; }
        internal Vector2 TouchMagnifierOffset => touchMagnifierOffset * (Screen.height / 1920f);
        public Vector2 MagnifierScreenPosition => ScreenPosition +
            (pointerId >= 0 ? TouchMagnifierOffset : Vector2.zero);
        public bool IsUsingTool { get; private set; }
        public bool IsUsingXray => IsUsingTool && IsSelected(ExaminationToolKind.Xray);
        public bool IsUsingStethoscope => IsUsingTool && IsSelected(ExaminationToolKind.Stethoscope);
        public bool IsUsingThermometer => IsUsingTool && IsSelected(ExaminationToolKind.Thermometer);
        public bool IsSelected(ExaminationToolKind kind) => available && Selected != null && Selected.Kind == kind;

        private void Awake()
        {
            if (deskSurface == null) TryGetComponent(out deskSurface);
        }

        public void SetAvailable(bool value)
        {
            available = value;
            ClearSelection();
            // Opening examination while a finger is down must not start a scan.
            pointerWasHeld = ReadPointer(out _, out _);
        }
        public void ClearSelection()
        {
            Selected = null;
            useGesture = IsUsingTool = false;
            if (tools != null)
                foreach (var tool in tools) if (tool != null) tool.SetSelected(false);
        }
        private void OnEnable()
        {
            ClearSelection();
            pointerWasHeld = ReadPointer(out _, out _);
        }
        private void OnDisable() => ClearSelection();
        private void Update()
        {
            bool held = ReadPointer(out var position, out int id);
            ScreenPosition = position;
            IsUsingTool = false;
            if (!available || !held || (Selected != null && !Selected.isActiveAndEnabled))
            {
                useGesture = false;
                if (Selected != null && !Selected.isActiveAndEnabled) ClearSelection();
            }
            else if (!pointerWasHeld)
            {
                pointerId = id;
                useGesture = false;
                if (!IsOverUI(position))
                {
                    var hit = FindTool(position);
                    if (hit != null)
                    {
                        bool deselect = Selected == hit;
                        ClearSelection();
                        if (!deselect)
                        {
                            Selected = hit; hit.SetSelected(true);
                            ToolSelected?.Invoke(hit.Kind);
                        }
                    }
                    else useGesture = Selected != null &&
                        (Selected.Kind != ExaminationToolKind.Xray || CanBeginXray(position));
                }
            }
            if (held && id != pointerId) useGesture = false;
            IsUsingTool = available && held && useGesture && Selected != null &&
                // X-ray owns its accepted gesture through release. Crossing a desk
                // button must not hide the offset lens or start another action.
                (Selected.Kind == ExaminationToolKind.Xray || !IsOverUI(position));
            pointerWasHeld = held;
        }
        private bool CanBeginXray(Vector2 position)
        {
            if (targetCamera == null || !targetCamera.pixelRect.Contains(position)) return false;
            if (deskSurface == null) return true;
            var ray = targetCamera.ScreenPointToRay(position);
            var plane = new Plane(Vector3.forward, deskSurface.transform.position);
            return plane.Raycast(ray, out float distance) && ray.GetPoint(distance).y > deskSurface.bounds.max.y;
        }
        private bool IsOverUI(Vector2 position)
        {
            if (DeskActionButton.IsOverButton(position, targetCamera)) return true;
            if (NpcDialogueNext.IsOverButton(position, targetCamera)) return true;
            if (EventSystem.current == null) return false;
            uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, uiHits);
            return uiHits.Count > 0;
        }
        private ExaminationTool FindTool(Vector2 position)
        {
            if (targetCamera == null || tools == null || !targetCamera.pixelRect.Contains(position)) return null;
            Ray ray = targetCamera.ScreenPointToRay(position);
            ExaminationTool best = null;
            foreach (var tool in tools)
            {
                if (tool == null || !tool.isActiveAndEnabled || !tool.Sprite.enabled || !FirstRunTutorial.AllowsWorld(tool)) continue;
                var hitArea = tool.HitArea;
                if (hitArea == null || !hitArea.isActiveAndEnabled) continue;
                var plane = new Plane(Vector3.forward, hitArea.transform.position);
                if (!plane.Raycast(ray, out float distance)) continue;
                if (!hitArea.OverlapPoint(ray.GetPoint(distance))) continue;
                if (best == null || tool.Sprite.sortingOrder > best.Sprite.sortingOrder) best = tool;
            }
            return best;
        }
        private static bool ReadPointer(out Vector2 position, out int id)
        {
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
            {
                position = touch.primaryTouch.position.ReadValue();
                id = touch.primaryTouch.touchId.ReadValue();
                return true;
            }
            var mouse = Mouse.current;
            position = mouse != null ? mouse.position.ReadValue() : default;
            id = -1;
            return mouse != null && mouse.leftButton.isPressed;
        }
    }
}




