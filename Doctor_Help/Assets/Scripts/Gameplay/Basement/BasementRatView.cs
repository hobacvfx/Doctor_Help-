using UnityEngine;

namespace DoctorHelp
{
    // Visual of one simulated rat: the 10-frame run flipbook (Animator, loops) seen from above, head up.
    // A slowed rat runs its flipbook slower and takes a frosty tint.
    [DisallowMultipleComponent]
    public sealed class BasementRatView : MonoBehaviour
    {
        [Tooltip("Rotation (degrees) added so the head points along the running direction; the art faces up.")]
        [SerializeField] private float headingOffset = -90f;
        [SerializeField] private Color slowTint = new Color(.72f, .88f, 1f, 1f);

        private SpriteRenderer sprite;
        private Animator animator;
        private Color normalColor = Color.white;
        private float shownSpeed = -1f;

        internal int RatId { get; set; }

        private void Awake()
        {
            sprite = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            if (sprite != null) normalColor = sprite.color;
        }

        // speed: the simulated speed multiplier (1 = normal).
        internal void ShowSpeed(float speed)
        {
            if (Mathf.Approximately(speed, shownSpeed)) return;
            shownSpeed = speed;
            if (animator != null) animator.speed = speed;
            if (sprite != null) sprite.color = speed < .999f ? slowTint : normalColor;
        }

        internal void Place(Vector3 position, Vector2 heading)
        {
            transform.position = position;
            if (heading.sqrMagnitude > 1e-8f)
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg + headingOffset);
        }
    }
}
