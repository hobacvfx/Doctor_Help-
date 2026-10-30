using UnityEngine;

namespace DoctorHelp
{
    public enum ExaminationToolKind { Xray = 0, Stethoscope = 1, Book = 2, Telephone = 3, Thermometer = 4 }

    [RequireComponent(typeof(SpriteRenderer), typeof(PolygonCollider2D))]
    public sealed class ExaminationTool : MonoBehaviour
    {
        [SerializeField] private ExaminationToolKind kind;
        [SerializeField] private GameObject selectedIcon;
        [SerializeField] private ToolSelectionGlow glow;
        public ExaminationToolKind Kind => kind;
        public bool IsSelected { get; private set; }
        public SpriteRenderer Sprite => GetComponent<SpriteRenderer>();
        public void SetSelected(bool selected)
        {
            IsSelected = selected && isActiveAndEnabled;
            if (selectedIcon != null) selectedIcon.SetActive(IsSelected);
            if (glow != null) glow.SetSelected(IsSelected);
        }
        private void OnEnable() => SetSelected(false);
        private void OnDisable() => SetSelected(false);
    }
}

