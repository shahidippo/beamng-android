using System;

namespace ImpactDrive.Physics
{
    /// <summary>Small deterministic scalar helpers shared by the engine-independent models.</summary>
    internal static class Scalar
    {
        public const float TwoPi = 6.283185307179586f;
        public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;
        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static void RequireFinite(float value, string name)
        {
            if (!Finite(value)) throw new ArgumentOutOfRangeException(name, "Value must be finite.");
        }
        public static void Range(float value, float min, float max, string name)
        {
            if (!Finite(value) || value < min || value > max)
                throw new ArgumentOutOfRangeException(name, $"Expected a finite value in [{min}, {max}].");
        }
        public static void TimeStep(float deltaTime)
        {
            Range(deltaTime, 0.000001f, 0.1f, nameof(deltaTime));
        }
    }
}
