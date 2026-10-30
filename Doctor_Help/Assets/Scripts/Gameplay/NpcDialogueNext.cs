using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DoctorHelp
{
    // Uses the authored Next collider without making every world collider a uGUI raycast target.
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class NpcDialogueNext : MonoBehaviour
    {
        [SerializeField] private NpcDialogue dialogue;
        [SerializeField] private Collider2D hitArea;
        private static readonly HashSet<NpcDialogueNext> active = new HashSet<NpcDialogueNext>();
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private bool wasHeld, armed;
        private int pointerId;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => active.Clear();
        private void OnEnable() { active.Add(this); wasHeld = ReadPointer(out _, out _); armed = false; }
        private void OnDisable() { active.Remove(this); armed = false; }
        private bool Hit(Vector2 point, Camera camera)
        {
            if (camera == null || hitArea == null || !hitArea.enabled || !hitArea.gameObject.activeInHierarchy) return false;
            var ray = camera.ScreenPointToRay(point);
            if (!new Plane(Vector3.forward, hitArea.transform.position).Raycast(ray, out float distance)) return false;
            Physics2D.SyncTransforms(); return hitArea.OverlapPoint(ray.GetPoint(distance));
        }
        public static bool IsOverButton(Vector2 point, Camera camera)
        {
            foreach (var button in active)
                if (button != null && button.isActiveAndEnabled && camera != null && button.gameObject.scene == camera.gameObject.scene && button.Hit(point, camera)) return true;
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
            if (held && !wasHeld) { pointerId = id; armed = Hit(point, Camera.main) && !OverUI(point); }
            if (armed && held && id != pointerId) armed = false;
            if (wasHeld && !held)
            {
                bool click = armed && id == pointerId && Hit(point, Camera.main) && !OverUI(point);
                armed = false;
                if (click && dialogue != null) dialogue.Next();
            }
            wasHeld = held;
        }
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
