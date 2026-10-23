using System;
using UnityEngine;

namespace ImpactDrive.Runtime
{
    [Serializable]
    public struct WheelTelemetry
    {
        public bool Grounded;
        public float CompressionM;
        public float SuspensionLengthM;
        public float NormalLoadN;
        public float SlipRatio;
        public float SlipAngleRad;
        public float AngularVelocityRadS;
        public float SteerAngleRad;
        public float BrakeTemperatureC;
        public bool AbsActive;
        /// <summary>X is longitudinal force, Y is lateral force, both in newtons.</summary>
        public Vector2 TireForceN;
        public Vector3 ContactPoint;
        public float AlignmentDamageRad;
        public DriveSurface Surface;
    }
}
