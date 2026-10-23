using System.Collections;
using ImpactDrive.Physics;
using ImpactDrive.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ImpactDrive.Tests
{
    /// <summary>
    /// Exercises the actual Unity rigidbody, casts and FixedUpdate integration.
    /// These are release-blocking checks until they have run in the pinned Editor;
    /// the separate .NET runner does not execute or substitute for these tests.
    /// </summary>
    public sealed class VehicleIntegrationTests
    {
        private GameObject ground;
        private GameObject vehicleObject;
        private VehicleController vehicle;
        private float previousFixedDeltaTime;
        private float previousTimeScale;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousFixedDeltaTime = Time.fixedDeltaTime;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 1;
            Time.fixedDeltaTime = 1f / 120f;
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Integration test ground";
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.transform.localScale = new Vector3(500, 1, 500);
            vehicleObject = new GameObject("Integration test vehicle");
            vehicleObject.transform.position = new Vector3(0, 0.65f, 0);
            vehicle = vehicleObject.AddComponent<VehicleController>();
            vehicle.Initialize(VehicleSpec.CreateAster(), null);
            vehicle.SetSpawnPose(vehicleObject.transform.position, Quaternion.identity);
            UnityEngine.Physics.SyncTransforms();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (vehicleObject != null) Object.Destroy(vehicleObject);
            if (ground != null) Object.Destroy(ground);
            Time.fixedDeltaTime = previousFixedDeltaTime;
            Time.timeScale = previousTimeScale;
            yield return null;
        }

        private static IEnumerator Simulate(float seconds)
        {
            int steps = Mathf.CeilToInt(seconds / Time.fixedDeltaTime);
            for (int i = 0; i < steps; i++) yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator SuspendedCarSettlesWithFourSupportingWheels()
        {
            vehicle.SetGear(0);
            yield return Simulate(5);
            float totalLoad = 0;
            foreach (var wheel in vehicle.Wheels)
            {
                Assert.That(wheel.Grounded, Is.True, "all four wheels should contact the level test surface");
                Assert.That(wheel.NormalLoadN, Is.GreaterThan(0));
                Assert.That(float.IsNaN(wheel.CompressionM), Is.False);
                totalLoad += wheel.NormalLoadN;
            }
            Assert.That(totalLoad, Is.EqualTo(vehicle.Body.mass * 9.81f).Within(vehicle.Body.mass * 9.81f * 0.2f),
                "settled wheel loads must approximately support weight");
            Assert.That(vehicle.Body.linearVelocity.magnitude, Is.LessThan(0.4f), "suspension must decay instead of bouncing indefinitely");
            Assert.That(vehicle.transform.position.y, Is.InRange(0.2f, 1.2f), "chassis must remain supported above the surface");
        }

        [UnityTest]
        public IEnumerator ThrottleAcceleratesThroughWheelContactAndBrakesStopTheCar()
        {
            vehicle.SetGear(1);
            yield return Simulate(3);
            vehicle.Input = new DriverInput { Throttle = 1 };
            yield return Simulate(7);
            float speedBeforeBraking = vehicle.SpeedKph;
            Assert.That(speedBeforeBraking, Is.GreaterThan(15), "drive torque must accelerate the chassis through tire contact");
            Assert.That(vehicle.transform.position.z, Is.GreaterThan(10), "forward gear must drive forward");
            vehicle.Input = new DriverInput { Brake = 1 };
            yield return Simulate(6);
            Assert.That(vehicle.SpeedKph, Is.LessThan(speedBeforeBraking * 0.2f), "per-wheel brake torque should reduce translational speed");
        }

        [UnityTest]
        public IEnumerator ReverseGearMovesVehicleBackwards()
        {
            yield return Simulate(3);
            vehicle.SetGear(-1);
            vehicle.Input = new DriverInput { Throttle = 0.5f };
            yield return Simulate(5);
            Assert.That(vehicle.transform.position.z, Is.LessThan(-2), "negative gearing must reverse tire drive force");
            Assert.That(Vector3.Dot(vehicle.Body.linearVelocity, vehicle.transform.forward), Is.LessThan(-0.5f));
        }

        [UnityTest]
        public IEnumerator SteeringWithoutWheelLoadCannotRotateChassis()
        {
            Object.Destroy(ground);
            ground = null;
            vehicle.transform.position = new Vector3(0, 50, 0);
            vehicle.Body.useGravity = false;
            vehicle.Body.linearVelocity = Vector3.zero;
            vehicle.Body.angularVelocity = Vector3.zero;
            Quaternion before = vehicle.transform.rotation;
            vehicle.Input = new DriverInput { Steering = 1, Throttle = 1 };
            yield return Simulate(2);
            Assert.That(Quaternion.Angle(before, vehicle.transform.rotation), Is.LessThan(0.1f),
                "steering input must not directly rotate the airborne chassis");
            Assert.That(vehicle.Body.linearVelocity.magnitude, Is.LessThan(0.1f),
                "spinning airborne wheels cannot directly propel the chassis");
        }
    }
}
