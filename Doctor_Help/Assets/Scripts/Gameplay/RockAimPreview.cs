using UnityEngine;

namespace DoctorHelp
{
    internal sealed class RockAimPreview
    {
        private const float Spacing = 0.62f;
        private const float DashLength = 0.34f;
        private readonly LineRenderer[] dashes = new LineRenderer[32];
        private readonly Vector3[] points = new Vector3[4];
        private readonly Transform parent;
        private readonly Material material;

        private readonly int sortingLayer, sortingOrder;
        private readonly float width;
        private readonly Color color;

        internal RockAimPreview(Transform parent, Material material)
            : this(parent, material, 0, 21, .095f, new Color(1f, .89f, .65f, .92f)) { }

        internal RockAimPreview(Transform parent, Material material, int sortingLayer, int sortingOrder,
            float width, Color color)
        {
            this.parent = parent;
            this.material = material;
            this.sortingLayer = sortingLayer;
            this.sortingOrder = sortingOrder;
            this.width = width;
            this.color = color;
        }

        internal void Show(StreetRockTrajectory trajectory, float depth)
        {
            var previous = trajectory.Origin;
            float distance = 0f;
            int index = 0;
            float nextDash = 0.25f;
            float apexTime = Mathf.Clamp(trajectory.Velocity.y / StreetRockTrajectory.Gravity, 0f, trajectory.Duration);
            var apex = trajectory.At(apexTime);
            bool vertical = trajectory.Velocity.x == 0f && trajectory.Velocity.y > 0f;
            float previewDuration = vertical ? apexTime : trajectory.Duration;
            var endPoint = trajectory.At(previewDuration);
            float pathBound = Mathf.Abs(endPoint.x - previous.x) + (apex.y - previous.y) + (apex.y - endPoint.y);
            float spacing = Mathf.Max(Spacing, pathBound / dashes.Length);
            const float stepTime = 0.02f;
            for (float time = 0f; time < previewDuration && index < dashes.Length; time += stepTime)
            {
                float segmentTime = Mathf.Min(stepTime, previewDuration - time);
                var next = trajectory.At(time + segmentTime);
                float segment = Vector2.Distance(previous, next);
                if (segment > 0f && distance + segment >= nextDash)
                {
                    float start = time + segmentTime * ((nextDash - distance) / segment);
                    float end = Mathf.Min(previewDuration, start + DashLength * segmentTime / segment);
                    for (int point = 0; point < points.Length; point++)
                    {
                        Vector2 position = trajectory.At(Mathf.Lerp(start, end, point / (points.Length - 1f)));
                        points[point] = new Vector3(position.x, position.y, depth);
                    }
                    ShowDash(index++);
                    nextDash += spacing;
                }
                distance += segment;
                previous = next;
            }
            HideFrom(index);
        }

        internal void ShowOffice(Vector3 origin, Vector3 target, float arcHeight)
        {
            // Estimate the projected curve length to keep spacing readable on short and long throws.
            var middle = OfficePosition(origin, target, arcHeight, .5f);
            float length = Vector2.Distance(origin, middle) + Vector2.Distance(middle, target);
            const float markerClearance = .22f;
            float visibleLength = length - markerClearance;
            if (visibleLength <= 0f) { Hide(); return; }

            int count = Mathf.Clamp(Mathf.CeilToInt(visibleLength / .36f), 1, dashes.Length);
            float step = visibleLength / (length * count);
            for (int index = 0; index < count; index++)
            {
                for (int point = 0; point < points.Length; point++)
                {
                    float progress = (index + .62f * point / (points.Length - 1f)) * step;
                    points[point] = OfficePosition(origin, target, arcHeight, progress);
                }
                ShowDash(index);
            }
            HideFrom(count);
        }

        internal static Vector3 OfficePosition(Vector3 origin, Vector3 target, float arcHeight, float progress)
        {
            var position = Vector3.Lerp(origin, target, progress);
            position.y += 4f * arcHeight * progress * (1f - progress);
            return position;
        }

        private void ShowDash(int index)
        {
            var dash = dashes[index] ??= CreateDash();
            dash.SetPositions(points);
            dash.enabled = true;
        }

        internal void Hide() => HideFrom(0);

        private void HideFrom(int index)
        {
            for (; index < dashes.Length && dashes[index] != null; index++)
                dashes[index].enabled = false;
        }

        private LineRenderer CreateDash()
        {
            var line = new GameObject("Aim Dash").AddComponent<LineRenderer>();
            line.transform.SetParent(parent, false);
            line.sharedMaterial = material;
            line.sortingLayerID = sortingLayer;
            line.sortingOrder = sortingOrder;
            line.positionCount = points.Length;
            line.startWidth = line.endWidth = width;
            line.startColor = line.endColor = color;
            line.numCapVertices = 3;
            return line;
        }
    }
}
