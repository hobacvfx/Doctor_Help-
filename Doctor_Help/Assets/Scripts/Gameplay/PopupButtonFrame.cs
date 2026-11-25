using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DoctorHelp
{
    // Keep the source's complete end caps at the button's height. Horizontal resizing
    // then stretches only the middle, instead of turning the brass rivets into bars.
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Image))]
    public sealed class PopupButtonFrame : UIBehaviour
    {
        private Image frame;

        protected override void OnEnable() { base.OnEnable(); FitCaps(); }
        protected override void OnRectTransformDimensionsChange()
        { base.OnRectTransformDimensionsChange(); FitCaps(); }
        protected override void OnCanvasHierarchyChanged()
        { base.OnCanvasHierarchyChanged(); FitCaps(); }
#if UNITY_EDITOR
        protected override void OnValidate() { base.OnValidate(); FitCaps(); }
#endif

        private void FitCaps()
        {
            if (frame == null) frame = GetComponent<Image>();
            if (frame == null || frame.sprite == null || frame.rectTransform.rect.height <= 0) return;
            float reference = frame.canvas != null ? frame.canvas.referencePixelsPerUnit : 100;
            float scale = frame.sprite.rect.height / frame.rectTransform.rect.height *
                reference / frame.sprite.pixelsPerUnit;
            scale = Mathf.Max(.01f, scale);
            if (Mathf.Abs(frame.pixelsPerUnitMultiplier - scale) > .001f)
                frame.pixelsPerUnitMultiplier = scale;
        }
    }
}
