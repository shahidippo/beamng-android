using ImpactDrive.Physics;
using NUnit.Framework;

namespace ImpactDrive.Tests
{
    /// <summary>Ensures the shared model also compiles and executes under Unity's runtime.</summary>
    public sealed class CoreCompatibilityTests
    {
        [Test]
        public void AckermannGeometryMatchesTurnCentre()
        {
            var wheel = SteeringModel.Ackermann(0.4f, 2.6f, 1.52f);
            double outer = 2.6 / System.Math.Tan(wheel.LeftRad) - 1.52 / 2;
            double inner = 2.6 / System.Math.Tan(wheel.RightRad) + 1.52 / 2;
            Assert.That(outer, Is.EqualTo(inner).Within(0.0001));
        }

        [Test]
        public void TireCombinedForceRespectsGripBudget()
        {
            var tireState = new TireState();
            var tire = TireParameters.Road();
            for (int i = 0; i < 200; i++)
            {
                var force = TireModel.Evaluate(ref tireState, 20, 4, 150, 3500, 0.31f, tire, SurfaceParameters.DryAsphalt, 0.01f);
                double magnitude = System.Math.Sqrt((double)force.LongitudinalN * force.LongitudinalN + (double)force.LateralN * force.LateralN);
                Assert.That(magnitude, Is.LessThanOrEqualTo(force.GripLimitN + 0.01));
            }
        }

        [Test]
        public void DamageWorkBalancesImpactEnergy()
        {
            var model = DamageModel.CreateHatchback();
            var result = model.ApplyImpact(new SimVector(0, 0, 2), new SimVector(0, 0, -1), 50000, 1.5f);
            Assert.That(result.AbsorbedEnergyJ, Is.GreaterThan(0));
            Assert.That(result.AbsorbedEnergyJ + result.RemainingEnergyJ, Is.EqualTo(50000).Within(0.1));
        }
    }
}
