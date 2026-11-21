using UnityEngine;

namespace DoctorHelp
{
    // A floating balloon of the Dart minigame. The transform sits at the end of the string,
    // where the reward card hangs, so the card stays attached while the balloon drifts.
    [DisallowMultipleComponent]
    public sealed class DartBalloon : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer balloon;
        [SerializeField] private Transform card;
        [SerializeField] private SpriteRenderer cardArt;
        [Tooltip("Center of the balloon body that a dart has to hit, relative to the string end.")]
        [SerializeField] private Vector2 bodyCenter = new Vector2(0f, 1.88f);
        [SerializeField] private Vector2 bodyRadius = new Vector2(.5f, .63f);
        [SerializeField, Min(0f)] private float bobHeight = .06f;
        [SerializeField, Min(0f)] private float cardSwayDegrees = 12f;
        [SerializeField, Min(0f)] private float cardGravity = 16f;

        private Rect area;
        private Vector2 speedRange, anchor, target;
        private float speed, phase, swing, killY, cardSpin;
        private Vector3 cardHome, cardVelocity;
        private bool popped, placed;

        internal bool IsAvailable => gameObject.activeSelf && !popped;
        internal Vector3 BodyCenter => transform.TransformPoint(bodyCenter);
        internal Sprite CardSprite { set => cardArt.sprite = value; }

        private void Awake() => cardHome = card.localPosition;

        // area is in the parent's space; killY is the world height below which a falling card is hidden.
        internal void Place(Rect area, Vector2 speedRange, float killY)
        {
            this.speedRange = speedRange;
            popped = false;
            balloon.enabled = true;
            card.gameObject.SetActive(true);
            card.localPosition = cardHome;
            card.localRotation = Quaternion.identity;
            phase = Random.value * Mathf.PI * 2f;
            SetArea(area, killY);
            anchor = RandomPoint();
            placed = true;
            PickTarget();
            Apply();
        }

        internal void SetArea(Rect area, float killY)
        {
            this.area = area;
            this.killY = killY;
            if (!placed) return;
            anchor = Clamp(anchor);
            target = Clamp(target);
        }

        internal bool Contains(Vector2 worldPoint)
        {
            if (!IsAvailable) return false;
            var offset = (Vector2)transform.InverseTransformPoint(worldPoint) - bodyCenter;
            offset.x /= bodyRadius.x;
            offset.y /= bodyRadius.y;
            return offset.sqrMagnitude <= 1f;
        }

        // Hides the balloon and lets its card drop.
        internal void Pop()
        {
            popped = true;
            balloon.enabled = false;
            cardVelocity = new Vector3(Random.Range(-.8f, .8f), 1.4f, 0f);
            cardSpin = Random.Range(-200f, 200f);
        }

        private void Update()
        {
            if (popped) { DropCard(); return; }
            if (!placed) return;
            float previousX = anchor.x;
            anchor = Vector2.MoveTowards(anchor, target, speed * Time.deltaTime);
            if ((anchor - target).sqrMagnitude < .0001f) PickTarget();
            float velocityX = Time.deltaTime > 0f ? (anchor.x - previousX) / Time.deltaTime : 0f;
            float sway = Mathf.Clamp(-velocityX * 25f, -cardSwayDegrees, cardSwayDegrees) +
                Mathf.Sin(Time.time * 1.3f + phase) * 3f;
            swing = Mathf.Lerp(swing, sway, 1f - Mathf.Exp(-4f * Time.deltaTime));
            Apply();
        }

        private void Apply()
        {
            transform.localPosition = new Vector3(anchor.x, anchor.y + Mathf.Sin(Time.time * 1.6f + phase) * bobHeight,
                transform.localPosition.z);
            card.localRotation = Quaternion.Euler(0f, 0f, swing);
        }

        private void DropCard()
        {
            if (!card.gameObject.activeSelf) return;
            float delta = Time.deltaTime;
            cardVelocity.y -= cardGravity * delta;
            card.position += cardVelocity * delta;
            card.Rotate(0f, 0f, cardSpin * delta);
            if (card.position.y < killY) card.gameObject.SetActive(false);
        }

        private void PickTarget()
        {
            target = RandomPoint();
            speed = Random.Range(Mathf.Min(speedRange.x, speedRange.y), Mathf.Max(speedRange.x, speedRange.y));
        }

        private Vector2 RandomPoint() => new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax));

        private Vector2 Clamp(Vector2 point) =>
            new Vector2(Mathf.Clamp(point.x, area.xMin, area.xMax), Mathf.Clamp(point.y, area.yMin, area.yMax));
    }
}
