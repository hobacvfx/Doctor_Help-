using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DoctorHelp
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class PlayerHomeMovement : MonoBehaviour
    {
        private const float HoldDelay = 0.18f;
        private const float DragThreshold = 8f;

        [SerializeField] private Camera sceneCamera;
        [SerializeField] private Transform guideChest;
        [SerializeField, Min(0.01f)] private float moveSpeed = 10f;

        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private Rigidbody2D body;
        private CapsuleCollider2D bodyCollider;
        private float? destinationX;
        private float? shaftEntranceX;
        private ButtonControl steeringPress;
        private Vector2Control steeringPosition;
        private Vector2 pressPoint;
        private float pressTime;
        private bool steering;
        private bool aimingRock;
        internal bool IsWalkingAutomatically { get; private set; }
        internal bool IsTutorialLocked { get; private set; }
        internal bool IsWalkingManually => body != null && !IsRiding && !IsWalkingAutomatically &&
            !IsTutorialLocked && Mathf.Abs(body.linearVelocityX) > .01f;

        internal Vector2 Position => body.position;
        internal float FeetY => bodyCollider.bounds.min.y;
        internal Bounds HitBounds => bodyCollider.bounds;
        internal Vector3 GuideChestPosition => guideChest != null ? guideChest.position : HitBounds.center;
        internal Vector2 ScreenToRockAim(Vector2 drag) =>
            drag / Mathf.Max(1f, sceneCamera != null ? sceneCamera.pixelWidth : Screen.width);

        internal void SetRockAimLocked(bool locked)
        {
            aimingRock = locked;
            if (!locked) return;
            destinationX = null;
            ClearGesture();
            body.linearVelocityX = 0f;
        }
        internal bool IsRiding { get; private set; }
        internal void SetShaftEntranceLimit(float? entranceX) => shaftEntranceX = entranceX;

        internal void SetTutorialLocked(bool locked)
        {
            IsTutorialLocked = locked;
            if (locked) SetAutomaticDestination(null);
            ClearGesture();
        }

        internal void BeginAutomaticWalk()
        {
            ClearGesture();
            IsWalkingAutomatically = true;
            SetAutomaticDestination(null);
        }

        internal void SetAutomaticDestination(float? x)
        {
            destinationX = x;
            if (!x.HasValue && body != null && !IsRiding)
                body.linearVelocityX = 0f;
        }

        internal void EndAutomaticWalk()
        {
            IsWalkingAutomatically = false;
            SetAutomaticDestination(null);
        }

        internal void PlaceAtEntrance(Vector3 position)
        {
            destinationX = null;
            ClearGesture();
            body.linearVelocity = Vector2.zero;
            transform.position = position;
            body.position = position;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
        }

        private void Update()
        {
            if (IsRiding || aimingRock || IsTutorialLocked || IsWalkingAutomatically || sceneCamera == null)
                return;

            if (steeringPress == null)
            {
                if (!TryReadPress(out var press, out var position))
                    return;

                var point = position.ReadValue();
                if (!sceneCamera.pixelRect.Contains(point) || IsOverUI(point) ||
                    DeskActionButton.IsOverButton(point, sceneCamera))
                    return;

                steeringPress = press;
                steeringPosition = position;
                pressPoint = point;
                pressTime = Time.unscaledTime;
                SetDestination(point);
            }
            else if (!steeringPress.device.added || !steeringPress.isPressed ||
                steeringPress.wasReleasedThisFrame)
            {
                // A tap keeps its clicked destination. Releasing a steering gesture stops movement.
                if (steering) destinationX = null;
                ClearGesture();
            }
            else
            {
                var point = steeringPosition.ReadValue();
                steering |= Time.unscaledTime - pressTime >= HoldDelay ||
                    (point - pressPoint).sqrMagnitude >= DragThreshold * DragThreshold;
                if (!steering) return;

                var viewport = sceneCamera.pixelRect;
                point.x = Mathf.Clamp(point.x, viewport.xMin, viewport.xMax);
                point.y = Mathf.Clamp(point.y, viewport.yMin, viewport.yMax);
                SetDestination(point);
            }
        }

        private void SetDestination(Vector2 point)
        {
            var plane = new Plane(Vector3.forward, transform.position);
            var ray = sceneCamera.ScreenPointToRay(point);
            if (plane.Raycast(ray, out float distance))
                destinationX = ray.GetPoint(distance).x;
        }

        private void FixedUpdate()
        {
            if (IsRiding || aimingRock || IsTutorialLocked)
                return;

            if (destinationX.HasValue && shaftEntranceX.HasValue)
                destinationX = Mathf.Min(destinationX.Value,
                    shaftEntranceX.Value - bodyCollider.bounds.extents.x - 0.05f);

            float delta = destinationX.GetValueOrDefault(body.position.x) - body.position.x;
            if (Mathf.Abs(delta) <= 0.01f)
            {
                body.linearVelocityX = 0f;
                destinationX = null;
                return;
            }

            body.linearVelocityX = Mathf.Clamp(delta / Time.fixedDeltaTime, -moveSpeed, moveSpeed);
        }

        internal void BeginRide()
        {
            IsRiding = true;
            destinationX = null;
            ClearGesture();
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        internal void PlaceDuringRide(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        internal void EndRide()
        {
            body.position = transform.position;
            body.simulated = true;
            IsRiding = false;
        }

        private void OnDisable()
        {
            IsWalkingAutomatically = false;
            destinationX = null;
            ClearGesture();
            if (body != null && !IsRiding)
                body.linearVelocityX = 0f;
        }

        private void OnApplicationFocus(bool focused) { if (!focused) CancelGesture(); }
        private void OnApplicationPause(bool paused) { if (paused) CancelGesture(); }

        private void CancelGesture()
        {
            if (steering) destinationX = null;
            ClearGesture();
        }

        private void ClearGesture()
        {
            steeringPress = null;
            steeringPosition = null;
            steering = false;
        }

        private bool IsOverUI(Vector2 point)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            uiHits.Clear();
            eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = point }, uiHits);
            return uiHits.Count > 0;
        }

        private static bool TryReadPress(out ButtonControl press, out Vector2Control position)
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
                foreach (var touch in touchscreen.touches)
                    if (touch.press.wasPressedThisFrame)
                    {
                        press = touch.press;
                        position = touch.position;
                        return true;
                    }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                press = mouse.leftButton;
                position = mouse.position;
                return true;
            }

            press = null;
            position = null;
            return false;
        }
    }
}
