using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    // Shows the authored disabled art (e.g. Button_Disabled) when a basement UI button is not interactable,
    // and dims its labels, while pressed feedback stays the Button's own color tint.
    [RequireComponent(typeof(Button))]
    public sealed class BasementButtonState : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private Sprite enabledSprite, disabledSprite;
        [SerializeField] private Graphic[] labels = new Graphic[0];
        [SerializeField, Range(0f, 1f)] private float disabledLabelAlpha = .7f;

        private Button button;
        private Color[] authored;
        private int shown = -1;

        private void Awake()
        {
            button = GetComponent<Button>();
            authored = new Color[labels.Length];
            for (int i = 0; i < labels.Length; i++) authored[i] = labels[i] != null ? labels[i].color : Color.white;
        }

        private void OnEnable() => shown = -1;

        private void LateUpdate()
        {
            int on = button.IsInteractable() ? 1 : 0;
            if (on == shown) return;
            shown = on;
            if (image != null && enabledSprite != null && disabledSprite != null)
                image.sprite = on == 1 ? enabledSprite : disabledSprite;
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == null) continue;
                var color = authored[i];
                if (on == 0) color.a *= disabledLabelAlpha;
                labels[i].color = color;
            }
        }
    }
}
