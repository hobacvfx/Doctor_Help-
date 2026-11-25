using System.Collections;
using UnityEngine;

namespace DoctorHelp
{
    // Player Home shower. Walking into the zone pays clean water, closes the curtain, pours water for a while,
    // removes dirt and opens the curtain again. A clean player (0 dirt) cannot shower. The player cannot act until the curtain is open.
    [DisallowMultipleComponent]
    public sealed class PlayerShower : MonoBehaviour
    {
        [SerializeField] private PlayerHomeMovement player;
        [Header("Curtain")]
        [SerializeField] private Animator curtain;
        [Tooltip("Curtain flipbook state. Its first frame is fully closed, its last frame fully open.")]
        [SerializeField] private string curtainState = "ShowerCurtain_8Frames_4x2_Loop";
        [Header("Zone (local to this object)")]
        [SerializeField] private Vector2 zoneCenter;
        [SerializeField] private Vector2 zoneSize = Vector2.one;
        [Header("Water")]
        [SerializeField] private ParticleSystem waterPourPrefab;
        [SerializeField] private Transform waterPourPoint;
        [SerializeField, Min(0f)] private float pourSeconds = 2f;
        [SerializeField, Min(0f)] private float pourFadeSeconds = 1f;
        [SerializeField, Min(0)] private int waterCost = 50;
        [SerializeField, Min(0f)] private float dirtRemoved = 3f;
        [Header("Not enough water")]
        [SerializeField] private GameObject warning;
        [SerializeField, Min(0f)] private float warningSeconds = 10f;

        private PlayerProfileService profile;
        private PlayerDirtiness dirtiness;
        private int curtainHash;
        private float curtainSeconds = .5f, warningUntil;
        private bool inside, busy, locked;

        private void Start()
        {
            profile = PlayerProfileService.Instance;
            if (player == null) player = FindFirstObjectByType<PlayerHomeMovement>();
            if (player != null) dirtiness = player.GetComponent<PlayerDirtiness>();
            curtainHash = Animator.StringToHash(curtainState);
            if (curtain != null && curtain.runtimeAnimatorController != null)
            {
                foreach (var clip in curtain.runtimeAnimatorController.animationClips)
                    if (clip != null) { curtainSeconds = clip.length; break; }
                curtain.speed = 0f; // The script drives the flipbook both ways.
            }
            SetCurtain(1f);
            if (warning != null) warning.SetActive(false);
            inside = PlayerInside(); // Spawning in the shower does not start one.
        }

        private void Update()
        {
            if (warning != null && warning.activeSelf && Time.time >= warningUntil) warning.SetActive(false);
            if (busy || player == null) return;
            bool now = PlayerInside();
            if (now && !inside) TryShower();
            inside = now;
        }

        private Rect Zone
        {
            get
            {
                var center = transform.TransformPoint(zoneCenter);
                var scale = transform.lossyScale;
                var size = new Vector2(Mathf.Abs(zoneSize.x * scale.x), Mathf.Abs(zoneSize.y * scale.y));
                return new Rect((Vector2)center - size * .5f, size);
            }
        }

        private bool PlayerInside()
        {
            if (player == null || !player.isActiveAndEnabled || player.IsRiding) return false;
            var zone = Zone;
            var body = player.HitBounds;
            return zone.xMin <= player.Position.x && player.Position.x <= zone.xMax &&
                body.max.y > zone.yMin && body.min.y < zone.yMax;
        }

        private void TryShower()
        {
            if (profile == null || !profile.IsLoaded) return;
            if (profile.Dirt <= 0f) return; // Already clean: nothing to wash off, so the shower stays off.
            int cost = waterCost;
            if (profile.Water < cost)
            {
                ShowWarning();
                return;
            }
            StartCoroutine(Shower(cost));
        }

        private void ShowWarning()
        {
            if (warning == null) return;
            warningUntil = Time.time + warningSeconds;
            warning.SetActive(true);
        }

        private IEnumerator Shower(int cost)
        {
            busy = true;
            if (dirtiness != null) dirtiness.HoldVisuals(true); // Keep the old smell until the water is off.
            if (!profile.TryShower(cost, dirtRemoved))
            {
                if (dirtiness != null) dirtiness.HoldVisuals(false);
                busy = false;
                ShowWarning();
                yield break;
            }
            if (warning != null) warning.SetActive(false);
            Lock(true);

            // Step under the shower head so the curtain hides the player.
            float centerX = Zone.center.x;
            player.BeginAutomaticWalk();
            player.SetAutomaticDestination(centerX);
            float giveUp = Time.time + 3f;
            while (Mathf.Abs(player.Position.x - centerX) > .05f && Time.time < giveUp) yield return null;
            player.EndAutomaticWalk();
            player.SetActionLocked(true);

            yield return AnimateCurtain(1f, 0f);
            ParticleSystem pour = null;
            if (waterPourPrefab != null)
            {
                var at = waterPourPoint != null ? waterPourPoint.position : transform.position;
                pour = Instantiate(waterPourPrefab, at, waterPourPrefab.transform.rotation);
                pour.Play(true);
            }
            yield return new WaitForSeconds(pourSeconds);
            if (pour != null) pour.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            yield return new WaitForSeconds(pourFadeSeconds);
            if (pour != null) Destroy(pour.gameObject);

            if (dirtiness != null) dirtiness.HoldVisuals(false);
            yield return AnimateCurtain(0f, 1f);
            Lock(false);
            busy = false;
            inside = PlayerInside(); // Leave the zone and come back for another shower.
        }

        private IEnumerator AnimateCurtain(float from, float to)
        {
            for (float time = 0f; time < curtainSeconds; time += Time.deltaTime)
            {
                SetCurtain(Mathf.Lerp(from, to, time / curtainSeconds));
                yield return null;
            }
            SetCurtain(to);
        }

        // 0 = closed (first frame), 1 = open (last frame).
        private void SetCurtain(float progress)
        {
            if (curtain == null || curtain.runtimeAnimatorController == null) return;
            curtain.Play(curtainHash, 0, Mathf.Clamp(progress, 0f, .999f));
        }

        private void Lock(bool value)
        {
            if (locked == value) return;
            locked = value;
            DeskActionButton.WorldBlockers += value ? 1 : -1;
            if (!value && player != null) player.SetActionLocked(false);
        }

        private void OnDisable()
        {
            if (!busy) return;
            busy = false;
            if (player != null) player.EndAutomaticWalk();
            Lock(false);
            if (dirtiness != null) dirtiness.HoldVisuals(false);
        }
    }
}
