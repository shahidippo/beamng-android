using System;

namespace ImpactDrive.Physics
{
    [Serializable]
    public sealed class TireParameters
    {
        public float FrictionCoefficient = 1f;
        public float LongitudinalStiffnessN = 70000f;
        public float CorneringStiffnessNPerRad = 60000f;
        public float ReferenceLoadN = 3200f;
        public float LoadSensitivity = 0.12f;
        public float LongitudinalRelaxationLengthM = 0.25f;
        public float LateralRelaxationLengthM = 0.40f;
        public float LowSpeedRegularizationMps = 0.8f;
        public static TireParameters Road() => new TireParameters();
        public void Validate()
        {
            Scalar.Range(FrictionCoefficient, 0.01f, 3f, nameof(FrictionCoefficient));
            Scalar.Range(LongitudinalStiffnessN, 100f, 2000000f, nameof(LongitudinalStiffnessN));
            Scalar.Range(CorneringStiffnessNPerRad, 100f, 2000000f, nameof(CorneringStiffnessNPerRad));
            Scalar.Range(ReferenceLoadN, 10f, 100000f, nameof(ReferenceLoadN));
            Scalar.Range(LoadSensitivity, 0f, 0.5f, nameof(LoadSensitivity));
            Scalar.Range(LongitudinalRelaxationLengthM, 0.001f, 5f, nameof(LongitudinalRelaxationLengthM));
            Scalar.Range(LateralRelaxationLengthM, 0.001f, 5f, nameof(LateralRelaxationLengthM));
            Scalar.Range(LowSpeedRegularizationMps, 0.1f, 5f, nameof(LowSpeedRegularizationMps));
        }
    }

    /// <summary>
    /// Contact parameters are initial design assumptions. Gravel/sand/mud are rigid ground
    /// approximations: no terrain sinkage or soil shear-strength model is implemented here.
    /// </summary>
    [Serializable]
    public sealed class SurfaceParameters
    {
        public string Name;
        public float FrictionCoefficient;
        public float StiffnessScale;
        public float RelaxationScale;
        public float RollingResistance;
        public float SlidingFrictionRatio;
        public SurfaceParameters(string name, float frictionCoefficient, float stiffnessScale,
            float relaxationScale, float rollingResistance, float slidingFrictionRatio)
        {
            Name = name;
            FrictionCoefficient = frictionCoefficient;
            StiffnessScale = stiffnessScale;
            RelaxationScale = relaxationScale;
            RollingResistance = rollingResistance;
            SlidingFrictionRatio = slidingFrictionRatio;
        }
        // Each access gives independent editable data, never shared mutable global state.
        public static SurfaceParameters DryAsphalt => new SurfaceParameters("Dry asphalt", 1f, 1f, 1f, 0.013f, 0.87f);
        public static SurfaceParameters WetAsphalt => new SurfaceParameters("Wet asphalt", 0.66f, 0.87f, 1.12f, 0.015f, 0.78f);
        public static SurfaceParameters Concrete => new SurfaceParameters("Concrete", 0.95f, 1.02f, 1f, 0.013f, 0.87f);
        public static SurfaceParameters Gravel => new SurfaceParameters("Gravel", 0.58f, 0.42f, 1.8f, 0.035f, 0.92f);
        public static SurfaceParameters Dirt => new SurfaceParameters("Compact dirt", 0.56f, 0.55f, 1.5f, 0.028f, 0.90f);
        public static SurfaceParameters Sand => new SurfaceParameters("Sand", 0.42f, 0.25f, 2.4f, 0.10f, 0.94f);
        public static SurfaceParameters Mud => new SurfaceParameters("Mud", 0.35f, 0.20f, 2.8f, 0.12f, 0.82f);
        public static SurfaceParameters Grass => new SurfaceParameters("Grass", 0.43f, 0.35f, 1.7f, 0.045f, 0.80f);
        public static SurfaceParameters Snow => new SurfaceParameters("Packed snow", 0.26f, 0.32f, 1.8f, 0.045f, 0.83f);
        public static SurfaceParameters Ice => new SurfaceParameters("Ice", 0.10f, 0.22f, 1.35f, 0.008f, 0.72f);
        public void Validate()
        {
            Scalar.Range(FrictionCoefficient, 0f, 3f, nameof(FrictionCoefficient));
            Scalar.Range(StiffnessScale, 0.01f, 3f, nameof(StiffnessScale));
            Scalar.Range(RelaxationScale, 0.01f, 10f, nameof(RelaxationScale));
            Scalar.Range(RollingResistance, 0f, 0.5f, nameof(RollingResistance));
            Scalar.Range(SlidingFrictionRatio, 0.1f, 1f, nameof(SlidingFrictionRatio));
        }
    }

    [Serializable]
    public struct TireState
    {
        public float RelaxedSlipRatio;
        public float RelaxedSlipAngleRad;
    }

    public struct TireForces
    {
        public float LongitudinalN;
        public float LateralN;
        public float SlipRatio;
        public float SlipAngleRad;
        public float GripLimitN;
    }

    /// <summary>
    /// Reduced combined-slip brush model. The cubic adhesion-to-sliding transition has the
    /// shape of Fiala's lateral brush law, applied to a two-dimensional elastic force demand.
    /// This is explicitly not a fitted Pacejka model or a complete Fiala implementation.
    /// Forward is +longitudinal; lateral velocity right produces a leftward road force.
    /// </summary>
    public static class TireModel
    {
        public static TireForces Evaluate(ref TireState state, float longitudinalVelocityMps,
            float lateralVelocityMps, float wheelAngularVelocityRadS, float normalLoadN,
            float radiusM, TireParameters tire, SurfaceParameters surface, float deltaTime)
        {
            if (tire == null) throw new ArgumentNullException(nameof(tire));
            if (surface == null) throw new ArgumentNullException(nameof(surface));
            Scalar.TimeStep(deltaTime);
            Scalar.RequireFinite(longitudinalVelocityMps, nameof(longitudinalVelocityMps));
            Scalar.RequireFinite(lateralVelocityMps, nameof(lateralVelocityMps));
            Scalar.RequireFinite(wheelAngularVelocityRadS, nameof(wheelAngularVelocityRadS));
            Scalar.Range(normalLoadN, 0f, 1000000f, nameof(normalLoadN));
            Scalar.Range(radiusM, 0.01f, 3f, nameof(radiusM));
            if (normalLoadN <= 0f)
            {
                state = default;
                return default;
            }

            float rollingVelocity = wheelAngularVelocityRadS * radiusM;
            float speedDenominator = Math.Max(tire.LowSpeedRegularizationMps,
                Math.Max(Math.Abs(longitudinalVelocityMps), Math.Abs(rollingVelocity)));
            float slipRatio = (rollingVelocity - longitudinalVelocityMps) / speedDenominator;
            float slipAngle = (float)Math.Atan2(lateralVelocityMps,
                Math.Max(tire.LowSpeedRegularizationMps, Math.Abs(longitudinalVelocityMps)));
            // Exact solution of ds/dt=(s_target-s)*v/L for constant input over this step.
            // A low-speed floor avoids a frozen relaxation state when starting from rest.
            float relaxationSpeed = Math.Max(tire.LowSpeedRegularizationMps, Math.Abs(longitudinalVelocityMps));
            float blendX = 1f - (float)Math.Exp(-deltaTime * relaxationSpeed / (tire.LongitudinalRelaxationLengthM * surface.RelaxationScale));
            float blendY = 1f - (float)Math.Exp(-deltaTime * relaxationSpeed / (tire.LateralRelaxationLengthM * surface.RelaxationScale));
            state.RelaxedSlipRatio = Scalar.Lerp(state.RelaxedSlipRatio, slipRatio, blendX);
            state.RelaxedSlipAngleRad = Scalar.Lerp(state.RelaxedSlipAngleRad, slipAngle, blendY);

            float loadRatio = Math.Max(0.05f, normalLoadN / tire.ReferenceLoadN);
            float mu = tire.FrictionCoefficient * surface.FrictionCoefficient * (float)Math.Pow(loadRatio, -tire.LoadSensitivity);
            float grip = mu * normalLoadN;
            if (grip <= 0f) return new TireForces { SlipRatio = slipRatio, SlipAngleRad = slipAngle };

            float stiffnessScale = surface.StiffnessScale * (float)Math.Pow(loadRatio, 0.85f);
            float elasticX = tire.LongitudinalStiffnessN * stiffnessScale * state.RelaxedSlipRatio;
            float elasticY = -tire.CorneringStiffnessNPerRad * stiffnessScale * (float)Math.Tan(state.RelaxedSlipAngleRad);
            float demand = (float)Math.Sqrt(elasticX * elasticX + elasticY * elasticY);
            if (demand < 0.00001f) return new TireForces { SlipRatio = slipRatio, SlipAngleRad = slipAngle, GripLimitN = grip };

            float normalizedDemand = demand / (3f * grip);
            float normalizedForce;
            if (normalizedDemand < 1f)
            {
                float adhesion = 1f - normalizedDemand;
                normalizedForce = 1f - adhesion * adhesion * adhesion;
            }
            else
            {
                // Sliding friction approaches its surface-specific plateau continuously.
                float slidingBlend = 1f - (float)Math.Exp(-(normalizedDemand - 1f) * 0.35f);
                normalizedForce = Scalar.Lerp(1f, surface.SlidingFrictionRatio, slidingBlend);
            }
            float scale = grip * normalizedForce / demand;
            return new TireForces
            {
                LongitudinalN = elasticX * scale,
                LateralN = elasticY * scale,
                SlipRatio = slipRatio,
                SlipAngleRad = slipAngle,
                GripLimitN = grip
            };
        }
    }
}
