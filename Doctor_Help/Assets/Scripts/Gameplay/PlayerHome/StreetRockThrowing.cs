using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    public sealed class StreetRockThrowing : MonoBehaviour, IRockAim
    {
        private const float PlayerCooldown = 1f;
        private const float ApproachDistance = 6f;
        private const float ThrowHeight = 1f;
        private static readonly string[] Outbursts =
        {
            "Murderer!",
            "Killer!",
            "Give my love one back!",
            "Justice!"
        };

        [SerializeField] private PlayerHomeCrowd crowd;
        [SerializeField] private PlayerHomeMovement player;
        [SerializeField] private PlayerHomeCharacterAnimation playerAnimation;
        [SerializeField] private SpriteRenderer doctorHelpSign;
        [SerializeField] private Collider2D floor;
        [SerializeField] private Button throwButton;
        [SerializeField] private Material aimLineMaterial;
        [SerializeField] private Sprite rockSprite;
        [SerializeField] private ParticleSystem impactPrefab;
        [SerializeField] private GameObject madEyesPrefab;
        [SerializeField, Min(0.01f)] private float rockDiameter = 0.55f;
        [SerializeField] private Vector2 npcBodySize = new Vector2(2.2f, 7.5f);

        internal enum Target { None, Npcs, Player, Sign }

        private sealed class NpcAttack
        {
            internal float nextThrow;
            internal Target target;
        }

        private readonly Dictionary<PlayerHomeWalker, NpcAttack> attacks = new();
        private readonly List<StreetRockProjectile> rocks = new();
        private float nextPlayerThrow;
        private bool configured;
        private bool aiming, aimFacesRight, playerWindup;
        private StreetRockAimInput aimInput;
        private RockAimPreview aimPreview;

        private bool ThrowUnlocked => PlayerProfileService.Instance?.ThrowRock == true;
        private bool CanPlayerThrow => ThrowUnlocked && Time.timeScale > 0f && player.isActiveAndEnabled && !player.IsRiding &&
            Time.time >= nextPlayerThrow && playerAnimation.CanThrowRock;
        private float Ground => floor.bounds.max.y + rockDiameter * 0.5f;
        private Vector2 PlayerThrowOrigin => (Vector2)player.HitBounds.center + Vector2.up * ThrowHeight;

        private void Awake()
        {
            aimInput = throwButton != null ? throwButton.GetComponent<StreetRockAimInput>() : null;
            configured = crowd != null && player != null && playerAnimation != null && doctorHelpSign != null &&
                floor != null && aimInput != null && rockSprite != null && impactPrefab != null &&
                aimLineMaterial != null && madEyesPrefab != null;
            if (!configured)
            {
                Debug.LogError("Street rock throwing requires its crowd, player, colliders, button and rock assets.", this);
                enabled = false;
                return;
            }
            aimPreview = new RockAimPreview(transform, aimLineMaterial);
        }

        private void OnEnable()
        {
            if (!configured) return;
            aimInput.Throwing = this;
            RefreshThrowButton();
        }

        private void Update() => RefreshThrowButton();

        private void RefreshThrowButton()
        {
            if (aiming && !CanPlayerThrow) CancelAim();
            if (throwButton.gameObject.activeSelf != ThrowUnlocked) throwButton.gameObject.SetActive(ThrowUnlocked);
            throwButton.interactable = CanPlayerThrow;
        }

        public bool TryBeginAim()
        {
            if (!isActiveAndEnabled || aiming || !CanPlayerThrow) return false;
            aiming = true;
            aimFacesRight = playerAnimation.FacesRight;
            player.SetRockAimLocked(true);
            UpdateAim(Vector2.zero);
            return true;
        }

        public void UpdateAim(Vector2 drag)
        {
            if (!aiming) return;
            var trajectory = PlayerTrajectory(drag);
            playerAnimation.SetFacing(FacesRightForAim(trajectory));
            aimPreview.Show(trajectory, player.transform.position.z);
        }

        public void ReleaseAim(Vector2 drag)
        {
            if (!aiming) return;
            var trajectory = PlayerTrajectory(drag);
            bool right = FacesRightForAim(trajectory);
            playerWindup = CanPlayerThrow && playerAnimation.TryThrowRock(right, () =>
            {
                playerWindup = false;
                player.SetRockAimLocked(false);
                Launch(trajectory, Target.Npcs);
            });
            CancelAim();
            if (!playerWindup) return;
            nextPlayerThrow = Time.time + PlayerCooldown;
            throwButton.interactable = false;
        }

        private StreetRockTrajectory PlayerTrajectory(Vector2 drag) =>
            StreetRockTrajectory.FromDrag(PlayerThrowOrigin, Ground, aimFacesRight, player.ScreenToRockAim(drag));

        private bool FacesRightForAim(StreetRockTrajectory trajectory) =>
            trajectory.Velocity.x == 0f ? aimFacesRight : trajectory.Velocity.x > 0f;

        public void CancelAim()
        {
            if (!aiming) return;
            aiming = false;
            if (aimInput != null) aimInput.ForgetPointer();
            aimPreview.Hide();
            if (!playerWindup && player != null) player.SetRockAimLocked(false);
        }

        private void OnApplicationFocus(bool focused) { if (!focused) CancelAim(); }
        private void OnApplicationPause(bool paused) { if (paused) CancelAim(); }

        internal void ConfigureNpc(PlayerHomeCharacterAnimation animation) =>
            animation.ConfigureExpressionEyes(madEyesPrefab);

        internal bool TickNpc(PlayerHomeWalker walker)
        {
            if (!isActiveAndEnabled) return false;
            var profile = PlayerProfileService.Instance;
            if (profile == null || !profile.IsStreetRockThrowingActive)
            {
                if (attacks.Remove(walker))
                {
                    walker.Animation.CancelRockThrow();
                    walker.ResumeWandering();
                }
                return false;
            }

            if (walker.Animation.IsThrowingRock) return true;
            if (!attacks.TryGetValue(walker, out var attack))
            {
                attacks.Add(walker, new NpcAttack { nextThrow = Time.time + Random.Range(6f, 14f) });
                return false;
            }
            if (Time.time < attack.nextThrow || !walker.Animation.CanThrowRock) return false;

            bool playerOutside = player.isActiveAndEnabled;
            if (attack.target == Target.None)
                attack.target = playerOutside && Random.value < 0.5f ? Target.Player : Target.Sign;
            else if (attack.target == Target.Player && !playerOutside)
                attack.target = Target.Sign;

            Target target = attack.target;
            float targetX = TargetBounds(target).center.x;
            float delta = targetX - walker.Transform.position.x;
            bool right = delta >= 0f;
            if (Mathf.Abs(delta) > ApproachDistance)
            {
                walker.FollowRockTarget(targetX - Mathf.Sign(delta) * (ApproachDistance - 0.5f));
                return true;
            }

            if (!walker.Animation.TryThrowRock(right, () =>
            {
                Vector2 origin = walker.Transform.position;
                origin.y += ThrowHeight;
                var bounds = TargetBounds(target);
                Vector2 aim = bounds.center;
                if (target == Target.Player) aim.y = Mathf.Clamp(origin.y, bounds.min.y, bounds.max.y);
                Launch(StreetRockTrajectory.ToTarget(origin, Ground, right, aim), target);
            })) return false;

            walker.Animation.ShowExpressionEyes(2f);
            if (Random.value < 0.3f && walker.Transform.TryGetComponent<NpcDialogue>(out var dialogue))
                dialogue.SpeakStreetOutburst(Outbursts[Random.Range(0, Outbursts.Length)]);
            attack.nextThrow = Time.time + Random.Range(6f, 14f);
            attack.target = Target.None;
            walker.ResumeWandering();
            return true;
        }

        private Bounds TargetBounds(Target target) => target == Target.Player ? player.HitBounds : doctorHelpSign.bounds;

        private void Launch(StreetRockTrajectory trajectory, Target target)
        {
            if (!isActiveAndEnabled) return;
            var rock = new GameObject("Thrown Rock", typeof(SpriteRenderer));
            rock.transform.SetParent(transform, false);
            var renderer = rock.GetComponent<SpriteRenderer>();
            renderer.sprite = rockSprite;
            renderer.sortingOrder = 15;
            var size = rockSprite.bounds.size;
            rock.transform.localScale = Vector3.one * (rockDiameter / Mathf.Max(size.x, size.y));

            rocks.Add(new StreetRockProjectile(rock.transform, trajectory, player.transform.position.z, target));
        }

        private void FixedUpdate()
        {
            for (int i = rocks.Count - 1; i >= 0; i--)
            {
                var rock = rocks[i];
                if (rock.Step(Time.fixedDeltaTime, this)) continue;
                Destroy(rock.Visual.gameObject);
                rocks.RemoveAt(i);
            }
        }

        internal bool TryImpact(Target target, Vector2 from, Vector2 to, float depth, out Vector2 hit)
        {
            hit = to;
            float nearest = (to - from).magnitude;
            var ray = new Ray(from, (to - from).normalized);
            PlayerHomeCharacterAnimation hitCharacter = null;
            if (target == Target.Npcs)
            {
                foreach (var walker in crowd.StreetWalkers)
                {
                    if (walker.Transform == null || !walker.Transform.gameObject.activeInHierarchy) continue;
                    Vector3 center = walker.Transform.position;
                    var bounds = new Bounds(center, npcBodySize);
                    if (!FindHit(bounds, ray, ref nearest)) continue;
                    hitCharacter = walker.Animation;
                }
                if (hitCharacter == null) return false;
            }
            else
            {
                if (target == Target.Player && !player.isActiveAndEnabled) return false;
                if (!FindHit(TargetBounds(target), ray, ref nearest)) return false;
                if (target == Target.Player) hitCharacter = playerAnimation;
            }

            hit = ray.GetPoint(nearest);
            if (hitCharacter != null) hitCharacter.FlashRockHit();
            if (target == Target.Player) playerAnimation.ShowExpressionEyes(1f);
            if (target == Target.Sign) depth = doctorHelpSign.bounds.center.z;
            var impact = Instantiate(impactPrefab, new Vector3(hit.x, hit.y, depth),
                impactPrefab.transform.rotation, transform);
            var settings = impact.main;
            settings.stopAction = ParticleSystemStopAction.Destroy;
            impact.Play();
            return true;
        }

        private bool FindHit(Bounds bounds, Ray ray, ref float nearest)
        {
            // All Street actors walk along one 2D plane, with different Z values for rendering.
            var center = bounds.center;
            center.z = 0f;
            bounds.center = center;
            var size = bounds.size;
            size.z = 1f;
            bounds.size = size;
            bounds.Expand(rockDiameter);
            float distance = 0f;
            if (!bounds.Contains(ray.origin) && !bounds.IntersectRay(ray, out distance)) return false;
            if (distance > nearest) return false;
            nearest = distance;
            return true;
        }

        private void OnDisable()
        {
            if (!configured) return;
            CancelAim();
            if (aimInput != null) aimInput.Throwing = null;
            playerWindup = false;
            if (player != null) player.SetRockAimLocked(false);
            if (throwButton != null)
                throwButton.interactable = false;
            if (playerAnimation != null) playerAnimation.CancelRockThrow();
            foreach (var walker in attacks.Keys)
                if (walker.Animation != null)
                    walker.Animation.CancelRockThrow();
            attacks.Clear();
            foreach (var rock in rocks)
                if (rock.Visual != null) Destroy(rock.Visual.gameObject);
            rocks.Clear();
        }
    }
}
