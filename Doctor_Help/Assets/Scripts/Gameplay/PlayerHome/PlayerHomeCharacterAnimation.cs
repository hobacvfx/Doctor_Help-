using System;
using UnityEngine;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    public sealed class PlayerHomeCharacterAnimation : MonoBehaviour
    {
        private static readonly int Idle = Animator.StringToHash("Base Layer.NPC_Idle");
        private static readonly int Run = Animator.StringToHash("Base Layer.A_Run");
        private static readonly int ThrowRock = Animator.StringToHash("Base Layer.A_ThrowRock");
        // The forward pose is at frame 26 of the 47-frame throw.
        private const float ReleaseTime = 26f / 47f;
        private const float FlashInterval = 0.08f;
        private const float FlashDuration = 4f * FlashInterval;

        [SerializeField] private RuntimeAnimatorController locomotion;
        [SerializeField] private Shader examinationShader;
        [SerializeField] private Material walkingSpriteMaterial;
        [SerializeField] private bool facesRightAtRest;
        [SerializeField] private Transform eyeSlot;
        [SerializeField] private GameObject expressionEyesPrefab;

        private Animator animator;
        private Vector3 visualOrigin;
        private float originalScaleSign;
        private float previousX;
        private bool moving;
        private bool facingRight;
        private bool throwingRock;
        private Action releaseRock;
        private SpriteRenderer[] sprites;
        private Color[] originalColors;
        private float flashEnds;
        private bool flashVisible;
        private GameObject expressionEyes;
        private GameObject[] normalEyes;
        private bool[] eyeWasActive;
        private float expressionEyesUntil;

        internal bool FacesRight => facingRight;
        internal bool IsThrowingRock => throwingRock;
        internal bool CanThrowRock => isActiveAndEnabled && animator != null && !throwingRock &&
            animator.HasState(0, ThrowRock);
        internal void SetFacing(bool right) => facingRight = right;

        internal void Configure(RuntimeAnimatorController controller, Shader shader, Material spriteMaterial)
        {
            locomotion = controller;
            examinationShader = shader;
            walkingSpriteMaterial = spriteMaterial;
        }

        internal void ConfigureExpressionEyes(GameObject prefab)
        {
            var appearance = GetComponent<NpcAppearanceProfile>();
            if (appearance?.Slots == null) return;
            foreach (var slot in appearance.Slots)
            {
                if (slot == null || !string.Equals(slot.id, "Eye", StringComparison.OrdinalIgnoreCase)) continue;
                eyeSlot = slot.attachPoint;
                expressionEyesPrefab = prefab;
                return;
            }
        }

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null || locomotion == null)
            {
                Debug.LogError("Player Home character requires a visual animator and locomotion controller.", this);
                enabled = false;
                return;
            }

            // The examination material renders only one face. Use the sprite material in this scene.
            PrepareExpressionEyes();
            sprites = animator.GetComponentsInChildren<SpriteRenderer>(true);
            if (examinationShader != null && walkingSpriteMaterial != null)
                foreach (var sprite in sprites)
                    if (sprite.sharedMaterial != null && sprite.sharedMaterial.shader == examinationShader)
                        sprite.sharedMaterial = walkingSpriteMaterial;

            visualOrigin = animator.transform.localPosition;
            originalScaleSign = Mathf.Sign(animator.transform.localScale.x);
            previousX = transform.position.x;
            facingRight = facesRightAtRest;
            animator.runtimeAnimatorController = locomotion;
            animator.applyRootMotion = false;
            animator.enabled = true;
        }

        internal bool TryThrowRock(bool toRight, Action release)
        {
            if (!CanThrowRock) return false;
            facingRight = toRight;
            throwingRock = true;
            releaseRock = release;
            animator.Play(ThrowRock, 0, 0f);
            return true;
        }

        internal void CancelRockThrow()
        {
            if (!throwingRock) return;
            throwingRock = false;
            releaseRock = null;
            moving = false;
            if (animator != null) animator.Play(Idle, 0, 0f);
        }

        internal void FlashRockHit()
        {
            if (sprites == null) return;
            if (originalColors == null)
            {
                originalColors = new Color[sprites.Length];
                for (int i = 0; i < sprites.Length; i++)
                    if (sprites[i] != null) originalColors[i] = sprites[i].color;
            }
            flashEnds = Time.time + FlashDuration;
            SetHitTint(true);
        }

        private void LateUpdate()
        {
            float currentX = transform.position.x;
            float distance = currentX - previousX;
            previousX = currentX;
            bool running = Mathf.Abs(distance) > 0.08f * Time.deltaTime;
            bool wasThrowing = throwingRock;
            if (wasThrowing)
                AdvanceRockThrow();
            if (running && !wasThrowing)
                facingRight = distance > 0f;
            if (!throwingRock && (wasThrowing || running != moving))
            {
                moving = running;
                animator.Play(running ? Run : Idle, 0, 0f);
            }

            // Keep the authored X/Z alignment. The two clips use different local X offsets.
            var art = animator.transform;
            var position = art.localPosition;
            art.localPosition = new Vector3(visualOrigin.x, position.y, visualOrigin.z);
            var scale = art.localScale;
            scale.x = Mathf.Abs(scale.x) * originalScaleSign * (facingRight == facesRightAtRest ? 1f : -1f);
            art.localScale = scale;
            UpdateHitTint();
            if (expressionEyes != null && expressionEyes.activeSelf && Time.time >= expressionEyesUntil)
                RestoreEyes();
        }

        private void PrepareExpressionEyes()
        {
            if (eyeSlot == null || expressionEyesPrefab == null) return;
            normalEyes = new GameObject[eyeSlot.childCount];
            eyeWasActive = new bool[normalEyes.Length];
            for (int i = 0; i < normalEyes.Length; i++)
                normalEyes[i] = eyeSlot.GetChild(i).gameObject;
            expressionEyes = Instantiate(expressionEyesPrefab, eyeSlot, false);
            expressionEyes.SetActive(false);
        }

        internal void ShowExpressionEyes(float duration)
        {
            if (expressionEyes == null) return;
            expressionEyesUntil = Time.time + duration;
            if (expressionEyes.activeSelf) return;
            for (int i = 0; i < normalEyes.Length; i++)
            {
                if (normalEyes[i] == null) continue;
                eyeWasActive[i] = normalEyes[i].activeSelf;
                normalEyes[i].SetActive(false);
            }
            expressionEyes.SetActive(true);
        }

        private void RestoreEyes()
        {
            if (expressionEyes == null || !expressionEyes.activeSelf) return;
            expressionEyes.SetActive(false);
            for (int i = 0; i < normalEyes.Length; i++)
                if (normalEyes[i] != null) normalEyes[i].SetActive(eyeWasActive[i]);
        }

        private void AdvanceRockThrow()
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash != ThrowRock) return;
            if (state.normalizedTime >= ReleaseTime && releaseRock != null)
            {
                var release = releaseRock;
                releaseRock = null;
                release();
            }
            if (state.normalizedTime >= 1f)
                throwingRock = false;
        }

        private void UpdateHitTint()
        {
            if (originalColors == null) return;
            float remaining = flashEnds - Time.time;
            SetHitTint(remaining > 0f && Mathf.CeilToInt(remaining / FlashInterval) % 2 == 0);
            if (remaining <= 0f) originalColors = null;
        }

        private void SetHitTint(bool visible)
        {
            if (flashVisible == visible) return;
            flashVisible = visible;
            var red = new Color(1f, 0.23f, 0.23f, 1f);
            for (int i = 0; i < sprites.Length; i++)
                if (sprites[i] != null) sprites[i].color = visible ? originalColors[i] * red : originalColors[i];
        }

        private void OnDisable()
        {
            CancelRockThrow();
            SetHitTint(false);
            originalColors = null;
            RestoreEyes();
        }
    }
}
