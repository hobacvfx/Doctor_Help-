using UnityEngine;

namespace DoctorHelp
{
    // Shows the hidden dirt stat on the player wherever the avatar walks:
    // 4+ smells (VFX 1), 7+ smells worse (VFX 2) and walks 40% slower, 10 walks 85% slower.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerHomeMovement))]
    public sealed class PlayerDirtiness : MonoBehaviour
    {
        internal const float SmellyDirt = 4f;
        internal const float SlowDirt = 7f;

        [SerializeField] private ParticleSystem smellyEffect;
        [SerializeField] private ParticleSystem verySmellyEffect;
        [Tooltip("Effect position relative to the centre of the player's body.")]
        [SerializeField] private Vector3 effectOffset;
        [SerializeField, Range(0f, 1f)] private float slowPenalty = .4f;
        [SerializeField, Range(0f, 1f)] private float filthyPenalty = .85f;

        private PlayerHomeMovement movement;
        private PlayerHomeCharacterAnimation characterAnimation;
        private Collider2D body;
        private PlayerProfileService profile;
        private ParticleSystem effectPrefab, effect;
        private bool held;

        private void Awake()
        {
            movement = GetComponent<PlayerHomeMovement>();
            characterAnimation = GetComponent<PlayerHomeCharacterAnimation>();
            body = GetComponent<Collider2D>();
        }

        private void OnEnable()
        {
            profile = PlayerProfileService.Instance;
            if (profile != null) profile.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (profile != null) profile.Changed -= Refresh;
        }

        // Keeps the current look until a scripted moment (the shower) is ready to reveal the new dirt level.
        internal void HoldVisuals(bool hold)
        {
            held = hold;
            if (!hold) Refresh();
        }

        private void Refresh()
        {
            if (held) return;
            float dirt = profile != null && profile.IsLoaded ? profile.Dirt : 0f;
            float speed = dirt >= PlayerProfileData.MaxDirt ? 1f - filthyPenalty : dirt >= SlowDirt ? 1f - slowPenalty : 1f;
            movement.SpeedMultiplier = speed;
            if (characterAnimation != null) characterAnimation.RunSpeedScale = speed;

            var wanted = dirt >= SlowDirt ? verySmellyEffect : dirt >= SmellyDirt ? smellyEffect : null;
            if (wanted == effectPrefab && (wanted == null || effect != null)) return;
            if (effect != null) Destroy(effect.gameObject);
            effect = null;
            effectPrefab = wanted;
            if (wanted == null) return;
            effect = Instantiate(wanted, transform);
            effect.transform.position = (body != null ? body.bounds.center : transform.position) + effectOffset;
            effect.Play(true);
        }
    }
}
