using System;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    public sealed class GivenMedicineSlot : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Button removeButton, removeIconButton;
        private string medicineId;
        private Action<string, Vector2> remove;
        internal Button RemoveButton => removeButton;
        private void Awake()
        {
            removeButton.onClick.AddListener(Remove);
            removeIconButton.onClick.AddListener(Remove);
        }
        internal void Bind(string id, Sprite sprite, Action<string, Vector2> onRemove, bool editable)
        {
            medicineId = id; icon.sprite = sprite; remove = onRemove;
            bool occupied = !string.IsNullOrEmpty(id);
            icon.gameObject.SetActive(occupied);
            removeButton.gameObject.SetActive(occupied);
            removeButton.interactable = occupied && editable;
            // Only the X removes a staged medicine; the card remains a drop target.
            removeIconButton.enabled = false;
        }
        private void Remove()
        {
            var canvas = icon.canvas.rootCanvas;
            var eye = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var screen = RectTransformUtility.WorldToScreenPoint(eye, icon.rectTransform.TransformPoint(icon.rectTransform.rect.center));
            remove?.Invoke(medicineId, screen);
        }
        private void OnDestroy()
        {
            removeButton.onClick.RemoveListener(Remove);
            removeIconButton.onClick.RemoveListener(Remove);
        }
    }
}
