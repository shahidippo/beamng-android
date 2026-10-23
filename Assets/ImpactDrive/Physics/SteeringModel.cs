using System;

namespace ImpactDrive.Physics
{
    public struct SteeringAngles
    {
        public float LeftRad;
        public float RightRad;
    }

    public static class SteeringModel
    {
        /// <summary>
        /// Positive rack angle turns wheels right in a +Z-forward, +X-right frame.
        /// Reverse uses the same wheel geometry; reversing tyre forces changes the yaw.
        /// </summary>
        public static SteeringAngles Ackermann(float centerAngleRad, float wheelbaseM, float trackM)
        {
            Scalar.Range(centerAngleRad, -1.4f, 1.4f, nameof(centerAngleRad));
            Scalar.Range(wheelbaseM, 0.1f, 30f, nameof(wheelbaseM));
            Scalar.Range(trackM, 0.1f, 10f, nameof(trackM));
            if (Math.Abs(centerAngleRad) < 0.000001f) return new SteeringAngles();
            float radius = wheelbaseM / (float)Math.Tan(Math.Abs(centerAngleRad));
            if (radius <= trackM * 0.5f) throw new ArgumentException("Ackermann inner turning radius must be positive.");
            float inner = (float)Math.Atan(wheelbaseM / (radius - trackM * 0.5f));
            float outer = (float)Math.Atan(wheelbaseM / (radius + trackM * 0.5f));
            return centerAngleRad > 0f
                ? new SteeringAngles { LeftRad = outer, RightRad = inner }
                : new SteeringAngles { LeftRad = -inner, RightRad = -outer };
        }

        public static float MoveRack(float currentAngleRad, float targetAngleRad, float maximumRateRadS, float deltaTime)
        {
            Scalar.RequireFinite(currentAngleRad, nameof(currentAngleRad));
            Scalar.RequireFinite(targetAngleRad, nameof(targetAngleRad));
            Scalar.Range(maximumRateRadS, 0f, 20f, nameof(maximumRateRadS));
            Scalar.TimeStep(deltaTime);
            return currentAngleRad + Scalar.Clamp(targetAngleRad - currentAngleRad, -maximumRateRadS * deltaTime, maximumRateRadS * deltaTime);
        }
    }
}
