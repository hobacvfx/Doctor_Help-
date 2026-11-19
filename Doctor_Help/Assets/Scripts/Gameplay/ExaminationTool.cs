using UnityEngine;

namespace DoctorHelp
{
    public enum ExaminationToolKind { Xray = 0, Stethoscope = 1, Book = 2, Telephone = 3, Thermometer = 4 }

    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ExaminationTool : MonoBehaviour
    {
        [SerializeField] private ExaminationToolKind kind;
        [SerializeField] private GameObject selectedIcon;
        [SerializeField] private ToolSelectionGlow glow;
        [SerializeField] private Collider2D hitArea;
        public ExaminationToolKind Kind => kind;
        public bool IsSelected { get; private set; }
        public SpriteRenderer Sprite => GetComponent<SpriteRenderer>();
        public Collider2D HitArea => hitArea;

        private void Awake()
        {
            if (hitArea == null) TryGetComponent(out hitArea);
        }
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

