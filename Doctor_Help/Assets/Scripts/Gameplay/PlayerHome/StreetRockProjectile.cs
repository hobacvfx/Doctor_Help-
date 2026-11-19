using UnityEngine;

namespace DoctorHelp
{
    internal sealed class StreetRockProjectile
    {
        private const float GroundLifetime = 2f;

        internal Transform Visual { get; }
        private readonly StreetRockTrajectory trajectory;
        private readonly float depth;
        private readonly float direction;
        private readonly StreetRockThrowing.Target target;
        private Vector2 position;
        private Vector2 bounceVelocity;
        private float elapsed;
        private float remaining = GroundLifetime;
        private bool bounced;
        private bool landed;

        internal StreetRockProjectile(Transform visual, StreetRockTrajectory trajectory, float depth,
            StreetRockThrowing.Target target)
        {
            Visual = visual;
            this.trajectory = trajectory;
            this.depth = depth;
            this.target = target;
            direction = Mathf.Sign(trajectory.Velocity.x);
            position = trajectory.Origin;
            ApplyPosition();
        }

        internal bool Step(float dt, StreetRockThrowing street)
        {
            if (Visual == null) return false;
            if (landed)
            {
                remaining -= dt;
                return remaining > 0f;
            }

            if (bounced)
            {
                position += bounceVelocity * dt + Vector2.down * (0.5f * StreetRockTrajectory.Gravity * dt * dt);
                bounceVelocity.y -= StreetRockTrajectory.Gravity * dt;
                landed = position.y <= trajectory.Ground;
                position.y = Mathf.Max(trajectory.Ground, position.y);
            }
            else
            {
                elapsed += dt;
                var next = trajectory.At(elapsed);
                if (street.TryImpact(target, position, next, depth, out var hit))
                {
                    position = hit;
                    bounceVelocity = new Vector2(-direction * Mathf.Clamp(Mathf.Abs(trajectory.Velocity.x) * 0.3f,
                        0.8f, 4f), 2.8f);
                    bounced = true;
                }
                else
                {
                    position = next;
                    landed = elapsed >= trajectory.Duration;
                }
            }

            ApplyPosition();
            Visual.Rotate(0f, 0f, (bounced ? direction : -direction) * 360f * dt);
            return true;
        }

        private void ApplyPosition() => Visual.position = new Vector3(position.x, position.y, depth);
    }
}
