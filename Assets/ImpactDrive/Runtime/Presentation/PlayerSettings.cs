using System;
using UnityEngine;

namespace ImpactDrive.Runtime
{
    public enum SteeringControlMode { Stick, Wheel, Buttons, Tilt }

    [Serializable]
    public sealed class PlayerSettings
    {
        public int Version = 1;
        public SteeringControlMode SteeringMode = SteeringControlMode.Stick;
        public float SteeringSensitivity = 1f;
        public float SteeringDeadZone = .04f;
        public float TiltCenter;
        public float SoundVolume = .45f;
        public int TargetFrameRate = 60;
        public int QualityLevel = 1;
        public bool AbsEnabled = true;
        public bool TractionControlEnabled = true;
        public bool SteeringAssistEnabled = true;
        public bool ShowTelemetry = true;

        public void Validate()
        {
            if (Version != 1) throw new ArgumentException("Unsupported settings version.");
            if ((int)SteeringMode < 0 || (int)SteeringMode > 3) SteeringMode = SteeringControlMode.Stick;
            SteeringSensitivity = FiniteClamp(SteeringSensitivity, .4f, 1.8f, 1f);
            SteeringDeadZone = FiniteClamp(SteeringDeadZone, 0f, .25f, .04f);
            SoundVolume = FiniteClamp(SoundVolume, 0f, 1f, .45f);
            TiltCenter = FiniteClamp(TiltCenter, -1f, 1f, 0f);
            TargetFrameRate = TargetFrameRate == 30 ? 30 : 60;
            QualityLevel = Mathf.Clamp(QualityLevel, 0, Math.Max(0, QualitySettings.names.Length - 1));
        }

        private static float FiniteClamp(float value, float minimum, float maximum, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, minimum, maximum);
        }
    }
}
