using System;
using UnityEngine;

namespace ImpactDrive.Runtime
{
    /// <summary>A frame-independent snapshot sampled by the fixed-step simulation.</summary>
    [Serializable]
    public struct DriverInput
    {
        [Range(-1f, 1f)] public float Steering;
        [Range(0f, 1f)] public float Throttle;
        [Range(0f, 1f)] public float Brake;
        [Range(0f, 1f)] public float Handbrake;

        public DriverInput Sanitized()
        {
            return new DriverInput
            {
                Steering = FiniteClamp(Steering, -1f, 1f),
                Throttle = FiniteClamp(Throttle, 0f, 1f),
                Brake = FiniteClamp(Brake, 0f, 1f),
                Handbrake = FiniteClamp(Handbrake, 0f, 1f)
            };
        }

        private static float FiniteClamp(float value, float min, float max)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Clamp(value, min, max);
        }
    }
}
