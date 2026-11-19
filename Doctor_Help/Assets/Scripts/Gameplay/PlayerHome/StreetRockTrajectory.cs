using UnityEngine;

namespace DoctorHelp
{
    internal readonly struct StreetRockTrajectory
    {
        internal const float Gravity = 22f;
        private const float MaxRange = 10f;
        private const float SkyThrowSpeed = 30f;
        private static readonly float DefaultSlope = Mathf.Tan(20f * Mathf.Deg2Rad);
        private static readonly Vector2 SteepestDownwardAim = new Vector2(
            Mathf.Cos(35f * Mathf.Deg2Rad), -Mathf.Sin(35f * Mathf.Deg2Rad));

        internal Vector2 Origin { get; }
        internal Vector2 Velocity { get; }
        internal float Ground { get; }
        internal float Duration { get; }

        private StreetRockTrajectory(Vector2 origin, Vector2 velocity, float ground)
        {
            Origin = origin;
            Velocity = velocity;
            Ground = ground;
            float height = Mathf.Max(0f, origin.y - ground);
            Duration = (velocity.y + Mathf.Sqrt(velocity.y * velocity.y + 2f * Gravity * height)) / Gravity;
        }

        internal static StreetRockTrajectory FromDrag(Vector2 origin, float ground, bool facesRight, Vector2 drag)
        {
            float direction = facesRight ? 1f : -1f;
            float height = Mathf.Max(0f, origin.y - ground);
            float horizontalSpeed = MaxRange * Mathf.Sqrt(Gravity / (2f * (height + MaxRange * DefaultSlope)));
            var velocity = new Vector2(direction * horizontalSpeed, horizontalSpeed * DefaultSlope);
            if (drag.sqrMagnitude > 0.012f * 0.012f)
            {
                var aim = Mathf.Abs(drag.x) <= 0.008f && drag.y > 0f
                    ? Vector2.up
                    : new Vector2(Mathf.Abs(drag.x), drag.y).normalized;
                if (aim.y < SteepestDownwardAim.y) aim = SteepestDownwardAim;
                if (Mathf.Abs(drag.x) > 0.008f) direction = Mathf.Sign(drag.x);
                float skyAim = Mathf.InverseLerp(velocity.normalized.y, 1f, aim.y);
                float speed = Mathf.Lerp(velocity.magnitude, SkyThrowSpeed, skyAim);

                // Limit horizontal reach without imposing a height cap on vertical throws.
                float rangeFactor = aim.x * (height * aim.x + MaxRange * aim.y);
                if (rangeFactor > 0f)
                    speed = Mathf.Min(speed, MaxRange * Mathf.Sqrt(Gravity / (2f * rangeFactor)));
                velocity = new Vector2(direction * aim.x, aim.y) * speed;
            }
            return new StreetRockTrajectory(origin, velocity, ground);
        }

        internal static StreetRockTrajectory ToTarget(Vector2 origin, float ground, bool right, Vector2 target)
        {
            Vector2 delta = target - origin;
            if (Mathf.Abs(delta.x) > MaxRange || (Mathf.Abs(delta.x) > 0.1f && (delta.x > 0f) != right))
                return FromDrag(origin, ground, right, Vector2.zero);

            float arcHeight = Mathf.Abs(delta.x) * DefaultSlope - delta.y;
            // Close or elevated targets need a steeper arc, including a sign directly overhead.
            float time = arcHeight > 0.7f
                ? Mathf.Sqrt(2f * arcHeight / Gravity)
                : Mathf.Lerp(0.4f, 1.05f, Mathf.Clamp01(delta.y / 9f));
            var velocity = new Vector2(delta.x / time, delta.y / time + 0.5f * Gravity * time);
            return new StreetRockTrajectory(origin, velocity, ground);
        }

        internal Vector2 At(float time)
        {
            time = Mathf.Min(time, Duration);
            float x = Mathf.Clamp(Velocity.x * time, -MaxRange, MaxRange);
            float y = Velocity.y * time - 0.5f * Gravity * time * time;
            return new Vector2(Origin.x + x, Mathf.Max(Ground, Origin.y + y));
        }
    }
}
