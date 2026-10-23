using ImpactDrive.Physics;

namespace ImpactDrive.HeadlessTests;

/// <summary>
/// Synthetic one-dimensional contact bench: four fixed normal loads, one translational
/// degree of freedom, actual tire/powertrain model source. It catches coupling instability
/// but does not include Unity contacts, suspension, load transfer, ABS/TC or steering.
/// Its metrics are diagnostics, not validated vehicle performance or Android benchmarks.
/// </summary>
internal sealed class LongitudinalBench
{
    private readonly VehicleSpec spec = VehicleSpec.CreateAster();
    private readonly SurfaceParameters surface;
    private readonly Powertrain engine;
    private readonly TireState[] tireStates = new TireState[4];
    private readonly float[] wheelOmega = new float[4];
    public float VelocityMps { get; private set; }
    public float DistanceM { get; private set; }
    public float ElapsedS { get; private set; }

    public LongitudinalBench(SurfaceParameters surface, float initialSpeedMps = 0, int initialGear = 1)
    {
        this.surface = surface;
        VelocityMps = initialSpeedMps;
        engine = new Powertrain(spec);
        engine.SetGear(initialGear);
        for (int i = 0; i < 4; i++) wheelOmega[i] = initialSpeedMps / spec.WheelRadiusM;
    }

    public void Step(float throttle, float brake, float dt)
    {
        engine.Step(throttle, (wheelOmega[0] + wheelOmega[1]) * 0.5f, dt);
        float summedForce = 0;
        const int wheelSubsteps = 16;
        float subDt = dt / wheelSubsteps;
        for (int i = 0; i < 4; i++)
        {
            bool front = i < 2;
            float verticalLoad = spec.MassKg * 9.80665f * (front ? spec.FrontWeightFraction : 1 - spec.FrontWeightFraction) * 0.5f;
            float driveTorque = front ? engine.OutputTorqueNm * 0.5f : 0;
            float brakeTorque = brake * spec.MaxBrakeTorqueNm * (front ? spec.FrontBrakeBias : 1 - spec.FrontBrakeBias) * 0.5f;
            for (int j = 0; j < wheelSubsteps; j++)
            {
                var contact = TireModel.Evaluate(ref tireStates[i], VelocityMps, 0, wheelOmega[i], verticalLoad,
                    spec.WheelRadiusM, spec.Tire, surface, subDt);
                float rollingTorque = surface.RollingResistance * verticalLoad * spec.WheelRadiusM * Math.Clamp(wheelOmega[i], -1, 1);
                float tentativeOmega = wheelOmega[i] + (driveTorque - contact.LongitudinalN * spec.WheelRadiusM - rollingTorque) * subDt / spec.WheelInertiaKgM2;
                float stoppedOmega = Math.Max(0, Math.Abs(tentativeOmega) - brakeTorque * subDt / spec.WheelInertiaKgM2);
                wheelOmega[i] = Math.Sign(tentativeOmega) * stoppedOmega;
                summedForce += contact.LongitudinalN / wheelSubsteps;
                Check.Between(wheelOmega[i], -3000, 3000, "wheel angular integration must remain finite and bounded");
            }
        }
        float dragForce = 0.5f * 1.225f * spec.DragCoefficient * spec.FrontalAreaM2 * VelocityMps * Math.Abs(VelocityMps);
        VelocityMps += (summedForce - dragForce) * dt / spec.MassKg;
        DistanceM += VelocityMps * dt;
        ElapsedS += dt;
        Check.Between(VelocityMps, -150, 150, "translation integration must remain finite and bounded");
    }

    public static void Register(TestSuite tests)
    {
        tests.Run("coupled bench/neutral resting wheels cannot spontaneously accelerate", () =>
        {
            var bench = new LongitudinalBench(SurfaceParameters.DryAsphalt, 0, 0);
            for (int i = 0; i < 2400; i++) bench.Step(0, 0, 1f / 120);
            Check.Near(bench.VelocityMps, 0, 1e-6, "zero initial state remains at rest with no applied torque");
            Check.Near(bench.DistanceM, 0, 1e-6, "zero initial state does not creep");
        });
        tests.Run("coupled bench/coasting dissipates translational energy", () =>
        {
            var bench = new LongitudinalBench(SurfaceParameters.DryAsphalt, 20, 0);
            for (int i = 0; i < 1200; i++) bench.Step(0, 0, 1f / 120);
            Check.Between(bench.VelocityMps, 0, 19.9, "rolling and aerodynamic resistance must slow a coasting vehicle");
        });
        tests.Run("coupled bench/launch accelerates with bounded wheel reaction torque", () =>
        {
            var bench = new LongitudinalBench(SurfaceParameters.DryAsphalt);
            for (int i = 0; i < 1200; i++) bench.Step(1, 0, 1f / 120);
            tests.Measure("fixed-load bench speed after 10s full throttle", bench.VelocityMps * 3.6, "km/h");
            Check.Between(bench.VelocityMps, 5, 50, "coupled launch should drive forward without divergence");
            Check.True(bench.DistanceM > 30, "propulsion must cover distance through tire contact");
        });
        tests.Run("coupled bench/60Hz and 120Hz retain similar launch response", () =>
        {
            var sixty = new LongitudinalBench(SurfaceParameters.DryAsphalt);
            var oneTwenty = new LongitudinalBench(SurfaceParameters.DryAsphalt);
            for (int i = 0; i < 600; i++) sixty.Step(1, 0, 1f / 60);
            for (int i = 0; i < 1200; i++) oneTwenty.Step(1, 0, 1f / 120);
            tests.Measure("fixed-load bench 60Hz speed after 10s", sixty.VelocityMps * 3.6, "km/h");
            tests.Measure("fixed-load bench 120Hz speed after 10s", oneTwenty.VelocityMps * 3.6, "km/h");
            Check.Near(sixty.VelocityMps, oneTwenty.VelocityMps, oneTwenty.VelocityMps * 0.15, "timestep change must not radically change launch dynamics");
        });
        tests.Run("coupled bench/reverse drivetrain drives negative translation", () =>
        {
            var bench = new LongitudinalBench(SurfaceParameters.DryAsphalt, 0, -1);
            for (int i = 0; i < 600; i++) bench.Step(0.5f, 0, 1f / 120);
            Check.True(bench.VelocityMps < -2 && bench.DistanceM < -5, "reverse gearing must reverse actual tire/body coupling");
        });
        tests.Run("coupled bench/ice requires longer locked-wheel stop than dry asphalt", () =>
        {
            float StopDistance(SurfaceParameters surface)
            {
                var bench = new LongitudinalBench(surface, 100f / 3.6f, 0);
                for (int i = 0; i < 120 * 90 && bench.VelocityMps > 0.5f; i++) bench.Step(0, 1, 1f / 120);
                Check.True(bench.VelocityMps <= 0.5f, "braking must bring this diagnostic to a stop");
                return bench.DistanceM;
            }
            float dry = StopDistance(SurfaceParameters.DryAsphalt);
            float ice = StopDistance(SurfaceParameters.Ice);
            tests.Measure("fixed-load bench dry 100km/h locked-wheel stop to 0.5m/s", dry, "m");
            tests.Measure("fixed-load bench ice 100km/h locked-wheel stop to 0.5m/s", ice, "m");
            Check.True(dry > 10 && ice > dry * 2.5f, "surface differences must materially affect stopping distance");
        });
    }
}
