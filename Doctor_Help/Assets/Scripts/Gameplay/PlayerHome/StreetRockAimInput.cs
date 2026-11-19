using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DoctorHelp
{
    public interface IRockAim
    {
        bool TryBeginAim();
        void UpdateAim(Vector2 drag);
        void ReleaseAim(Vector2 drag);
        void CancelAim();
    }

    [RequireComponent(typeof(Button))]
    public sealed class StreetRockAimInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IDragHandler, IEndDragHandler, ICancelHandler
    {
        internal IRockAim Throwing { private get; set; }
        private int? pointer;
        private Vector2 origin;

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || pointer.HasValue ||
                Throwing == null || !Throwing.TryBeginAim()) return;
            pointer = data.pointerId;
            origin = data.position;
        }

        public void OnInitializePotentialDrag(PointerEventData data) => data.useDragThreshold = false;

        public void OnDrag(PointerEventData data)
        {
            if (data.pointerId == pointer && Throwing != null) Throwing.UpdateAim(data.position - origin);
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (data.pointerId != pointer) return;
            pointer = null;
            if (Throwing == null) return;
            if (data is UnityEngine.InputSystem.UI.ExtendedPointerEventData extended &&
                extended.device is UnityEngine.InputSystem.Touchscreen touchscreen)
            {
                foreach (var touch in touchscreen.touches)
                    if (touch.touchId.ReadValue() == extended.touchId &&
                        touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
                    {
                        Throwing.CancelAim();
                        return;
                    }
            }
            Throwing.ReleaseAim(data.position - origin);
        }

        public void OnEndDrag(PointerEventData data)
        {
            if (data.pointerId == pointer) ResetPointer();
        }

        public void OnCancel(BaseEventData data) => ResetPointer();

        private void OnDisable() => ResetPointer();

        internal void ForgetPointer() => pointer = null;

        private void ResetPointer()
        {
            pointer = null;
            if (Throwing != null) Throwing.CancelAim();
        }
    }
}
