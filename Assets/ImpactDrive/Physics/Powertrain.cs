using System;

namespace ImpactDrive.Physics
{
    /// <summary>
    /// One combustion engine, a slipping torque-limited clutch and a stepped gearbox.
    /// Signed torque is delivered at the driven axle; an open differential divides it
    /// equally between wheels. The caller must apply equal-and-opposite tyre wheel torque.
    /// </summary>
    public sealed class Powertrain
    {
        private readonly VehicleSpec spec;
        private float engineOmega;
        private float shiftTimer;
        private float shiftCooldown;
        private bool engineRunning = true;
        private float engineHealth = 1f;
        public int Gear { get; private set; } = 1;
        public bool Automatic { get; set; } = true;
        public float EngineRpm => engineOmega * 60f / Scalar.TwoPi;
        public float OutputTorqueNm { get; private set; }
        public float ClutchTorqueNm { get; private set; }
        public float ClutchSlipRadS { get; private set; }
        public bool IsShifting => shiftTimer > 0f;
        public bool EngineRunning
        {
            get => engineRunning;
            set
            {
                engineRunning = value;
                engineOmega = value ? spec.IdleRpm * Scalar.TwoPi / 60f : 0f;
                OutputTorqueNm = 0f;
                ClutchTorqueNm = 0f;
            }
        }
        public float EngineHealth01
        {
            get => engineHealth;
            set
            {
                Scalar.Range(value, 0f, 1f, nameof(EngineHealth01));
                engineHealth = value;
                if (value <= 0f) EngineRunning = false;
            }
        }

        public Powertrain(VehicleSpec spec)
        {
            this.spec = spec ?? throw new ArgumentNullException(nameof(spec));
            spec.Validate();
            engineOmega = spec.IdleRpm * Scalar.TwoPi / 60f;
        }

        public void SetGear(int gear)
        {
            if (gear < -1 || gear > spec.ForwardGearRatios.Length) throw new ArgumentOutOfRangeException(nameof(gear));
            if (gear == Gear) return;
            Gear = gear;
            shiftTimer = spec.ShiftDurationS;
            shiftCooldown = spec.ShiftDurationS + 0.5f;
            OutputTorqueNm = 0f;
        }
        public void ShiftUp() => SetGear(Math.Min(spec.ForwardGearRatios.Length, Gear + 1));
        public void ShiftDown() => SetGear(Math.Max(-1, Gear - 1));

        public float TorqueAtRpm(float rpm)
        {
            Scalar.RequireFinite(rpm, nameof(rpm));
            if (rpm <= spec.TorqueCurveRpm[0]) return spec.TorqueCurveNm[0];
            for (int i = 1; i < spec.TorqueCurveRpm.Length; i++)
            {
                if (rpm <= spec.TorqueCurveRpm[i])
                    return Scalar.Lerp(spec.TorqueCurveNm[i - 1], spec.TorqueCurveNm[i],
                        (rpm - spec.TorqueCurveRpm[i - 1]) / (spec.TorqueCurveRpm[i] - spec.TorqueCurveRpm[i - 1]));
            }
            return spec.TorqueCurveNm[spec.TorqueCurveNm.Length - 1];
        }

        public void Step(float throttle, float drivenWheelAngularVelocityRadS, float deltaTime)
        {
            Scalar.Range(throttle, 0f, 1f, nameof(throttle));
            Scalar.RequireFinite(drivenWheelAngularVelocityRadS, nameof(drivenWheelAngularVelocityRadS));
            Scalar.TimeStep(deltaTime);
            OutputTorqueNm = 0f;
            ClutchTorqueNm = 0f;
            if (!engineRunning || engineHealth <= 0f)
            {
                engineOmega = 0f;
                ClutchSlipRadS = 0f;
                return;
            }
            shiftTimer = Math.Max(0f, shiftTimer - deltaTime);
            shiftCooldown = Math.Max(0f, shiftCooldown - deltaTime);
            if (Automatic && Gear > 0 && shiftCooldown <= 0f)
            {
                // Estimate gearbox input RPM from the driven-wheel average. Wheelspin can
                // still trigger a shift; torque intervention belongs to traction control.
                float coupledRpm = Math.Abs(drivenWheelAngularVelocityRadS) * spec.ForwardGearRatios[Gear - 1] * spec.FinalDriveRatio * 60f / Scalar.TwoPi;
                float upshiftRpm = Scalar.Lerp(3000f, spec.RedlineRpm * 0.91f, throttle);
                if (coupledRpm > upshiftRpm && Gear < spec.ForwardGearRatios.Length) SetGear(Gear + 1);
                else if (coupledRpm < 1450f && Gear > 1) SetGear(Gear - 1);
            }

            float ratio = Gear < 0 ? spec.ReverseGearRatio : Gear == 0 ? 0f : spec.ForwardGearRatios[Gear - 1];
            ratio *= spec.FinalDriveRatio;
            float coupledOmega = drivenWheelAngularVelocityRadS * ratio;
            float idleOmega = spec.IdleRpm * Scalar.TwoPi / 60f;
            float redlineOmega = spec.RedlineRpm * Scalar.TwoPi / 60f;
            float engineTorque = EngineRpm < spec.RedlineRpm ? TorqueAtRpm(EngineRpm) * throttle * engineHealth : 0f;
            float dragTorque = 8f + 0.025f * engineOmega;
            // Idle governor supplies real torque, never a direct RPM floor or road force.
            float idleTorque = Scalar.Clamp((idleOmega - engineOmega) * 3f + 8f + 0.025f * idleOmega, 0f, 65f) * (1f - throttle);

            // Launch clutch opens below idle, engages progressively under throttle, and
            // stays coupled while moving. Explicit manual clutch/stall mode is deferred.
            float launchRpm = spec.IdleRpm + 900f * throttle;
            float engagement = Scalar.Clamp((EngineRpm - spec.IdleRpm * 0.72f) / Math.Max(100f, launchRpm - spec.IdleRpm * 0.72f), 0f, 1f);
            if (Math.Abs(coupledOmega) < idleOmega && throttle < 0.02f) engagement = 0f;
            if (ratio == 0f || shiftTimer > 0f) engagement = 0f;
            float couplingNs = 2.2f * engagement;
            float capacity = spec.ClutchCapacityNm * engagement;
            float noClutchTorque = engineTorque + idleTorque - dragTorque;
            // Implicit linear clutch solve prevents a stiff coupling from destabilizing
            // engine inertia at 60 Hz; friction saturation remains torque limited.
            float candidateOmega = (engineOmega + deltaTime * (noClutchTorque + couplingNs * coupledOmega) / spec.EngineInertiaKgM2)
                / (1f + deltaTime * couplingNs / spec.EngineInertiaKgM2);
            float clutchTorque = Scalar.Clamp(couplingNs * (candidateOmega - coupledOmega), -capacity, capacity);
            engineOmega = Math.Max(0f, engineOmega + (noClutchTorque - clutchTorque) * deltaTime / spec.EngineInertiaKgM2);
            // Mechanical overspeed is bounded for numerical safety. Real failure/heat
            // from sustained overspeed remains a future powertrain validation task.
            engineOmega = Math.Min(redlineOmega * 1.25f, engineOmega);
            ClutchTorqueNm = clutchTorque;
            ClutchSlipRadS = engineOmega - coupledOmega;
            // Efficiency always removes power: reverse flow requires larger axle torque
            // to deliver a given back-driven engine torque through the lossy gearbox.
            bool engineDriving = clutchTorque * coupledOmega >= 0f;
            float lossScale = engineDriving ? spec.DrivetrainEfficiency : 1f / spec.DrivetrainEfficiency;
            OutputTorqueNm = clutchTorque * ratio * lossScale;
        }
    }
}
