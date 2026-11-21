using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DoctorHelp
{
    // A scene-owned sprite/collider action, fixed to the desk rather than a screen Canvas.
    [DefaultExecutionOrder(-200), DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DeskActionButton : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private TextMesh caption;
        [SerializeField] private Collider2D hitArea;
        [SerializeField] private bool useSceneArtwork;
        [SerializeField, Range(.1f, 1)] private float pressedBrightness = .6f;
        [SerializeField, Min(.001f)] private float pressFadeSeconds = .06f;
        [SerializeField, Min(.001f)] private float releaseFadeSeconds = .14f;
        private static readonly HashSet<DeskActionButton> active = new HashSet<DeskActionButton>();
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private bool interactable = true, wasHeld, armed;
        private int pointerId;
        private Color authoredColor;
        private Color authoredCaptionColor;
        private float brightness = 1;
        public event Action Clicked;
        internal Collider2D HitArea => hitArea;
        internal Camera TargetCamera => targetCamera;
        public string Label { get => caption != null ? caption.text : ""; set { if (caption != null) { caption.text = value; caption.gameObject.SetActive(!string.IsNullOrWhiteSpace(value)); } } }
        public bool IsConfigured => (useSceneArtwork || (visual != null && visual.sprite != null)) &&
            hitArea != null && targetCamera != null;
        public bool Interactable
        {
            get => interactable;
            set
            {
                interactable = value;
                if (!value) { armed = false; brightness = 1; }
                ApplyTint();
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            active.Clear();
            WorldBlockers = 0;
        }

        // While above zero (e.g. during a shower) world buttons ignore presses.
        internal static int WorldBlockers { get; set; }
        private void Awake()
        {
            if (visual != null) authoredColor = visual.color;
            if (caption != null) authoredCaptionColor = caption.color;
        }
        private void OnEnable() { active.Add(this); ResetPress(); }
        private void OnDisable() { active.Remove(this); ResetPress(); }
        private void ResetPress()
        { wasHeld = ReadPointer(out _, out _); armed = false; brightness = 1; ApplyTint(); }
        private void ApplyTint()
        {
            float alpha = interactable ? 1 : .5f;
            if (visual != null) visual.color = new Color(authoredColor.r * brightness, authoredColor.g * brightness,
                authoredColor.b * brightness, authoredColor.a * alpha);
            if (caption != null) caption.color = new Color(authoredCaptionColor.r * brightness, authoredCaptionColor.g * brightness,
                authoredCaptionColor.b * brightness, authoredCaptionColor.a * alpha);
        }
        private bool Hit(Vector2 point, Camera camera)
        {
            if (camera == null || !camera.pixelRect.Contains(point) || hitArea == null || !hitArea.enabled) return false;
            var ray = camera.ScreenPointToRay(point);
            if (!new Plane(Vector3.forward, hitArea.transform.position).Raycast(ray, out float depth)) return false;
            Physics2D.SyncTransforms(); return hitArea.OverlapPoint(ray.GetPoint(depth));
        }
        public static bool IsOverButton(Vector2 point, Camera camera)
        {
            if (camera == null) return false;
            foreach (var button in active)
                if (button != null && button.isActiveAndEnabled && button.gameObject.scene == camera.gameObject.scene && button.Hit(point, camera)) return true;
            return false;
        }
        private bool OverUI(Vector2 point)
        {
            if (EventSystem.current == null) return false;
            uiHits.Clear(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, uiHits);
            return uiHits.Count > 0;
        }
        private void Update()
        {
            bool held = ReadPointer(out var point, out int id);
            bool allowed = interactable && WorldBlockers == 0 && FirstRunTutorial.AllowsWorld(this);
            if (held && !wasHeld)
            {
                pointerId = id;
                armed = allowed && Hit(point, targetCamera) && !OverUI(point);
            }
            if (armed && held && id != pointerId) armed = false;
            if (!held)
            {
                var touch = Touchscreen.current?.primaryTouch;
                bool touchEnded = touch != null && touch.press.wasReleasedThisFrame &&
                    touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended;
                bool pressedHere = wasHeld && id == pointerId && armed;
                if (touchEnded && (!wasHeld || id != pointerId))
                {
                    // A fast tap can begin and end between polls, including in Device Simulator.
                    Vector2 start = touch.startPosition.ReadValue();
                    pressedHere = Hit(start, targetCamera) && !OverUI(start);
                }
                bool click = allowed && pressedHere && (id == -1 || touchEnded) &&
                    Hit(point, targetCamera) && !OverUI(point);
                armed = false;
                if (click) Clicked?.Invoke();
            }
            wasHeld = held;
            bool pressed = armed && held && allowed && Hit(point, targetCamera) && !OverUI(point);
            float target = pressed ? pressedBrightness : 1;
            float seconds = pressed ? pressFadeSeconds : releaseFadeSeconds;
            brightness = Mathf.MoveTowards(brightness, target, (1 - pressedBrightness) * Time.unscaledDeltaTime / Mathf.Max(.001f, seconds));
            ApplyTint();
        }
        private void OnApplicationFocus(bool focused) { if (!focused) ResetPress(); }
        private void OnApplicationPause(bool paused) { if (paused) ResetPress(); }
        private static bool ReadPointer(out Vector2 point, out int id)
        {
            var touch = Touchscreen.current;
            if (touch != null && (touch.primaryTouch.press.isPressed || touch.primaryTouch.press.wasReleasedThisFrame))
            { point = touch.primaryTouch.position.ReadValue(); id = touch.primaryTouch.touchId.ReadValue(); return touch.primaryTouch.press.isPressed; }
            var mouse = Mouse.current; point = mouse != null ? mouse.position.ReadValue() : default; id = -1;
            return mouse != null && mouse.leftButton.isPressed;
        }
    }
}
