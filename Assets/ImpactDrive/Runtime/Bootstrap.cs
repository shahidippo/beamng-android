using UnityEngine;

namespace ImpactDrive.Runtime
{
    /// <summary>Entry point for the explicitly labelled engineering build.</summary>
    public sealed class Bootstrap : MonoBehaviour
    {
        private void Start()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            UnityEngine.Physics.defaultSolverIterations = 8;
            UnityEngine.Physics.defaultSolverVelocityIterations = 4;
            // This baseline is evaluated against 60 Hz by the test harness.
            // Graphics presets never modify simulation frequency.
            Time.fixedDeltaTime = 1f / 120f;
            Time.maximumDeltaTime = 8f / 120f;
            var vehicle = ProvingGroundBuilder.Build();
            GameplayPresentation.Create(vehicle);
        }

    }
}
