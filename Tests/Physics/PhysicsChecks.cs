using ImpactDrive.Physics;

namespace ImpactDrive.HeadlessTests;

internal static class PhysicsChecks
{
    public static void Register(TestSuite tests)
    {
        RegisterConfiguration(tests);
        RegisterSteering(tests);
        RegisterTires(tests);
        RegisterSuspension(tests);
        RegisterPowertrain(tests);
        RegisterDamage(tests);
    }

    private static void RegisterConfiguration(TestSuite tests)
    {
        tests.Run("configuration/default vehicle has valid finite SI parameters", () => VehicleSpec.CreateAster().Validate());
        tests.Run("configuration/nonphysical mass and NaN values are rejected", () =>
        {
            var spec = VehicleSpec.CreateAster();
            spec.MassKg = -100;
            Check.Throws<ArgumentException>(spec.Validate, "negative mass must be rejected");
            spec.MassKg = float.NaN;
            Check.Throws<ArgumentException>(spec.Validate, "NaN mass must be rejected");
        });
        tests.Run("configuration/torque curve must be strictly ordered", () =>
        {
            var spec = VehicleSpec.CreateAster();
            spec.TorqueCurveRpm[2] = spec.TorqueCurveRpm[1];
            Check.Throws<ArgumentException>(spec.Validate, "duplicate RPM knots make interpolation ambiguous");
        });
        tests.Run("configuration/independent defaults do not share mutable tuning", () =>
        {
            var first = VehicleSpec.CreateAster();
            var second = VehicleSpec.CreateAster();
            float secondRatio = second.ForwardGearRatios[0];
            first.ForwardGearRatios[0] *= 2;
            first.Tire.FrictionCoefficient *= 0.5f;
            Check.Near(second.ForwardGearRatios[0], secondRatio, 1e-6, "gear data must not alias between vehicles");
            Check.Near(second.Tire.FrictionCoefficient, TireParameters.Road().FrictionCoefficient, 1e-6, "tire data must not alias between vehicles");
        });
    }

    private static void RegisterSteering(TestSuite tests)
    {
        tests.Run("steering/straight ahead produces zero wheel angles", () =>
        {
            var angles = SteeringModel.Ackermann(0, 2.6f, 1.52f);
            Check.Near(angles.LeftRad, 0, 1e-7, "left angle");
            Check.Near(angles.RightRad, 0, 1e-7, "right angle");
        });
        tests.Run("steering/right wheel is inside a right turn", () =>
        {
            var angles = SteeringModel.Ackermann(0.4f, 2.6f, 1.52f);
            Check.True(angles.RightRad > angles.LeftRad && angles.LeftRad > 0, "positive steering must turn both wheels right, with larger right angle");
        });
        tests.Run("steering/left and right turns are mirrored", () =>
        {
            var right = SteeringModel.Ackermann(0.4f, 2.6f, 1.52f);
            var left = SteeringModel.Ackermann(-0.4f, 2.6f, 1.52f);
            Check.Near(right.LeftRad, -left.RightRad, 1e-6, "outer angle symmetry");
            Check.Near(right.RightRad, -left.LeftRad, 1e-6, "inner angle symmetry");
        });
        tests.Run("steering/Ackermann wheel axes share the same turn centre", () =>
        {
            foreach (float steering in new[] { 0.05f, 0.2f, 0.5f })
            {
                var angles = SteeringModel.Ackermann(steering, 2.6f, 1.52f);
                double outerRadius = 2.6 / Math.Tan(angles.LeftRad) - 1.52 / 2;
                double innerRadius = 2.6 / Math.Tan(angles.RightRad) + 1.52 / 2;
                Check.Near(outerRadius, innerRadius, 1e-4, "wheel axes must intersect at one turning centre");
                Check.Near(innerRadius, 2.6 / Math.Tan(steering), 1e-4, "steering rack centre angle determines turn radius");
            }
        });
    }

    private static TireForces Settled(float vx, float vy, float omega, float load = 3500,
        SurfaceParameters? surfaceOverride = null)
    {
        var state = new TireState();
        var tire = TireParameters.Road();
        var surface = surfaceOverride ?? SurfaceParameters.DryAsphalt;
        TireForces forces = default;
        for (int i = 0; i < 400; i++)
            forces = TireModel.Evaluate(ref state, vx, vy, omega, load, 0.31f, tire, surface, 0.01f);
        return forces;
    }

    private static void RegisterTires(TestSuite tests)
    {
        tests.Run("tires/free rolling produces no propulsive or lateral force", () =>
        {
            var f = Settled(20, 0, 20 / 0.31f);
            Check.Near(f.LongitudinalN, 0, 0.01, "free rolling longitudinal force");
            Check.Near(f.LateralN, 0, 0.01, "free rolling lateral force");
        });
        tests.Run("tires/stationary contact does not create energy", () =>
        {
            var f = Settled(0, 0, 0);
            Check.Near(f.LongitudinalN, 0, 1e-6, "stationary longitudinal force");
            Check.Near(f.LateralN, 0, 1e-6, "stationary lateral force");
        });
        tests.Run("tires/wheelspin drives forward and reverse with correct sign", () =>
        {
            Check.True(Settled(10, 0, 12 / 0.31f).LongitudinalN > 0, "forward wheelspin must drive forward");
            Check.True(Settled(-10, 0, -12 / 0.31f).LongitudinalN < 0, "reverse wheelspin must drive backwards");
        });
        tests.Run("tires/locked wheel dissipates forward and reverse translation", () =>
        {
            Check.True(Settled(10, 0, 0).LongitudinalN < 0, "locked wheel opposes forward velocity");
            Check.True(Settled(-10, 0, 0).LongitudinalN > 0, "locked wheel opposes reverse velocity");
        });
        tests.Run("tires/lateral force opposes sideslip in either travel direction", () =>
        {
            foreach (float vx in new[] { -20f, 0f, 20f })
            foreach (float vy in new[] { -2f, 2f })
            {
                var f = Settled(vx, vy, vx / 0.31f);
                Check.True(f.LateralN * vy < 0, $"lateral contact must dissipate lateral motion at vx={vx}, vy={vy}");
            }
        });
        tests.Run("tires/combined grip circle holds over load and slip sweep", () =>
        {
            foreach (var surface in new[] { SurfaceParameters.DryAsphalt, SurfaceParameters.WetAsphalt, SurfaceParameters.Gravel, SurfaceParameters.Ice })
            foreach (float load in new[] { 100f, 1000f, 3500f, 9000f })
            foreach (float vx in new[] { -30f, -0.1f, 0f, 0.1f, 30f })
            foreach (float vy in new[] { -8f, 0f, 8f })
            foreach (float omega in new[] { -150f, 0f, 150f })
            {
                var f = Settled(vx, vy, omega, load, surface);
                double magnitude = Math.Sqrt((double)f.LongitudinalN * f.LongitudinalN + (double)f.LateralN * f.LateralN);
                Check.Between(f.GripLimitN, 0, 4 * load, "finite bounded grip coefficient");
                Check.Between(magnitude, 0, f.GripLimitN * 1.00001 + 0.001, "combined contact force cannot exceed grip limit");
            }
        });
        tests.Run("tires/load sensitivity reduces coefficient as wheel load increases", () =>
        {
            var light = Settled(20, 5, 180, 2000);
            var heavy = Settled(20, 5, 180, 4000);
            Check.True(heavy.GripLimitN > light.GripLimitN, "more vertical load must increase absolute available grip");
            Check.True(heavy.GripLimitN < 2 * light.GripLimitN, "doubling load must yield less than twice grip");
        });
        tests.Run("tires/dry wet and ice have ordered traction limits", () =>
        {
            var dry = Settled(20, 3, 180, 3500, SurfaceParameters.DryAsphalt);
            var wet = Settled(20, 3, 180, 3500, SurfaceParameters.WetAsphalt);
            var ice = Settled(20, 3, 180, 3500, SurfaceParameters.Ice);
            Check.True(dry.GripLimitN > wet.GripLimitN && wet.GripLimitN > ice.GripLimitN, "dry > wet > ice grip");
            Check.True(ice.GripLimitN < dry.GripLimitN * 0.3f, "ice must produce materially lower traction");
        });
        tests.Run("tires/unloaded wheel clears contact force and relaxation history", () =>
        {
            var state = new TireState();
            for (int i = 0; i < 100; i++)
                TireModel.Evaluate(ref state, 20, 4, 150, 3500, 0.31f, TireParameters.Road(), SurfaceParameters.DryAsphalt, 0.01f);
            var lifted = TireModel.Evaluate(ref state, 20, 4, 150, 0, 0.31f, TireParameters.Road(), SurfaceParameters.DryAsphalt, 0.01f);
            Check.Near(lifted.LongitudinalN, 0, 1e-6, "unloaded longitudinal force");
            Check.Near(lifted.LateralN, 0, 1e-6, "unloaded lateral force");
            var landed = TireModel.Evaluate(ref state, 20, 0, 20 / 0.31f, 3500, 0.31f, TireParameters.Road(), SurfaceParameters.DryAsphalt, 0.01f);
            Check.Near(landed.LongitudinalN, 0, 0.01, "stale longitudinal force must not survive contact loss");
            Check.Near(landed.LateralN, 0, 0.01, "stale lateral force must not survive contact loss");
        });
        tests.Run("tires/relaxation is independent of step partition for constant contact", () =>
        {
            var wholeState = new TireState();
            var splitState = new TireState();
            var tire = TireParameters.Road();
            var whole = TireModel.Evaluate(ref wholeState, 20, 2, 70, 3500, 0.31f, tire, SurfaceParameters.DryAsphalt, 0.02f);
            TireForces split = default;
            for (int i = 0; i < 4; i++) split = TireModel.Evaluate(ref splitState, 20, 2, 70, 3500, 0.31f, tire, SurfaceParameters.DryAsphalt, 0.005f);
            Check.Near(split.LongitudinalN, whole.LongitudinalN, 0.1, "exponential longitudinal relaxation");
            Check.Near(split.LateralN, whole.LateralN, 0.1, "exponential lateral relaxation");
        });
        tests.Run("tires/nonpositive integration step is rejected", () =>
        {
            var state = new TireState();
            Check.Throws<ArgumentException>(() => TireModel.Evaluate(ref state, 20, 0, 60, 3500, 0.31f, TireParameters.Road(), SurfaceParameters.DryAsphalt, 0), "zero dt must not silently modify tire state");
        });
    }

    private static void RegisterSuspension(TestSuite tests)
    {
        tests.Run("suspension/static sag supports configured axle weight", () =>
        {
            var spec = VehicleSpec.CreateAster();
            float front = SuspensionModel.Evaluate(spec.StaticSagM(true), 0, spec);
            float rear = SuspensionModel.Evaluate(spec.StaticSagM(false), 0, spec);
            Check.Near(2 * front, spec.MassKg * 9.80665 * spec.FrontWeightFraction, 0.1, "front axle static support");
            Check.Near(2 * rear, spec.MassKg * 9.80665 * (1 - spec.FrontWeightFraction), 0.1, "rear axle static support");
            Check.True(spec.StaticSagM(true) < spec.SuspensionTravelM * 0.85f, "default suspension must settle before bump stop");
        });
        tests.Run("suspension/bump stop increases tangent stiffness near travel limit", () =>
        {
            var spec = VehicleSpec.CreateAster();
            float below = spec.SuspensionTravelM * 0.7f;
            float above = spec.SuspensionTravelM * 0.95f;
            float regularSlope = (SuspensionModel.Evaluate(below + 0.001f, 0, spec) - SuspensionModel.Evaluate(below, 0, spec)) / 0.001f;
            float bumpSlope = (SuspensionModel.Evaluate(above + 0.001f, 0, spec) - SuspensionModel.Evaluate(above, 0, spec)) / 0.001f;
            Check.True(bumpSlope > regularSlope * 2, "bump-stop support must stiffen before travel is exhausted");
        });
        tests.Run("suspension/no compression or velocity supplies no support", () =>
            Check.Near(SuspensionModel.Evaluate(0, 0, VehicleSpec.CreateAster()), 0, 1e-6, "zero spring deflection"));
        tests.Run("suspension/increasing compression increases static support", () =>
        {
            var spec = VehicleSpec.CreateAster();
            float low = SuspensionModel.Evaluate(0.02f, 0, spec);
            float high = SuspensionModel.Evaluate(0.04f, 0, spec);
            Check.True(low > 0 && high > low, "spring must oppose increasing compression");
            Check.Near(high, 2 * low, 0.1, "linear working travel follows Hooke's law");
        });
        tests.Run("suspension/damping opposes compression and rebound motion", () =>
        {
            var spec = VehicleSpec.CreateAster();
            float staticForce = SuspensionModel.Evaluate(0.08f, 0, spec);
            float compression = SuspensionModel.Evaluate(0.08f, 0.1f, spec);
            float rebound = SuspensionModel.Evaluate(0.08f, -0.1f, spec);
            Check.True(compression > staticForce, "compression damping increases support force");
            Check.True(rebound < staticForce, "rebound damping decreases support force");
            Check.True(rebound >= 0, "ground contact cannot pull the vehicle down");
        });
        tests.Run("suspension/rapid rebound cannot produce tensile contact", () =>
            Check.Near(SuspensionModel.Evaluate(0.01f, -100, VehicleSpec.CreateAster()), 0, 1e-6, "unilateral contact force"));
        tests.Run("suspension/antiroll force is antisymmetric and absent in heave", () =>
        {
            Check.Near(SuspensionModel.AntiRollForce(0.08f, 0.08f, 12000), 0, 1e-6, "equal wheel displacement must not activate antiroll");
            float left = SuspensionModel.AntiRollForce(0.12f, 0.06f, 12000);
            float right = SuspensionModel.AntiRollForce(0.06f, 0.12f, 12000);
            Check.Near(left, -right, 1e-5, "opposite wheel displacements exchange equal antiroll forces");
            Check.True(left > 0, "positive left compression difference has correct sign");
        });
    }

    private static void RegisterPowertrain(TestSuite tests)
    {
        tests.Run("powertrain/torque curve interpolates its engineering data", () =>
        {
            var spec = VehicleSpec.CreateAster();
            var engine = new Powertrain(spec);
            for (int i = 0; i < spec.TorqueCurveRpm.Length; i++)
                Check.Near(engine.TorqueAtRpm(spec.TorqueCurveRpm[i]), spec.TorqueCurveNm[i], 0.0001, "torque at table knot");
            float midpoint = (spec.TorqueCurveRpm[1] + spec.TorqueCurveRpm[2]) / 2;
            Check.Near(engine.TorqueAtRpm(midpoint), (spec.TorqueCurveNm[1] + spec.TorqueCurveNm[2]) / 2, 0.0001, "linear interpolation between measured/design knots");
        });
        tests.Run("powertrain/gear change interrupts torque for shift duration", () =>
        {
            var spec = VehicleSpec.CreateAster();
            var engine = new Powertrain(spec) { Automatic = false };
            for (int i = 0; i < 100; i++) engine.Step(0.5f, 20, 0.01f);
            engine.SetGear(2);
            engine.Step(0.5f, 20, 0.01f);
            Check.True(engine.IsShifting, "shift should remain active during configured interruption");
            Check.Near(engine.OutputTorqueNm, 0, 1e-6, "open clutch during shift");
            for (int i = 0; i < 200; i++) engine.Step(0.5f, 20, 0.01f);
            Check.True(!engine.IsShifting && engine.OutputTorqueNm > 0, "torque must resume when shift completes");
        });
        tests.Run("powertrain/gearbox losses cannot add transmitted power", () =>
        {
            var spec = VehicleSpec.CreateAster();
            foreach (int gear in new[] { -1, 1, 3, 6 })
            foreach (float throttle in new[] { 0f, 0.5f, 1f })
            foreach (float wheelOmega in new[] { -100f, -10f, 10f, 100f })
            {
                var engine = new Powertrain(spec) { Automatic = false };
                engine.SetGear(gear);
                float ratio = (gear == -1 ? spec.ReverseGearRatio : spec.ForwardGearRatios[gear - 1]) * spec.FinalDriveRatio;
                for (int i = 0; i < 200; i++)
                {
                    engine.Step(throttle, wheelOmega, 0.01f);
                    double axlePower = engine.OutputTorqueNm * wheelOmega;
                    double beforeLossPower = engine.ClutchTorqueNm * ratio * wheelOmega;
                    Check.True(axlePower <= beforeLossPower + 0.1, "gearbox must dissipate power in drive and backdrive directions");
                    Check.Between(Math.Abs(engine.ClutchTorqueNm), 0, spec.ClutchCapacityNm + 0.001, "clutch torque limit");
                }
            }
        });
        tests.Run("powertrain/out-of-range gears are rejected", () =>
        {
            var engine = new Powertrain(VehicleSpec.CreateAster());
            Check.Throws<ArgumentOutOfRangeException>(() => engine.SetGear(-2), "invalid reverse gear");
            Check.Throws<ArgumentOutOfRangeException>(() => engine.SetGear(20), "gear beyond transmission ratios");
        });
        tests.Run("powertrain/neutral disconnects wheel torque", () =>
        {
            var engine = new Powertrain(VehicleSpec.CreateAster()) { Automatic = false };
            engine.SetGear(0);
            for (int i = 0; i < 200; i++) engine.Step(1, 50, 0.01f);
            Check.Near(engine.OutputTorqueNm, 0, 1e-6, "neutral must not propel the vehicle");
            Check.Between(engine.EngineRpm, 500, 12000, "running neutral engine rpm must remain finite and plausible");
        });
        tests.Run("powertrain/forward and reverse gears deliver opposite torque", () =>
        {
            var forward = new Powertrain(VehicleSpec.CreateAster()) { Automatic = false };
            var reverse = new Powertrain(VehicleSpec.CreateAster()) { Automatic = false };
            forward.SetGear(1);
            reverse.SetGear(-1);
            for (int i = 0; i < 300; i++)
            {
                forward.Step(0.5f, 20, 0.01f);
                reverse.Step(0.5f, -20, 0.01f);
            }
            Check.True(forward.OutputTorqueNm > 0, "forward gear must transmit positive drive torque");
            Check.True(reverse.OutputTorqueNm < 0, "reverse gear must transmit negative drive torque");
            Check.True(reverse.Gear == -1, "reverse cannot auto-select a forward gear");
        });
        tests.Run("powertrain/switched off engine provides no drive torque", () =>
        {
            var engine = new Powertrain(VehicleSpec.CreateAster()) { Automatic = false };
            engine.SetGear(1);
            for (int i = 0; i < 100; i++) engine.Step(1, 20, 0.01f);
            engine.EngineRunning = false;
            engine.Step(1, 20, 0.01f);
            Check.Near(engine.OutputTorqueNm, 0, 1e-6, "off engine cannot deliver drive torque");
            Check.Near(engine.EngineRpm, 0, 1e-6, "off engine rpm according to simplified stop model");
        });
        tests.Run("powertrain/destroyed engine cannot provide normal power", () =>
        {
            var healthy = new Powertrain(VehicleSpec.CreateAster()) { Automatic = false };
            var destroyed = new Powertrain(VehicleSpec.CreateAster()) { Automatic = false, EngineHealth01 = 0 };
            healthy.SetGear(1); destroyed.SetGear(1);
            for (int i = 0; i < 200; i++) { healthy.Step(0.5f, 20, 0.01f); destroyed.Step(0.5f, 20, 0.01f); }
            Check.True(healthy.OutputTorqueNm > 0, "healthy engine reference torque");
            Check.True(destroyed.OutputTorqueNm <= 0, "zero-health engine cannot generate positive drive torque");
        });
    }

    private static void RegisterDamage(TestSuite tests)
    {
        tests.Run("damage/zero-energy contact causes no deformation", () =>
        {
            var model = DamageModel.CreateHatchback();
            var result = model.ApplyImpact(new SimVector(0, 0, 2), new SimVector(0, 0, -1), 0, 1);
            Check.Near(result.AbsorbedEnergyJ, 0, 1e-6, "no work available");
            Check.Near(result.MaximumDeformationM, 0, 1e-6, "zero energy cannot deform material");
        });
        tests.Run("damage/plastic work never exceeds available impact energy", () =>
        {
            foreach (float energy in new[] { 10f, 1000f, 10000f, 1000000f })
            {
                var model = DamageModel.CreateHatchback();
                var result = model.ApplyImpact(new SimVector(0, 0, 2), new SimVector(0, 0, -1), energy, 1.5f);
                Check.Between(result.AbsorbedEnergyJ, 0, energy + 0.1, "absorbed plastic work");
                Check.Between(result.RemainingEnergyJ, 0, energy + 0.1, "unused energy");
                Check.Near(result.AbsorbedEnergyJ + result.RemainingEnergyJ, energy, Math.Max(0.01, energy * 1e-5), "energy ledger balances");
                Check.Between(result.Damage01, 0, 1, "normalized damage bounds");
            }
        });
        tests.Run("damage/larger impact produces at least as much plastic work", () =>
        {
            var small = DamageModel.CreateHatchback().ApplyImpact(new SimVector(0, 0, 2), new SimVector(0, 0, -1), 1000, 1.5f);
            var large = DamageModel.CreateHatchback().ApplyImpact(new SimVector(0, 0, 2), new SimVector(0, 0, -1), 50000, 1.5f);
            Check.True(large.AbsorbedEnergyJ >= small.AbsorbedEnergyJ, "impact severity cannot reduce plastic work from identical initial states");
            Check.True(large.MaximumDeformationM >= small.MaximumDeformationM, "larger energy cannot reduce maximum initial deformation");
            Check.True(large.AbsorbedEnergyJ > 0, "test impact must actually reach structural nodes");
        });
        tests.Run("damage/distant contact does not deform unrelated body structure", () =>
        {
            var result = DamageModel.CreateHatchback().ApplyImpact(new SimVector(100, 0, 100), new SimVector(0, 0, -1), 50000, 0.5f);
            Check.Near(result.AbsorbedEnergyJ, 0, 1e-6, "local damage cannot reach distant nodes");
            Check.Near(result.RemainingEnergyJ, 50000, 0.01, "unabsorbed energy is returned");
        });
        tests.Run("damage/repeated impacts cannot exceed node deformation bounds", () =>
        {
            var model = DamageModel.CreateHatchback();
            for (int i = 0; i < 100; i++)
            {
                var result = model.ApplyImpact(new SimVector(0, 0, 2), new SimVector(0, 0, -1), 100000, 2);
                Check.Between(result.MaximumDeformationM, 0, 0.451, "body cannot collapse beyond crumple travel");
                foreach (var node in model.Nodes)
                {
                    Check.Between(node.PlasticTravelM, 0, node.MaximumPlasticTravelM + 0.00001, "node plastic path must respect its own configured limit");
                    Check.Between(SimVector.Distance(node.Position, node.OriginalPosition), 0, node.MaximumPlasticTravelM + 0.00001, "node displacement must respect its own configured limit");
                }
                Check.Between(result.AbsorbedEnergyJ, 0, 100000.1, "each impact respects its energy budget");
            }
        });
        tests.Run("damage/opposite impacts cannot erase plastic work or repair nodes", () =>
        {
            var model = DamageModel.CreateHatchback();
            model.ApplyImpact(new SimVector(0, 0.4f, 1.9f), new SimVector(0, 0, -1), 2500, 1.5f);
            float previousWork = model.TotalAbsorbedEnergyJ;
            float[] previousTravel = model.Nodes.Select(n => n.PlasticTravelM).ToArray();
            model.ApplyImpact(new SimVector(0, 0.4f, 1.9f), new SimVector(0, 0, 1), 2500, 1.5f);
            Check.True(model.TotalAbsorbedEnergyJ >= previousWork, "reversed impact cannot refund plastic work");
            for (int i = 0; i < model.Nodes.Length; i++)
                Check.True(model.Nodes[i].PlasticTravelM >= previousTravel[i], "reversed impact cannot undo accumulated plastic travel");
        });
        tests.Run("damage/reset restores pristine structural state", () =>
        {
            var model = DamageModel.CreateHatchback();
            model.ApplyImpact(new SimVector(0, 0, 2), new SimVector(0, 0, -1), 100000, 1.5f);
            Check.True(model.TotalAbsorbedEnergyJ > 0, "setup impact must deform the structure");
            model.Reset();
            Check.Near(model.TotalAbsorbedEnergyJ, 0, 1e-6, "repair clears accumulated plastic work");
            foreach (var node in model.Nodes)
            {
                Check.Near(node.Position.X, node.OriginalPosition.X, 1e-6, "node x restored");
                Check.Near(node.Position.Y, node.OriginalPosition.Y, 1e-6, "node y restored");
                Check.Near(node.Position.Z, node.OriginalPosition.Z, 1e-6, "node z restored");
                Check.Near(node.PlasticTravelM, 0, 1e-6, "node plastic history cleared");
                Check.True(!node.Broken, "node fracture cleared");
            }
            foreach (var beam in model.Beams) Check.True(!beam.Broken, "beam fracture cleared");
        });
    }
}
