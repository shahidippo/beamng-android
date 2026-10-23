using System;

namespace ImpactDrive.Physics
{
    public enum Drivetrain { FrontWheelDrive, RearWheelDrive, AllWheelDrive }

    /// <summary>
    /// SI design data for an original fictional vehicle. Defaults are engineering targets,
    /// not measurements or a claim of agreement with a production car. Validate after editing.
    /// </summary>
    [Serializable]
    public sealed class VehicleSpec
    {
        public string Name = "Aster 1.6";
        public float MassKg = 1280f;
        public float FrontWeightFraction = 0.60f;
        public float WheelbaseM = 2.60f;
        public float FrontTrackM = 1.52f;
        public float RearTrackM = 1.50f;
        public float CenterOfMassHeightM = 0.52f;
        public float WheelRadiusM = 0.305f;
        public float WheelInertiaKgM2 = 1.1f;
        public float SuspensionRestLengthM = 0.35f;
        public float SuspensionTravelM = 0.24f;
        public float SpringRateNPerM = 34000f;
        public float BumpSpringRateNPerM = 180000f;
        public float CompressionDampingNsPerM = 2500f;
        public float ReboundDampingNsPerM = 3600f;
        public float AntiRollRateNPerM = 10000f;
        public float MaxSteerAngleRad = 0.54f;
        public float SteeringRateRadPerS = 1.8f;
        public float DragCoefficient = 0.31f;
        public float FrontalAreaM2 = 2.15f;
        public float RollingResistance = 0.013f;
        // Sum of all four wheel service-brake torques. Runtime distributes by axle bias.
        public float MaxBrakeTorqueNm = 6000f;
        public float FrontBrakeBias = 0.65f;
        // Sum of the two rear wheel parking-brake torques.
        public float MaxHandbrakeTorqueNm = 3000f;
        public Drivetrain Drivetrain = Drivetrain.FrontWheelDrive;
        public TireParameters Tire = TireParameters.Road();
        public float EngineInertiaKgM2 = 0.24f;
        public float IdleRpm = 850f;
        public float RedlineRpm = 6500f;
        public float FinalDriveRatio = 4.1f;
        public float ReverseGearRatio = -3.2f;
        public float[] ForwardGearRatios = { 3.54f, 2.05f, 1.38f, 1.03f, 0.82f, 0.68f };
        public float DrivetrainEfficiency = 0.92f;
        public float ClutchCapacityNm = 250f;
        public float ShiftDurationS = 0.22f;
        public float[] TorqueCurveRpm = { 800f, 1500f, 2500f, 3500f, 4500f, 5500f, 6500f };
        public float[] TorqueCurveNm = { 80f, 114f, 144f, 158f, 157f, 143f, 115f };

        public static VehicleSpec CreateAster() => new VehicleSpec();

        /// <summary>Static spring compression for one wheel on the selected axle, in metres.</summary>
        public float StaticSagM(bool front) => MassKg * 9.80665f * (front ? FrontWeightFraction : 1f - FrontWeightFraction) / (2f * SpringRateNPerM);

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name)) throw new ArgumentException("A vehicle requires a name.");
            Scalar.Range(MassKg, 100f, 40000f, nameof(MassKg));
            Scalar.Range(FrontWeightFraction, 0.1f, 0.9f, nameof(FrontWeightFraction));
            Scalar.Range(WheelbaseM, 0.5f, 15f, nameof(WheelbaseM));
            Scalar.Range(FrontTrackM, 0.5f, 4f, nameof(FrontTrackM));
            Scalar.Range(RearTrackM, 0.5f, 4f, nameof(RearTrackM));
            Scalar.Range(CenterOfMassHeightM, 0.05f, 4f, nameof(CenterOfMassHeightM));
            Scalar.Range(WheelRadiusM, 0.1f, 1.2f, nameof(WheelRadiusM));
            Scalar.Range(WheelInertiaKgM2, 0.05f, 150f, nameof(WheelInertiaKgM2));
            Scalar.Range(SuspensionRestLengthM, 0.05f, 2f, nameof(SuspensionRestLengthM));
            Scalar.Range(SuspensionTravelM, 0.01f, SuspensionRestLengthM, nameof(SuspensionTravelM));
            Scalar.Range(SpringRateNPerM, 1000f, 2000000f, nameof(SpringRateNPerM));
            Scalar.Range(BumpSpringRateNPerM, 0f, 5000000f, nameof(BumpSpringRateNPerM));
            Scalar.Range(CompressionDampingNsPerM, 0f, 100000f, nameof(CompressionDampingNsPerM));
            Scalar.Range(ReboundDampingNsPerM, 0f, 100000f, nameof(ReboundDampingNsPerM));
            Scalar.Range(AntiRollRateNPerM, 0f, 500000f, nameof(AntiRollRateNPerM));
            Scalar.Range(MaxSteerAngleRad, 0.01f, 1.2f, nameof(MaxSteerAngleRad));
            if (WheelbaseM / Math.Tan(MaxSteerAngleRad) <= Math.Max(FrontTrackM, RearTrackM) * 0.5f)
                throw new ArgumentException("Steering lock makes the Ackermann inner radius nonpositive.");
            Scalar.Range(SteeringRateRadPerS, 0.01f, 20f, nameof(SteeringRateRadPerS));
            Scalar.Range(DragCoefficient, 0f, 2f, nameof(DragCoefficient));
            Scalar.Range(FrontalAreaM2, 0.1f, 25f, nameof(FrontalAreaM2));
            Scalar.Range(RollingResistance, 0f, 0.3f, nameof(RollingResistance));
            Scalar.Range(MaxBrakeTorqueNm, 0f, 200000f, nameof(MaxBrakeTorqueNm));
            Scalar.Range(FrontBrakeBias, 0.05f, 0.95f, nameof(FrontBrakeBias));
            Scalar.Range(MaxHandbrakeTorqueNm, 0f, 200000f, nameof(MaxHandbrakeTorqueNm));
            if (!Enum.IsDefined(typeof(Drivetrain), Drivetrain)) throw new ArgumentException("Unsupported drivetrain.");
            Scalar.Range(EngineInertiaKgM2, 0.01f, 10f, nameof(EngineInertiaKgM2));
            Scalar.Range(IdleRpm, 100f, 2000f, nameof(IdleRpm));
            Scalar.Range(RedlineRpm, IdleRpm + 500f, 20000f, nameof(RedlineRpm));
            Scalar.Range(FinalDriveRatio, 0.5f, 20f, nameof(FinalDriveRatio));
            Scalar.Range(ReverseGearRatio, -20f, -0.1f, nameof(ReverseGearRatio));
            Scalar.Range(DrivetrainEfficiency, 0.1f, 1f, nameof(DrivetrainEfficiency));
            Scalar.Range(ClutchCapacityNm, 1f, 10000f, nameof(ClutchCapacityNm));
            Scalar.Range(ShiftDurationS, 0.01f, 2f, nameof(ShiftDurationS));
            if (ForwardGearRatios == null || ForwardGearRatios.Length == 0 || ForwardGearRatios.Length > 12)
                throw new ArgumentException("Expected one to twelve forward gears.");
            foreach (float gear in ForwardGearRatios) Scalar.Range(gear, 0.1f, 20f, nameof(ForwardGearRatios));
            if (TorqueCurveRpm == null || TorqueCurveNm == null || TorqueCurveRpm.Length < 2 || TorqueCurveRpm.Length != TorqueCurveNm.Length)
                throw new ArgumentException("Torque curve requires matching RPM and torque arrays with at least two points.");
            for (int i = 0; i < TorqueCurveRpm.Length; i++)
            {
                Scalar.Range(TorqueCurveRpm[i], 0f, 25000f, nameof(TorqueCurveRpm));
                Scalar.Range(TorqueCurveNm[i], 0f, 10000f, nameof(TorqueCurveNm));
                if (i > 0 && TorqueCurveRpm[i] <= TorqueCurveRpm[i - 1]) throw new ArgumentException("Torque RPM points must strictly increase.");
            }
            if (Tire == null) throw new ArgumentException("Tire parameters are required.");
            Tire.Validate();
        }
    }
}
