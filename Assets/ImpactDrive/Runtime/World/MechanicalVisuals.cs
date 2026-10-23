using System;
using UnityEngine;

namespace ImpactDrive.Runtime
{
    /// <summary>
    /// Visual links follow the solver's wheel hub positions; they never drive physics.
    /// Brake carriers follow steering but do not rotate with the wheel/disc assembly.
    /// These links represent a reduced strut/control-arm layout, not solved hardpoints.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class MechanicalVisuals : MonoBehaviour
    {
        private VehicleController vehicle;
        private Transform[] wheels, brakes;
        private Transform[] struts, arms;
        private Transform steeringWheel;
        private Quaternion steeringRest;

        public void Initialize(VehicleController controller, Transform[] wheelHubs, Transform[] brakeCarriers)
        {
            vehicle = controller;
            wheels = wheelHubs;
            brakes = brakeCarriers;
            struts = new Transform[4];
            arms = new Transform[4];
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var metal = new Material(shader) { name = "Suspension link steel", color = new Color(.22f, .25f, .27f) };
            metal.SetFloat("_Metallic", .7f);
            metal.SetFloat("_Smoothness", .45f);
            for (int i = 0; i < 4; i++)
            {
                struts[i] = Link("Damper travel visual " + i, metal);
                arms[i] = Link("Lower control arm visual " + i, metal);
            }
            var pivot = new GameObject("Steering wheel pivot");
            pivot.transform.SetParent(transform, false);
            pivot.transform.localPosition = new Vector3(-.42f, .65f, .34f);
            steeringWheel = pivot.transform;
            steeringRest = steeringWheel.localRotation;
            // The OBJ preserves named mesh components; retain their world pose while
            // collecting the wheel/spokes under the physically positioned column pivot.
            foreach (MeshFilter part in GetComponentsInChildren<MeshFilter>())
            {
                string name = part.gameObject.name;
                if (name.IndexOf("Steering_Wheel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("SteeringSpoke", StringComparison.OrdinalIgnoreCase) >= 0)
                    part.transform.SetParent(steeringWheel, true);
            }
            LateUpdate();
        }

        private Transform Link(string name, Material material)
        {
            GameObject link = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            link.name = name;
            link.transform.SetParent(transform, false);
            link.GetComponent<Renderer>().sharedMaterial = material;
            Collider collision = link.GetComponent<Collider>();
            collision.enabled = false;
            Destroy(collision);
            return link.transform;
        }

        private void LateUpdate()
        {
            if (vehicle == null || wheels == null) return;
            for (int i = 0; i < 4; i++)
            {
                Vector3 hub = wheels[i].position;
                Vector3 localHub = transform.InverseTransformPoint(hub);
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 upper = transform.TransformPoint(new Vector3(localHub.x * .76f, .36f, localHub.z));
                Vector3 inner = transform.TransformPoint(new Vector3(side * .35f, .02f, localHub.z - .11f));
                PlaceLink(struts[i], hub, upper, .022f);
                PlaceLink(arms[i], hub, inner, .018f);
                brakes[i].position = hub;
                brakes[i].rotation = transform.rotation * Quaternion.Euler(0f, vehicle.Wheels[i].SteerAngleRad * Mathf.Rad2Deg, 0f);
            }
            float rack = (vehicle.Wheels[0].SteerAngleRad + vehicle.Wheels[1].SteerAngleRad) * .5f;
            steeringWheel.localRotation = steeringRest * Quaternion.AngleAxis(-rack * 14f * Mathf.Rad2Deg, Vector3.forward);
        }

        private static void PlaceLink(Transform link, Vector3 start, Vector3 end, float radius)
        {
            Vector3 delta = end - start;
            link.position = (start + end) * .5f;
            link.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            link.localScale = new Vector3(radius * 2f, delta.magnitude * .5f, radius * 2f);
        }
    }
}
