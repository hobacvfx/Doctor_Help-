using System.Collections.Generic;
using UnityEngine;

namespace DoctorHelp
{
    // The pipe the rats run along, as ordered waypoints from the right opening to the generator end.
    // Move the waypoint children in the Scene view to retrace the pipe. Positions are measured by length,
    // so long and short pipe runs take proportionate time.
    [DisallowMultipleComponent]
    public sealed class BasementPipePath : MonoBehaviour
    {
        [Tooltip("In running order: the right pipe opening first, the generator end last.")]
        [SerializeField] private Transform[] waypoints = new Transform[0];
        [Tooltip("Half the distance used to average the running direction, so rats turn smoothly at elbows.")]
        [SerializeField, Min(0f)] private float turnSmoothing = .45f;

        private Vector3[] points;
        private float[] distances;

        public float Length { get { Build(); return distances.Length > 0 ? distances[distances.Length - 1] : 0f; } }
        public bool IsValid { get { Build(); return points.Length >= 2 && Length > 0f; } }

        private void Awake() => Build(true);

        // Waypoints do not move at runtime; cache their world positions once.
        private void Build(bool force = false)
        {
            if (!force && points != null) return;
            var list = new List<Vector3>();
            foreach (var waypoint in waypoints)
                if (waypoint != null) list.Add(waypoint.position);
            points = list.ToArray();
            distances = new float[points.Length];
            for (int index = 1; index < points.Length; index++)
                distances[index] = distances[index - 1] + Vector2.Distance(points[index - 1], points[index]);
        }

        public Vector3 Evaluate(float fraction)
        {
            Build();
            if (points.Length == 0) return transform.position;
            if (points.Length == 1) return points[0];
            float target = Mathf.Clamp01(fraction) * Length;
            int segment = 1;
            while (segment < distances.Length - 1 && distances[segment] < target) segment++;
            float span = distances[segment] - distances[segment - 1];
            float t = span > 0f ? (target - distances[segment - 1]) / span : 0f;
            return Vector3.Lerp(points[segment - 1], points[segment], t);
        }

        // Running direction averaged over a short stretch, so a rat rotates through elbows instead of snapping.
        public Vector2 Direction(float fraction)
        {
            float length = Length;
            if (length <= 0f) return Vector2.up;
            float spread = turnSmoothing / length;
            Vector2 behind = Evaluate(fraction - spread), ahead = Evaluate(fraction + spread);
            Vector2 direction = ahead - behind;
            if (direction.sqrMagnitude < 1e-8f)
                direction = (Vector2)Evaluate(fraction + .002f) - (Vector2)Evaluate(fraction - .002f);
            return direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector2.up;
        }

        // Path fractions within radius of a point, as (start, end) windows in path order.
        public Vector2[] WindowsWithin(Vector3 center, float radius, int samples = 4000)
        {
            var windows = new List<Vector2>();
            if (!IsValid || radius <= 0f) return windows.ToArray();
            float start = -1f;
            for (int index = 0; index <= samples; index++)
            {
                float fraction = index / (float)samples;
                bool inside = Vector2.Distance(Evaluate(fraction), center) <= radius;
                if (inside && start < 0f) start = fraction;
                if (!inside && start >= 0f)
                {
                    windows.Add(new Vector2(start, (index - 1) / (float)samples));
                    start = -1f;
                }
            }
            if (start >= 0f) windows.Add(new Vector2(start, 1f));
            return windows.ToArray();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, .55f, .1f, .9f);
            Transform previous = null;
            foreach (var waypoint in waypoints)
            {
                if (waypoint == null) continue;
                Gizmos.DrawSphere(waypoint.position, .18f);
                if (previous != null) Gizmos.DrawLine(previous.position, waypoint.position);
                previous = waypoint;
            }
        }

        private void OnValidate() => points = null;
    }
}
