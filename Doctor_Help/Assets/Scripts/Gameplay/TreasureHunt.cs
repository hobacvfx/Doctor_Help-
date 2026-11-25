using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    // Read the actual lens after the X-ray controller has moved it, including the touch offset.
    [DefaultExecutionOrder(50), DisallowMultipleComponent]
    public sealed class TreasureHunt : MonoBehaviour
    {
        private const float MinimumSize = .01f;
        private const float HoldSeconds = 2f;

        [SerializeField] private ExaminationToolSelection selection;
        [SerializeField] private PatientSpawnPoint patients;
        [SerializeField] private TreasureHuntFeedback feedback;
        [SerializeField] private ParticleSystem treasurePrefab, puffPrefab;
        [SerializeField, Range(0, 1)] private float chance = .7f;
        [SerializeField, Min(0), Tooltip("World distance at which the treasure reaches full size.")]
        private float fullSizeDistance = .13f;
        [SerializeField, Min(.001f), Tooltip("World distance at which the treasure shrinks to its minimum size.")]
        private float minimumSizeDistance = .39f;

        private MagnifierLens lens;
        private string consultation;
        private ParticleSystem treasure;
        private ParticleSystem[] emitters;
        private ParticleSystem.MinMaxCurve[] authoredSizes;
        private ParticleSystem.Particle[] particles;
        private bool rolled, showing, paused, unfocused;
        private float size, progressSeconds;

        private void Awake()
        {
            if (selection != null && selection.Magnifier != null)
                lens = selection.Magnifier.GetComponent<MagnifierLens>();
        }

        private void LateUpdate()
        {
            var patient = patients != null ? patients.CurrentPatient : null;
            string currentConsultation = patient != null ? patient.ConsultationId : null;
            if (consultation != currentConsultation)
            {
                ClearTreasure();
                consultation = currentConsultation;
                rolled = false;
            }

            if (paused || unfocused || Time.timeScale <= 0 || patient == null || !patient.HasArrived ||
                patients.IsDeparting || lens == null || !lens.IsConfigured || !lens.isActiveAndEnabled ||
                selection == null || !selection.IsUsingXray)
            {
                HideTreasure();
                return;
            }

            if (!rolled)
            {
                rolled = true;
                if (chance >= 1f || Random.value < chance) Spawn(patient.TreasureBody);
            }
            if (treasure == null) return;

            Vector3 worldCenter = lens.WorldCenter;
            float distance = Vector2.Distance(worldCenter, treasure.transform.position);
            float outerDistance = Mathf.Max(minimumSizeDistance, fullSizeDistance + .001f);
            SetSize(Mathf.Lerp(MinimumSize, 1f, Mathf.InverseLerp(outerDistance, fullSizeDistance, distance)));
            if (!showing)
            {
                treasure.Play(true);
                showing = true;
            }

            if (distance > fullSizeDistance)
            {
                ResetProgress();
                return;
            }

            progressSeconds += Time.deltaTime;
            if (progressSeconds < HoldSeconds)
            {
                feedback.ShowProgress(selection.TargetCamera.WorldToScreenPoint(worldCenter), progressSeconds / HoldSeconds);
                return;
            }

            var profile = PlayerProfileService.Instance;
            if (profile == null || !profile.IsLoaded) return;
            Vector3 position = treasure.transform.position;
            ExaminationPuff.Play(puffPrefab, treasure.gameObject, position);
            ClearTreasure();
            int gold = Random.Range(5, 11);
            profile.SetMoney((int)System.Math.Min((long)profile.Money + gold, int.MaxValue));
            feedback.FlyGold(selection.TargetCamera.WorldToScreenPoint(position), gold);
        }

        private void Spawn(SpriteRenderer body)
        {
            if (treasurePrefab == null || !TryFindPoint(body, selection.TargetCamera, out var localPoint)) return;
            treasure = Instantiate(treasurePrefab, body.transform, false);
            treasure.transform.localPosition = localPoint;
            treasure.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            emitters = treasure.GetComponentsInChildren<ParticleSystem>();
            authoredSizes = new ParticleSystem.MinMaxCurve[emitters.Length];
            int capacity = 0;
            for (int i = 0; i < emitters.Length; i++)
            {
                var main = emitters[i].main;
                authoredSizes[i] = main.startSize;
                capacity = Mathf.Max(capacity, main.maxParticles);
            }
            particles = new ParticleSystem.Particle[capacity];
            size = 1f;
            SetSize(MinimumSize);
        }

        private static bool TryFindPoint(SpriteRenderer body, Camera camera, out Vector2 position)
        {
            position = default;
            if (body == null || body.sprite == null || camera == null) return false;
            var sprite = body.sprite;
            var outlines = new List<Vector2>[sprite.GetPhysicsShapeCount()];
            for (int i = 0; i < outlines.Length; i++)
            {
                outlines[i] = new List<Vector2>();
                sprite.GetPhysicsShape(i, outlines[i]);
            }

            var bounds = sprite.bounds;
            var viewport = camera.pixelRect;
            for (int attempt = 0; attempt < 48; attempt++)
            {
                var candidate = new Vector2(Random.Range(bounds.min.x, bounds.max.x), Random.Range(bounds.min.y, bounds.max.y));
                foreach (var outline in outlines)
                {
                    if (!Contains(outline, candidate)) continue;
                    var local = new Vector2(body.flipX ? -candidate.x : candidate.x, body.flipY ? -candidate.y : candidate.y);
                    var screen = camera.WorldToScreenPoint(body.transform.TransformPoint(local));
                    if (screen.z <= 0 || !viewport.Contains(screen)) break;
                    position = local;
                    return true;
                }
            }
            return false;
        }

        private static bool Contains(List<Vector2> outline, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = outline.Count - 1; i < outline.Count; j = i++)
            {
                var a = outline[i];
                var b = outline[j];
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        private void SetSize(float next)
        {
            if (size == next) return;
            float ratio = next / size;
            for (int i = 0; i < emitters.Length; i++)
            {
                var emitter = emitters[i];
                var main = emitter.main;
                var authored = authoredSizes[i];
                switch (authored.mode)
                {
                    case ParticleSystemCurveMode.Constant:
                        authored.constant *= next;
                        break;
                    case ParticleSystemCurveMode.TwoConstants:
                        authored.constantMin *= next;
                        authored.constantMax *= next;
                        break;
                    default:
                        authored.curveMultiplier *= next;
                        break;
                }
                main.startSize = authored;
                if (!showing) continue;
                int count = emitter.GetParticles(particles);
                for (int j = 0; j < count; j++) particles[j].startSize *= ratio;
                if (count > 0) emitter.SetParticles(particles, count);
            }
            size = next;
        }

        private void ResetProgress()
        {
            progressSeconds = 0;
            if (feedback != null) feedback.HideProgress();
        }

        private void HideTreasure()
        {
            ResetProgress();
            if (!showing) return;
            showing = false;
            if (treasure != null)
            {
                treasure.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                SetSize(MinimumSize);
            }
        }

        private void ClearTreasure()
        {
            HideTreasure();
            if (treasure != null) Destroy(treasure.gameObject);
            treasure = null;
            emitters = null;
            authoredSizes = null;
            particles = null;
        }

        private void OnDisable() => HideTreasure();
        private void OnDestroy() => ClearTreasure();
        private void OnApplicationPause(bool value) { paused = value; if (value) HideTreasure(); }
        private void OnApplicationFocus(bool focused) { unfocused = !focused; if (!focused) HideTreasure(); }
    }
}
