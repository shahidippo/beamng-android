using System;
using ImpactDrive.Physics;
using UnityEngine;
using UnityPhysics = UnityEngine.Physics;

namespace ImpactDrive.Runtime
{
    /// <summary>
    /// Four independent force-based tire contacts driving a Unity rigid body. The chassis
    /// is never rotated or translated for steering. This reduced-order contact model is
    /// a development foundation; unsprung-body dynamics and multi-link kinematics are not modeled.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleController : MonoBehaviour
    {
        public DriverInput Input;
        public bool AbsEnabled = true;
        public bool TractionControlEnabled = true;
        public bool SteeringAssistEnabled = true;
        [Range(.25f, 2f)] public float SteeringSensitivity = 1f;
        [Range(4, 32)] public int WheelSubsteps = 16;
        public LayerMask GroundLayers = ~0;

        public VehicleSpec Specification { get; private set; }
        public Rigidbody Body { get; private set; }
        public WheelTelemetry[] Wheels { get; private set; } = new WheelTelemetry[4];
        public float SpeedKph => Body == null ? 0f : Body.linearVelocity.magnitude * 3.6f;
        public float SignedSpeedMps => Body == null ? 0f : Vector3.Dot(Body.linearVelocity, transform.forward);
        public float EngineRpm => powertrain == null ? 0f : powertrain.EngineRpm;
        public int CurrentGear => powertrain == null ? 0 : powertrain.Gear;
        public bool AutomaticTransmission => powertrain == null || powertrain.Automatic;
        public bool EngineRunning => powertrain != null && powertrain.EngineRunning;
        public float EngineHealth { get; internal set; } = 1f;
        public bool TractionControlActive { get; private set; }
        public float LastImpactSpeedMps { get; internal set; }
        public float LastImpactEnergyJ { get; internal set; }
        public float Damage01 => damage == null ? 0f : damage.Damage01;

        private sealed class WheelRuntime
        {
            internal Transform Visual;
            internal Quaternion NeutralRotation;
            internal Vector3 NeutralHub;
            internal Vector3 Anchor;
            internal float AngularVelocity;
            internal float SpinAngle;
            internal float PreviousCompression;
            internal bool WasGrounded;
            internal float AlignmentDamage;
            internal float BrakeTemperature = 20f;
            internal float AbsPressure = 1f;
            internal TireState TireState;
            internal Vector3 ContactPoint;
            internal Vector3 ContactNormal;
            internal Vector3 Forward;
            internal Vector3 Right;
            internal float Load;
            internal float AxisLoad;
            internal float NormalProjection = 1f;
            internal float Compression;
            internal float Length;
            internal float Steer;
            internal float LongitudinalVelocity;
            internal float LateralVelocity;
            internal Rigidbody ContactBody;
            internal SurfaceParameters Surface;
            internal DriveSurface SurfaceId;
            internal bool Grounded;
            internal Vector3 PreviousVisualPosition;
            internal Vector3 NextVisualPosition;
            internal Quaternion PreviousVisualRotation;
            internal Quaternion NextVisualRotation;
        }

        private static readonly SurfaceParameters DefaultSurface = SurfaceParameters.DryAsphalt;
        private readonly WheelRuntime[] wheel = new WheelRuntime[4];
        private readonly RaycastHit[] contactBuffer = new RaycastHit[32];
        private Powertrain powertrain;
        private VehicleDamage damage;
        private float rackAngle;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private bool initialized;
        private bool spawnSet;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            if (!initialized) Initialize(VehicleSpec.CreateAster(), null);
        }

        /// <param name="wheelVisuals">Wheel hubs ordered front-left, front-right, rear-left, rear-right. Spin axis is local X.</param>
        public void Initialize(VehicleSpec spec, Transform[] wheelVisuals)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (wheelVisuals != null && wheelVisuals.Length != 4)
                throw new ArgumentException("Exactly four wheel hubs are required (FL, FR, RL, RR).", nameof(wheelVisuals));
            spec.Validate();
            Specification = spec;
            Body = GetComponent<Rigidbody>();
            Body.mass = spec.MassKg;
            Body.linearDamping = 0f;
            Body.angularDamping = 0f;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.solverIterations = 12;
            Body.solverVelocityIterations = 8;
            Body.maxAngularVelocity = 20f;
            if (GetComponentsInChildren<Collider>().Length == 0)
            {
                var bodyCollider = gameObject.AddComponent<BoxCollider>();
                bodyCollider.center = new Vector3(0f, .35f, 0f);
                bodyCollider.size = new Vector3(1.65f, .55f, 3.9f);
            }
            float meanHubY = 0f;
            for (int i = 0; i < 4; ++i)
            {
                float track = i < 2 ? spec.FrontTrackM : spec.RearTrackM;
                var visual = wheelVisuals == null ? null : wheelVisuals[i];
                var neutral = visual == null
                    ? new Vector3((i % 2 == 0 ? -.5f : .5f) * track, -.15f, (i < 2 ? .5f : -.5f) * spec.WheelbaseM)
                    : transform.InverseTransformPoint(visual.position);
                wheel[i] = new WheelRuntime
                {
                    Visual = visual,
                    NeutralHub = neutral,
                    NeutralRotation = visual == null ? Quaternion.identity : visual.localRotation,
                    Anchor = neutral + Vector3.up * spec.SuspensionRestLengthM,
                    Length = spec.SuspensionRestLengthM,
                    Surface = DefaultSurface,
                    PreviousVisualPosition = neutral,
                    NextVisualPosition = neutral,
                    PreviousVisualRotation = visual == null ? Quaternion.identity : visual.localRotation,
                    NextVisualRotation = visual == null ? Quaternion.identity : visual.localRotation
                };
                meanHubY += neutral.y * .25f;
            }
            float nominalSag = spec.MassKg * UnityPhysics.gravity.magnitude / (4f * spec.SpringRateNPerM);
            float nominalRootHeight = spec.WheelRadiusM - meanHubY - nominalSag;
            Body.centerOfMass = new Vector3(0f, spec.CenterOfMassHeightM - nominalRootHeight,
                (spec.FrontWeightFraction - .5f) * spec.WheelbaseM);
            // Uniform-box estimate pending measured moments for the original vehicle. Units kg m².
            const float width = 1.7f, height = 1.1f, length = 4.05f;
            Body.inertiaTensorRotation = Quaternion.identity;
            Body.inertiaTensor = spec.MassKg / 12f * new Vector3(height * height + length * length,
                width * width + length * length, width * width + height * height);
            powertrain = new Powertrain(spec);
            EngineHealth = 1f;
            damage = GetComponent<VehicleDamage>();
            if (damage == null) damage = gameObject.AddComponent<VehicleDamage>();
            damage.Initialize(this, wheelVisuals);
            if (!spawnSet) SetSpawnPose(transform.position, transform.rotation);
            initialized = true;
        }

        public void SetSpawnPose(Vector3 position, Quaternion rotation)
        {
            spawnPosition = position;
            spawnRotation = rotation;
            spawnSet = true;
        }

        public bool SetGear(int gear)
        {
            if (powertrain == null) return false;
            // Direction selectors must not engage a gear against significant road speed.
            // Neutral remains available at any speed. Same-direction shifts use the clutch.
            if ((gear < 0 && SignedSpeedMps > 1f) || (gear > 0 && SignedSpeedMps < -1f)) return false;
            powertrain.SetGear(gear);
            return true;
        }
        public void ShiftUp()
        {
            if (powertrain != null) SetGear(Mathf.Min(Specification.ForwardGearRatios.Length, powertrain.Gear + 1));
        }
        public void ShiftDown()
        {
            if (powertrain != null) SetGear(Mathf.Max(-1, powertrain.Gear - 1));
        }
        public void ToggleTransmission() { if (powertrain != null) powertrain.Automatic = !powertrain.Automatic; }
        public void ToggleEngine() { if (powertrain != null) powertrain.EngineRunning = !powertrain.EngineRunning; }

        public void ResetVehicle()
        {
            if (!initialized) return;
            Input = default;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.position = spawnPosition;
            Body.rotation = spawnRotation;
            rackAngle = 0f;
            for (int i = 0; i < 4; ++i)
            {
                wheel[i].AngularVelocity = 0f;
                wheel[i].TireState = default;
                wheel[i].WasGrounded = false;
            }
            UnityPhysics.SyncTransforms();
            Body.WakeUp();
        }

        public void RepairVehicle()
        {
            bool engineWasDestroyed = EngineHealth <= 0f;
            EngineHealth = 1f;
            LastImpactEnergyJ = 0f;
            LastImpactSpeedMps = 0f;
            if (damage != null) damage.Repair();
            if (powertrain != null)
            {
                powertrain.EngineHealth01 = 1f;
                if (engineWasDestroyed) powertrain.EngineRunning = true;
            }
            for (int i = 0; i < 4; ++i)
            {
                if (wheel[i] == null) continue;
                wheel[i].AlignmentDamage = 0f;
                wheel[i].BrakeTemperature = 20f;
            }
        }

        internal void ApplyWheelDamage(int index, float alignmentDelta)
        {
            if (index < 0 || index >= 4 || wheel[index] == null) return;
            wheel[index].AlignmentDamage = Mathf.Clamp(wheel[index].AlignmentDamage + alignmentDelta, -.3f, .3f);
        }

        internal Vector3 WheelLocalPosition(int index) => wheel[index].NeutralHub;

        private void OnApplicationFocus(bool focused) { if (!focused) Input = default; }
        private void OnApplicationPause(bool paused) { if (paused) Input = default; }
        private void OnDisable() { Input = default; }

        private void FixedUpdate()
        {
            if (!initialized) return;
            float dt = Time.fixedDeltaTime;
            DriverInput input = Input.Sanitized();
            float forwardSpeed = SignedSpeedMps;
            // Optional steering assistance limits rack demand, never applies chassis torque.
            float speedLimit = SteeringAssistEnabled ? 1f / (1f + Mathf.Abs(forwardSpeed) * .035f) : 1f;
            float targetRack = input.Steering * Specification.MaxSteerAngleRad * speedLimit * SteeringSensitivity;
            rackAngle = Mathf.MoveTowards(rackAngle, Mathf.Clamp(targetRack, -Specification.MaxSteerAngleRad,
                Specification.MaxSteerAngleRad), Specification.SteeringRateRadPerS * dt);
            SteeringAngles steering = SteeringModel.Ackermann(rackAngle, Specification.WheelbaseM, Specification.FrontTrackM);
            for (int i = 0; i < 4; ++i)
            {
                wheel[i].Steer = (i == 0 ? steering.LeftRad : i == 1 ? steering.RightRad : 0f) + wheel[i].AlignmentDamage;
                FindContact(i, dt);
            }
            ApplyAntiRoll(0, 1);
            ApplyAntiRoll(2, 3);

            float drivenOmega = 0f;
            int drivenCount = 0;
            float maxDrivenSlip = 0f;
            for (int i = 0; i < 4; ++i)
            {
                if (!IsDriven(i)) continue;
                drivenOmega += wheel[i].AngularVelocity;
                ++drivenCount;
                float rolling = wheel[i].AngularVelocity * Specification.WheelRadiusM;
                if (wheel[i].Grounded && Mathf.Abs(wheel[i].LongitudinalVelocity) > 2f)
                    maxDrivenSlip = Mathf.Max(maxDrivenSlip, (Mathf.Abs(rolling) - Mathf.Abs(wheel[i].LongitudinalVelocity)) /
                        Mathf.Max(2f, Mathf.Abs(wheel[i].LongitudinalVelocity)));
            }
            drivenOmega /= Mathf.Max(1, drivenCount);
            TractionControlActive = TractionControlEnabled && maxDrivenSlip > .16f && input.Throttle > .05f;
            float throttle = input.Throttle;
            if (TractionControlActive) throttle *= Mathf.Clamp01(1f - (maxDrivenSlip - .16f) * 2.5f);
            powertrain.EngineHealth01 = EngineHealth;
            powertrain.Step(throttle, drivenOmega, dt);
            float axleTorque = powertrain.OutputTorqueNm;
            for (int i = 0; i < 4; ++i)
            {
                SimulateWheel(i, input, IsDriven(i) ? axleTorque / Mathf.Max(1, drivenCount) : 0f, dt);
                UpdateWheelVisualPose(i, dt);
            }
            Vector3 velocity = Body.linearVelocity;
            // Aerodynamic drag is a force at COM, proportional to squared air speed.
            float density = 1.225f;
            Body.AddForce(-.5f * density * Specification.DragCoefficient * Specification.FrontalAreaM2 *
                velocity.magnitude * velocity, ForceMode.Force);
        }

        private bool IsDriven(int index)
        {
            if (Specification.Drivetrain == Drivetrain.AllWheelDrive) return true;
            return Specification.Drivetrain == Drivetrain.FrontWheelDrive ? index < 2 : index >= 2;
        }

        private void FindContact(int index, float dt)
        {
            WheelRuntime w = wheel[index];
            Vector3 up = transform.up;
            Vector3 anchor = transform.TransformPoint(w.Anchor);
            int count = UnityPhysics.SphereCastNonAlloc(anchor, Specification.WheelRadiusM, -up, contactBuffer,
                Specification.SuspensionRestLengthM, GroundLayers, QueryTriggerInteraction.Ignore);
            bool found = false;
            RaycastHit nearest = default;
            float distance = float.PositiveInfinity;
            for (int j = 0; j < count; ++j)
            {
                var hit = contactBuffer[j];
                if (hit.collider == null || hit.collider.attachedRigidbody == Body || hit.collider.transform.IsChildOf(transform)) continue;
                if (Vector3.Dot(hit.normal, up) < .2f || hit.distance >= distance) continue;
                // SphereCast does not reliably describe initial overlaps. A support ray below
                // recovers a valid contact rather than using its documented zero-point normal.
                if (hit.distance <= .0001f) continue;
                found = true;
                nearest = hit;
                distance = hit.distance;
            }
            if (!found)
            {
                int rays = UnityPhysics.RaycastNonAlloc(anchor, -up, contactBuffer,
                    Specification.SuspensionRestLengthM + Specification.WheelRadiusM, GroundLayers, QueryTriggerInteraction.Ignore);
                for (int j = 0; j < rays; ++j)
                {
                    var hit = contactBuffer[j];
                    if (hit.collider == null || hit.collider.attachedRigidbody == Body || hit.collider.transform.IsChildOf(transform)) continue;
                    if (Vector3.Dot(hit.normal, up) < .2f) continue;
                    float length = Mathf.Max(0f, hit.distance - Specification.WheelRadiusM);
                    if (length >= distance) continue;
                    found = true;
                    nearest = hit;
                    distance = length;
                }
            }
            w.Grounded = found;
            w.Length = found ? Mathf.Clamp(distance, 0f, Specification.SuspensionRestLengthM) : Specification.SuspensionRestLengthM;
            w.Compression = Specification.SuspensionRestLengthM - w.Length;
            w.Load = 0f;
            w.AxisLoad = 0f;
            if (!found)
            {
                w.WasGrounded = false;
                w.TireState = default;
                w.ContactBody = null;
                w.SurfaceId = DriveSurface.DryAsphalt;
                return;
            }
            w.ContactPoint = nearest.point;
            w.ContactNormal = nearest.normal;
            w.ContactBody = nearest.rigidbody;
            Vector3 groundVelocity = w.ContactBody == null ? Vector3.zero : w.ContactBody.GetPointVelocity(nearest.point);
            Vector3 relativeVelocity = Body.GetPointVelocity(nearest.point) - groundVelocity;
            float compressionVelocity = w.WasGrounded
                ? (w.Compression - w.PreviousCompression) / dt
                : -Vector3.Dot(Body.GetPointVelocity(anchor) - groundVelocity, nearest.normal) / Mathf.Max(.2f, Vector3.Dot(up, nearest.normal));
            // Limit damper impulses from discontinuous cast geometry; genuine impacts remain
            // handled by chassis colliders. This bound is explicitly a numerical safeguard.
            compressionVelocity = Mathf.Clamp(compressionVelocity, -12f, 12f);
            w.NormalProjection = Mathf.Max(.2f, Vector3.Dot(up, nearest.normal));
            w.AxisLoad = SuspensionModel.Evaluate(w.Compression, compressionVelocity, Specification);
            // The suspension returns force along the strut axis. Resolve its projection
            // onto the contact normal before using the normal load in the tire law.
            float maximumNormal = Mathf.Min(1000000f, Specification.MassKg * UnityPhysics.gravity.magnitude * 3f);
            w.AxisLoad = Mathf.Clamp(w.AxisLoad, 0f, maximumNormal * w.NormalProjection);
            w.Load = w.AxisLoad / w.NormalProjection;
            w.PreviousCompression = w.Compression;
            w.WasGrounded = true;
            Vector3 steeredForward = Quaternion.AngleAxis(w.Steer * Mathf.Rad2Deg, up) * transform.forward;
            w.Forward = Vector3.ProjectOnPlane(steeredForward, nearest.normal).normalized;
            w.Right = Vector3.Cross(nearest.normal, w.Forward).normalized;
            w.LongitudinalVelocity = Vector3.Dot(relativeVelocity, w.Forward);
            w.LateralVelocity = Vector3.Dot(relativeVelocity, w.Right);
            SurfaceProfile surface = nearest.collider.GetComponentInParent<SurfaceProfile>();
            w.Surface = surface == null ? DefaultSurface : surface.Parameters;
            w.SurfaceId = surface == null ? DriveSurface.DryAsphalt : surface.Surface;
        }

        private void ApplyAntiRoll(int left, int right)
        {
            WheelRuntime a = wheel[left], b = wheel[right];
            // The coupled bar only exchanges support when both tire contacts exist. A
            // drooping wheel requires unsprung dynamics, deliberately not approximated here.
            if (!a.Grounded || !b.Grounded) return;
            float transfer = (a.Compression - b.Compression) * Specification.AntiRollRateNPerM;
            transfer = Mathf.Clamp(transfer, -a.AxisLoad, b.AxisLoad);
            a.AxisLoad += transfer;
            b.AxisLoad -= transfer;
            a.Load = Mathf.Min(1000000f, a.AxisLoad / a.NormalProjection);
            b.Load = Mathf.Min(1000000f, b.AxisLoad / b.NormalProjection);
        }

        private void SimulateWheel(int index, DriverInput input, float driveTorque, float dt)
        {
            WheelRuntime w = wheel[index];
            bool front = index < 2;
            float bias = front ? Specification.FrontBrakeBias : 1f - Specification.FrontBrakeBias;
            float serviceBrake = input.Brake * Specification.MaxBrakeTorqueNm * bias * .5f;
            float handBrake = front ? 0f : input.Handbrake * Specification.MaxHandbrakeTorqueNm * .5f;
            float roadSpeed = Mathf.Abs(w.LongitudinalVelocity);
            float circumSpeed = Mathf.Abs(w.AngularVelocity * Specification.WheelRadiusM);
            bool releaseAbs = AbsEnabled && input.Brake > .01f && w.Grounded && roadSpeed > 2f && circumSpeed < roadSpeed * .84f;
            w.AbsPressure = Mathf.MoveTowards(w.AbsPressure, releaseAbs ? .1f : 1f, dt * (releaseAbs ? 25f : 8f));
            float fade = Mathf.Lerp(1f, .35f, Mathf.InverseLerp(450f, 800f, w.BrakeTemperature));
            float brakeTorque = (serviceBrake * w.AbsPressure + handBrake) * fade;
            float accumulatedLong = 0f, accumulatedLat = 0f;
            TireForces tire = default;
            int steps = Mathf.Clamp(WheelSubsteps, 4, 32);
            float subDt = dt / steps;
            float totalBrakeWork = 0f;
            for (int step = 0; step < steps; ++step)
            {
                if (w.Grounded && w.Load > 0f)
                {
                    tire = TireModel.Evaluate(ref w.TireState, w.LongitudinalVelocity, w.LateralVelocity,
                        w.AngularVelocity, w.Load, Specification.WheelRadiusM, Specification.Tire, w.Surface, subDt);
                }
                else tire = default;
                float rollingTorque = w.Grounded
                    ? Mathf.Max(0f, Specification.RollingResistance + w.Surface.RollingResistance - .013f) * w.Load * Specification.WheelRadiusM * Mathf.Clamp(w.AngularVelocity, -1f, 1f)
                    : 0f;
                float netTorque = driveTorque - tire.LongitudinalN * Specification.WheelRadiusM - rollingTorque;
                // Brake opposes wheel angular momentum, with an exact stopping cap. It
                // cannot reverse a stationary wheel by numerically overshooting zero.
                float predicted = w.AngularVelocity + netTorque / Specification.WheelInertiaKgM2 * subDt;
                float usedBrake = Mathf.Min(brakeTorque, Mathf.Abs(predicted) * Specification.WheelInertiaKgM2 / subDt);
                float newOmega = predicted - Mathf.Sign(predicted) * usedBrake / Specification.WheelInertiaKgM2 * subDt;
                totalBrakeWork += usedBrake * Mathf.Abs((w.AngularVelocity + newOmega) * .5f) * subDt;
                w.AngularVelocity = newOmega;
                accumulatedLong += tire.LongitudinalN / steps;
                accumulatedLat += tire.LateralN / steps;
            }
            w.BrakeTemperature += totalBrakeWork / 4000f;
            w.BrakeTemperature = Mathf.Max(20f, w.BrakeTemperature - (w.BrakeTemperature - 20f) * (.008f + roadSpeed * .0008f) * dt);
            if (w.Grounded)
            {
                Vector3 force = w.ContactNormal * w.Load + w.Forward * accumulatedLong + w.Right * accumulatedLat;
                Body.AddForceAtPosition(force, w.ContactPoint, ForceMode.Force);
                if (w.ContactBody != null && !w.ContactBody.isKinematic)
                    w.ContactBody.AddForceAtPosition(-force, w.ContactPoint, ForceMode.Force);
            }
            Wheels[index] = new WheelTelemetry
            {
                Grounded = w.Grounded,
                CompressionM = w.Compression,
                SuspensionLengthM = w.Length,
                NormalLoadN = w.Load,
                SlipRatio = tire.SlipRatio,
                SlipAngleRad = tire.SlipAngleRad,
                AngularVelocityRadS = w.AngularVelocity,
                SteerAngleRad = w.Steer,
                BrakeTemperatureC = w.BrakeTemperature,
                AbsActive = releaseAbs || w.AbsPressure < .9f,
                TireForceN = new Vector2(accumulatedLong, accumulatedLat),
                ContactPoint = w.Grounded ? w.ContactPoint : Vector3.zero,
                AlignmentDamageRad = w.AlignmentDamage,
                Surface = w.SurfaceId
            };
        }

        private void UpdateWheelVisualPose(int index, float dt)
        {
            WheelRuntime w = wheel[index];
            w.SpinAngle = Mathf.Repeat(w.SpinAngle + w.AngularVelocity * dt * Mathf.Rad2Deg, 360f);
            w.PreviousVisualPosition = w.NextVisualPosition;
            w.PreviousVisualRotation = w.NextVisualRotation;
            w.NextVisualPosition = w.Anchor - Vector3.up * w.Length;
            w.NextVisualRotation = Quaternion.Euler(0f, w.Steer * Mathf.Rad2Deg, 0f) *
                Quaternion.AngleAxis(w.SpinAngle, Vector3.right) * w.NeutralRotation;
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
            for (int i = 0; i < 4; ++i)
            {
                WheelRuntime w = wheel[i];
                if (w.Visual == null) continue;
                w.Visual.localPosition = Vector3.Lerp(w.PreviousVisualPosition, w.NextVisualPosition, alpha);
                w.Visual.localRotation = Quaternion.Slerp(w.PreviousVisualRotation, w.NextVisualRotation, alpha);
            }
        }
    }
}
