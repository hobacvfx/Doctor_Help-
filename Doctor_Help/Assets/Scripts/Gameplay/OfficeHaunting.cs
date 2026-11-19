using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DoctorHelp
{
    [DisallowMultipleComponent]
    public sealed class OfficeHaunting : MonoBehaviour, IRockAim
    {
        [SerializeField] private Camera examinationCamera;
        [SerializeField] private PatientSpawnPoint spawnPoint;
        [SerializeField] private StreetRockAimInput aimInput;
        [SerializeField] private Transform launchPoint;
        [SerializeField] private CapsuleCollider2D ghostPrefab;
        [SerializeField] private SpriteRenderer rockPrefab;
        [SerializeField] private GameObject aimMarkerPrefab;
        [SerializeField] private PatientRockReaction reactionPrefab;
        [SerializeField] private ParticleSystem impactPrefab;
        [SerializeField, Min(.05f)] private float flightSeconds = .32f;
        [SerializeField, Range(.1f, 1f)] private float distanceScale = .35f;
        [SerializeField, Min(0)] private float arcHeight = .45f;
        [SerializeField, Min(.1f)] private float cooldownSeconds = 1f;

        private sealed class Orbit
        {
            public CapsuleCollider2D Ghost;
            public float Phase, RadiusX, RadiusY, Tilt, Height, Direction;
        }

        private readonly List<Orbit> ghosts = new List<Orbit>(3);
        private PatientExaminationProfile patient;
        private SpriteRenderer head;
        private SpriteRenderer[] outfit = Array.Empty<SpriteRenderer>();
        private DailyGameLoop loop;
        private FirstRunTutorial tutorial;
        private SpriteRenderer rock;
        private GameObject marker;
        private RockAimPreview aimPreview;
        private Vector3 aimOrigin, aimPoint, flightStart, flightEnd, rockScale;
        private float flightTime, orbitTime, readyAt, orbitSpeed;
        private bool available, aiming, tutorialOffered;

        private bool CanAim => available && patient != null && patient.HasArrived && ghosts.Count > 0 &&
            loop != null && loop.Step == DayStep.Examination && !loop.IsTransitioning;
        internal event Action<PatientExaminationProfile> PatientStruck;
        internal event Action RockReleased;
        internal StreetRockAimInput AimInput => aimInput;
        internal bool IsAiming => aiming;
        internal bool CanConfirmWithoutMedicine => spawnPoint?.CurrentPatient?.CanConfirmWithoutMedicine == true;
        internal bool CanTeachThrow => CanAim && !loop.IsEndingDayEarly && !patient.Definition.RequiresPoliceReport;

        private void Awake()
        {
            aimInput.Throwing = this;
            SetAvailable(false);
        }

        internal void SetAvailable(bool value)
        {
            available = value;
            if (!available)
            {
                CancelAim();
                DiscardRock();
            }
            UpdateButton();
        }

        private void LateUpdate()
        {
            var current = spawnPoint.CurrentPatient;
            if (current != null && !current.HasArrived) current = null;
            if (current != patient) BindPatient(current);
            if (patient != null && (spawnPoint.IsDeparting || patient.RockReaction != null))
                ClearGhosts();

            if (ghosts.Count > 0) MoveGhosts();
            if (rock != null) AdvanceRock();
            UpdateButton();
            if (!tutorialOffered && tutorial != null && CanTeachThrow)
                tutorialOffered = tutorial.OfferHauntedTutorial(this);
        }

        private void BindPatient(PatientExaminationProfile current)
        {
            ClearGhosts();
            CancelAim();
            DiscardRock();
            patient = current;
            head = null;
            outfit = Array.Empty<SpriteRenderer>();
            orbitTime = 0;
            tutorialOffered = false;
            if (patient == null || patient.Haunting == null || patient.HauntingCleared) return;
            if (loop == null) loop = FindFirstObjectByType<DailyGameLoop>();
            if (tutorial == null && loop != null) tutorial = loop.GetComponent<FirstRunTutorial>();

            // The examination rigs share the Head renderer and Outfit slot.
            foreach (var renderer in patient.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.name == "Head") { head = renderer; break; }
            var appearance = patient.GetComponent<NpcAppearanceProfile>();
            if (appearance != null)
                foreach (var slot in appearance.Slots)
                    if (slot != null && slot.id == "Outfit" && slot.attachPoint != null)
                    {
                        outfit = slot.attachPoint.GetComponentsInChildren<SpriteRenderer>(true);
                        break;
                    }

            int count = Mathf.Min(patient.Definition.HauntingDifficulty, 3);
            orbitSpeed = patient.Haunting.GhostSpeed(patient.Definition.HauntingDifficulty) * Mathf.PI * 2f;
            float startPhase = Random.Range(0f, Mathf.PI * 2f);
            for (int i = 0; i < count; i++)
            {
                var ghost = Instantiate(ghostPrefab, transform);
                ghosts.Add(new Orbit
                {
                    Ghost = ghost,
                    Phase = startPhase + Mathf.PI * 2f * i / count,
                    RadiusX = Random.Range(.48f, .74f),
                    RadiusY = Random.Range(.75f, 1f),
                    Height = Random.Range(.3f, .68f),
                    Tilt = Random.Range(-.28f, .28f),
                    Direction = Random.value < .5f ? -1f : 1f
                });
            }
        }

        private Vector3 Face => head != null ? head.bounds.center : patient.Heart.position + Vector3.up;

        private void MoveGhosts()
        {
            orbitTime += Time.deltaTime;
            var face = Face;
            var chest = patient.Heart.position;
            float width = head != null ? head.bounds.size.x : 1f;
            float height = Mathf.Max(width * .6f, Mathf.Abs(face.y - chest.y));
            float viewportWidth = 2f * examinationCamera.orthographicSize * examinationCamera.aspect;
            foreach (var orbit in ghosts)
            {
                float phase = orbit.Phase + orbitTime * orbitSpeed * orbit.Direction;
                var center = Vector3.Lerp(chest, face, orbit.Height);
                // Keep the ghost hittable in portrait view even when a patient's head is off-center.
                var viewport = examinationCamera.WorldToViewportPoint(center);
                float edgeDistance = Mathf.Min(viewport.x - .07f, .93f - viewport.x);
                float radius = Mathf.Min(width * orbit.RadiusX, Mathf.Max(.15f, edgeDistance * viewportWidth));
                float x = Mathf.Cos(phase) * radius;
                float y = Mathf.Sin(phase) * height * orbit.RadiusY + x * orbit.Tilt;
                var position = new Vector3(center.x + x, center.y + y, face.z - .1f);
                viewport = examinationCamera.WorldToViewportPoint(position);
                viewport.x = Mathf.Clamp(viewport.x, .07f, .93f);
                viewport.y = Mathf.Clamp(viewport.y, .05f, .95f);
                orbit.Ghost.transform.position = examinationCamera.ViewportToWorldPoint(viewport);
                orbit.Ghost.transform.rotation = Quaternion.Euler(0, 0, 12f * Mathf.Sin(phase));
            }
        }

        private void UpdateButton()
        {
            bool show = available && ghosts.Count > 0;
            if (aimInput.gameObject.activeSelf != show) aimInput.gameObject.SetActive(show);
        }

        public bool TryBeginAim()
        {
            if (!CanAim || aiming || rock != null || Time.time < readyAt) return false;
            aiming = true;
            aimOrigin = examinationCamera.WorldToScreenPoint(Vector3.Lerp(patient.Heart.position, Face, .55f));
            if (marker == null)
            {
                marker = Instantiate(aimMarkerPrefab, transform);
                var circle = marker.GetComponent<LineRenderer>();
                aimPreview = new RockAimPreview(transform, circle.sharedMaterial,
                    circle.sortingLayerID, circle.sortingOrder - 1, circle.startWidth, circle.startColor);
            }
            marker.SetActive(true);
            UpdateAim(Vector2.zero);
            return true;
        }

        public void UpdateAim(Vector2 drag)
        {
            if (!aiming) return;
            var viewport = examinationCamera.pixelRect;
            var screen = aimOrigin + (Vector3)drag;
            screen.x = Mathf.Clamp(screen.x, viewport.xMin, viewport.xMax);
            screen.y = Mathf.Clamp(screen.y, viewport.yMin, viewport.yMax);
            aimPoint = examinationCamera.ScreenToWorldPoint(screen);
            marker.transform.position = aimPoint;
            aimPreview.ShowOffice(launchPoint.position, aimPoint, arcHeight);
        }

        public void ReleaseAim(Vector2 drag)
        {
            if (!aiming) return;
            if (!CanAim) { CancelAim(); return; }
            UpdateAim(drag);
            CancelAim();
            rock = Instantiate(rockPrefab, launchPoint.position, launchPoint.rotation, transform);
            flightStart = launchPoint.position;
            flightEnd = aimPoint;
            rockScale = rock.transform.localScale;
            flightTime = 0;
            readyAt = Time.time + cooldownSeconds;
            RockReleased?.Invoke();
        }

        public void CancelAim()
        {
            aiming = false;
            if (marker != null) marker.SetActive(false);
            aimPreview?.Hide();
            if (aimInput != null) aimInput.ForgetPointer();
        }

        private void AdvanceRock()
        {
            flightTime += Time.deltaTime;
            float progress = Mathf.Clamp01(flightTime / flightSeconds);
            rock.transform.position = RockAimPreview.OfficePosition(flightStart, flightEnd, arcHeight, progress);
            rock.transform.localScale = rockScale * Mathf.Lerp(1f, distanceScale, progress);
            rock.transform.Rotate(0, 0, 500f * Time.deltaTime);
            if (progress < 1f) return;
            DiscardRock();
            if (patient == null) return;
            Physics2D.SyncTransforms();
            for (int i = 0; i < ghosts.Count; i++)
            {
                if (!ghosts[i].Ghost.OverlapPoint(flightEnd)) continue;
                PlayImpact();
                Destroy(ghosts[i].Ghost.gameObject);
                ghosts.RemoveAt(i);
                patient.HauntingCleared = ghosts.Count == 0;
                return;
            }
            if (!Contains(head, flightEnd) && !Array.Exists(outfit, sprite => Contains(sprite, flightEnd))) return;

            PlayImpact();
            patient.RockReaction = Instantiate(reactionPrefab, patient.transform);
            PatientStruck?.Invoke(patient);
        }

        private static bool Contains(SpriteRenderer sprite, Vector2 point)
        {
            if (sprite == null || !sprite.enabled || !sprite.gameObject.activeInHierarchy || sprite.sprite == null) return false;
            var bounds = sprite.bounds;
            if (bounds.extents.x <= 0 || bounds.extents.y <= 0) return false;
            var normalized = (point - (Vector2)bounds.center) / (Vector2)bounds.extents;
            return normalized.sqrMagnitude <= 1f;
        }

        private void PlayImpact()
        {
            var impact = Instantiate(impactPrefab, flightEnd, impactPrefab.transform.rotation, transform);
            var settings = impact.main;
            settings.stopAction = ParticleSystemStopAction.Destroy;
            impact.Play();
        }

        private void DiscardRock()
        {
            if (rock == null) return;
            Destroy(rock.gameObject);
            rock = null;
        }

        private void ClearGhosts()
        {
            foreach (var orbit in ghosts)
                if (orbit.Ghost != null) Destroy(orbit.Ghost.gameObject);
            ghosts.Clear();
        }

        private void OnApplicationFocus(bool focused) { if (!focused) CancelAim(); }
        private void OnApplicationPause(bool paused) { if (paused) CancelAim(); }
        private void OnDisable()
        {
            CancelAim();
            DiscardRock();
            ClearGhosts();
            patient = null;
        }
    }
}

