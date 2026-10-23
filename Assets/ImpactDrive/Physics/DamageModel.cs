using System;
using System.Collections.Generic;

namespace ImpactDrive.Physics
{
    [Serializable]
    public struct SimVector
    {
        public float X, Y, Z;
        public SimVector(float x, float y, float z) { X = x; Y = y; Z = z; }
        public float LengthSquared => X * X + Y * Y + Z * Z;
        public float Length => (float)Math.Sqrt(LengthSquared);
        public SimVector Normalized => Length > 0.000001f ? this / Length : default;
        public static SimVector operator +(SimVector a, SimVector b) => new SimVector(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static SimVector operator -(SimVector a, SimVector b) => new SimVector(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static SimVector operator *(SimVector a, float scalar) => new SimVector(a.X * scalar, a.Y * scalar, a.Z * scalar);
        public static SimVector operator /(SimVector a, float scalar) => new SimVector(a.X / scalar, a.Y / scalar, a.Z / scalar);
        public static float Dot(SimVector a, SimVector b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public static float Distance(SimVector a, SimVector b) => (a - b).Length;
        internal void Validate(string name)
        {
            Scalar.RequireFinite(X, name + ".X");
            Scalar.RequireFinite(Y, name + ".Y");
            Scalar.RequireFinite(Z, name + ".Z");
        }
    }

    [Serializable]
    public sealed class StructuralNode
    {
        public SimVector OriginalPosition;
        public SimVector Position;
        public float YieldForceN;
        public float HardeningNPerM;
        public float MaximumPlasticTravelM;
        // Accumulated plastic path length, not magnitude of the current displacement.
        // Opposite impacts cannot erase the work history or replenish damaged structure.
        public float PlasticTravelM;
        public float AbsorbedEnergyJ;
        public bool Broken;
    }

    [Serializable]
    public sealed class StructuralBeam
    {
        public int A, B;
        public float RestLengthM;
        public float YieldForceN;
        public float HardeningNPerM;
        public float YieldStrain;
        public float BreakStrain;
        public float Strain;
        public bool Yielded;
        public bool Broken;
    }

    public struct DamageResult
    {
        public float InputEnergyJ;
        public float AbsorbedEnergyJ;
        public float RemainingEnergyJ;
        public float MaximumDeformationM;
        public float Damage01;
    }

    /// <summary>
    /// Reduced-order quasistatic crash deformation. Each node is a plastic structural
    /// attachment with positive work resistance; connected beam orientation adds directional
    /// resistance and beam extension determines yielding/breakage. For a local displacement
    /// dx, work is (Fy+H*x)*dx + H*dx^2/2. The positive root spends at most the impact budget.
    ///
    /// It is not a time-resolved node-and-beam soft-body solver, fracture mechanics, or a
    /// replacement for the rigid-body collision solver. No forces or impulses are generated
    /// here: callers supply energy already allocated to permanent structural deformation.
    /// Restitution, rigid-body motion and heat must not allocate that energy a second time.
    /// </summary>
    public sealed class DamageModel
    {
        private readonly float[] weights;
        public StructuralNode[] Nodes { get; }
        public StructuralBeam[] Beams { get; }
        public float TotalAbsorbedEnergyJ { get; private set; }

        public float Damage01
        {
            get
            {
                float sum = 0f;
                for (int i = 0; i < Nodes.Length; i++) sum += Nodes[i].PlasticTravelM / Nodes[i].MaximumPlasticTravelM;
                return Nodes.Length > 0 ? Scalar.Clamp(sum / Nodes.Length, 0f, 1f) : 0f;
            }
        }
        public float MaximumDeformationM
        {
            get
            {
                float maximum = 0f;
                for (int i = 0; i < Nodes.Length; i++) maximum = Math.Max(maximum, SimVector.Distance(Nodes[i].Position, Nodes[i].OriginalPosition));
                return maximum;
            }
        }

        public DamageModel(StructuralNode[] nodes, StructuralBeam[] beams)
        {
            Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            Beams = beams ?? throw new ArgumentNullException(nameof(beams));
            if (nodes.Length == 0) throw new ArgumentException("At least one structural node is required.");
            weights = new float[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] == null) throw new ArgumentException("Null structural node.");
                nodes[i].OriginalPosition.Validate("node");
                Scalar.Range(nodes[i].YieldForceN, 1f, 10000000f, "YieldForceN");
                Scalar.Range(nodes[i].HardeningNPerM, 0f, 100000000f, "HardeningNPerM");
                Scalar.Range(nodes[i].MaximumPlasticTravelM, 0.0001f, 3f, "MaximumPlasticTravelM");
            }
            for (int i = 0; i < beams.Length; i++)
            {
                StructuralBeam beam = beams[i];
                if (beam == null || beam.A < 0 || beam.B < 0 || beam.A >= nodes.Length || beam.B >= nodes.Length || beam.A == beam.B)
                    throw new ArgumentException("Invalid structural beam endpoints.");
                Scalar.Range(beam.RestLengthM, 0.0001f, 30f, "RestLengthM");
                Scalar.Range(beam.YieldForceN, 0f, 10000000f, "YieldForceN");
                Scalar.Range(beam.HardeningNPerM, 0f, 100000000f, "HardeningNPerM");
                Scalar.Range(beam.YieldStrain, 0.0001f, 1f, "YieldStrain");
                Scalar.Range(beam.BreakStrain, beam.YieldStrain, 3f, "BreakStrain");
            }
            Reset();
        }

        /// <summary>Original 20-node body cage. Values are uncalibrated material targets.</summary>
        public static DamageModel CreateHatchback()
        {
            float[] stations = { -1.90f, -1.05f, 0f, 1.05f, 1.90f };
            var nodes = new StructuralNode[20];
            for (int z = 0; z < stations.Length; z++)
                for (int y = 0; y < 2; y++)
                    for (int x = 0; x < 2; x++)
                    {
                        bool crumpleZone = z == 0 || z == 4;
                        int index = z * 4 + y * 2 + x;
                        nodes[index] = new StructuralNode
                        {
                            OriginalPosition = new SimVector(x == 0 ? -0.78f : 0.78f, y == 0 ? 0.05f : 0.75f, stations[z]),
                            YieldForceN = crumpleZone ? 3000f : 9000f,
                            HardeningNPerM = crumpleZone ? 14000f : 35000f,
                            MaximumPlasticTravelM = crumpleZone ? 0.45f : 0.24f
                        };
                    }
            var beams = new List<StructuralBeam>(60);
            Action<int, int> connect = (a, b) => beams.Add(new StructuralBeam
            {
                A = a, B = b,
                RestLengthM = SimVector.Distance(nodes[a].OriginalPosition, nodes[b].OriginalPosition),
                YieldForceN = 2500f,
                HardeningNPerM = 16000f,
                YieldStrain = 0.025f,
                BreakStrain = 0.22f
            });
            for (int z = 0; z < 5; z++)
            {
                int p = z * 4;
                connect(p, p + 1); connect(p + 2, p + 3);
                connect(p, p + 2); connect(p + 1, p + 3);
                if (z < 4)
                {
                    for (int i = 0; i < 4; i++) connect(p + i, p + 4 + i);
                    connect(p, p + 7); connect(p + 1, p + 6);
                }
            }
            return new DamageModel(nodes, beams.ToArray());
        }

        public DamageResult ApplyImpact(SimVector localContactPoint, SimVector localImpulseDirection, float energyJ, float radiusM)
        {
            localContactPoint.Validate(nameof(localContactPoint));
            localImpulseDirection.Validate(nameof(localImpulseDirection));
            Scalar.Range(energyJ, 0f, 100000000f, nameof(energyJ));
            Scalar.Range(radiusM, 0.001f, 20f, nameof(radiusM));
            var result = new DamageResult { InputEnergyJ = energyJ, RemainingEnergyJ = energyJ };
            SimVector direction = localImpulseDirection.Normalized;
            if (energyJ <= 0f || direction.LengthSquared < 0.5f)
            {
                result.MaximumDeformationM = MaximumDeformationM;
                result.Damage01 = Damage01;
                return result;
            }
            float weightSum = 0f;
            for (int i = 0; i < Nodes.Length; i++)
            {
                float falloff = Math.Max(0f, 1f - SimVector.Distance(Nodes[i].Position, localContactPoint) / radiusM);
                weights[i] = Nodes[i].Broken ? 0f : falloff * falloff;
                weightSum += weights[i];
            }
            if (weightSum > 0f)
            {
                for (int i = 0; i < Nodes.Length; i++)
                {
                    if (weights[i] <= 0f) continue;
                    StructuralNode node = Nodes[i];
                    float budget = energyJ * weights[i] / weightSum;
                    float yieldForce = node.YieldForceN;
                    float hardening = node.HardeningNPerM;
                    for (int b = 0; b < Beams.Length; b++)
                    {
                        StructuralBeam beam = Beams[b];
                        if (beam.Broken || (beam.A != i && beam.B != i)) continue;
                        SimVector axis = (Nodes[beam.B].Position - Nodes[beam.A].Position).Normalized;
                        float alignment = Math.Abs(SimVector.Dot(axis, direction));
                        // Axially loaded neighbors resist more than transverse members.
                        yieldForce += beam.YieldForceN * alignment * 0.25f;
                        hardening += beam.HardeningNPerM * alignment * alignment * 0.25f;
                    }
                    float startingResistance = yieldForce + hardening * node.PlasticTravelM;
                    float travel;
                    if (hardening > 0f)
                        travel = 2f * budget / (startingResistance + (float)Math.Sqrt(startingResistance * startingResistance + 2f * hardening * budget));
                    else travel = budget / startingResistance;
                    travel = Math.Min(travel, Math.Max(0f, node.MaximumPlasticTravelM - node.PlasticTravelM));
                    float spent = Math.Min(budget, startingResistance * travel + 0.5f * hardening * travel * travel);
                    node.PlasticTravelM += travel;
                    node.Position += direction * travel;
                    node.AbsorbedEnergyJ += spent;
                    if (node.PlasticTravelM >= node.MaximumPlasticTravelM - 0.000001f) node.Broken = true;
                    result.AbsorbedEnergyJ += spent;
                }
            }
            for (int b = 0; b < Beams.Length; b++)
            {
                StructuralBeam beam = Beams[b];
                float length = SimVector.Distance(Nodes[beam.A].Position, Nodes[beam.B].Position);
                beam.Strain = (length - beam.RestLengthM) / beam.RestLengthM;
                beam.Yielded |= Math.Abs(beam.Strain) >= beam.YieldStrain;
                beam.Broken |= Math.Abs(beam.Strain) >= beam.BreakStrain || Nodes[beam.A].Broken || Nodes[beam.B].Broken;
            }
            // Floating-point accumulation may exceed the budget by a few ulps.
            result.AbsorbedEnergyJ = Math.Min(energyJ, result.AbsorbedEnergyJ);
            result.RemainingEnergyJ = energyJ - result.AbsorbedEnergyJ;
            TotalAbsorbedEnergyJ += result.AbsorbedEnergyJ;
            result.MaximumDeformationM = MaximumDeformationM;
            result.Damage01 = Damage01;
            return result;
        }

        public void Reset()
        {
            TotalAbsorbedEnergyJ = 0f;
            for (int i = 0; i < Nodes.Length; i++)
            {
                Nodes[i].Position = Nodes[i].OriginalPosition;
                Nodes[i].PlasticTravelM = 0f;
                Nodes[i].AbsorbedEnergyJ = 0f;
                Nodes[i].Broken = false;
            }
            for (int i = 0; i < Beams.Length; i++)
            {
                Beams[i].Strain = 0f;
                Beams[i].Yielded = false;
                Beams[i].Broken = false;
            }
        }
    }
}
