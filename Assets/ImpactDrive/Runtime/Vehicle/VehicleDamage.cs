using System;
using System.Collections.Generic;
using ImpactDrive.Physics;
using UnityEngine;

namespace ImpactDrive.Runtime
{
    /// <summary>
    /// Impact-energy-driven reduced-order structural damage. Permanent node displacement
    /// deforms readable body meshes and local impacts change alignment/engine output.
    /// Chassis collision shapes remain rigid: this is not a full soft-body crash solver.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VehicleDamage : MonoBehaviour
    {
        public bool DrawStructure;
        public float Damage01 => structure == null ? 0f : structure.Damage01;
        public float AbsorbedEnergyJ => structure == null ? 0f : structure.TotalAbsorbedEnergyJ;
        public event Action<float, float> Impact; // speed m/s, structural input energy J

        private sealed class DeformableMesh
        {
            internal MeshFilter Filter;
            internal Mesh Source;
            internal Mesh Instance;
            internal Vector3[] OriginalLocal;
            internal Vector3[] RootPositions;
            internal Vector3[] Output;
        }

        private VehicleController vehicle;
        private DamageModel structure;
        private readonly List<DeformableMesh> meshes = new List<DeformableMesh>();
        private Transform[] wheelVisuals;

        internal void Initialize(VehicleController owner, Transform[] wheels)
        {
            ReleaseMeshes();
            vehicle = owner;
            wheelVisuals = wheels;
            structure = DamageModel.CreateHatchback();
            foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null || !filter.sharedMesh.isReadable || IsWheel(filter.transform)) continue;
                // Glass, trim and interior are intentionally all retained as mesh geometry;
                // component-specific breaking is a later content-authoring requirement.
                Mesh source = filter.sharedMesh;
                Mesh instance = Instantiate(source);
                instance.name = source.name + " (vehicle damage instance)";
                instance.MarkDynamic();
                Vector3[] original = instance.vertices;
                Vector3[] root = new Vector3[original.Length];
                for (int i = 0; i < original.Length; ++i)
                    root[i] = transform.InverseTransformPoint(filter.transform.TransformPoint(original[i]));
                filter.sharedMesh = instance;
                meshes.Add(new DeformableMesh
                {
                    Filter = filter,
                    Source = source,
                    Instance = instance,
                    OriginalLocal = original,
                    RootPositions = root,
                    Output = new Vector3[original.Length]
                });
            }
        }

        private bool IsWheel(Transform candidate)
        {
            if (wheelVisuals == null) return false;
            foreach (Transform wheel in wheelVisuals)
                if (wheel != null && (candidate == wheel || candidate.IsChildOf(wheel))) return true;
            return false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (vehicle == null || structure == null || collision.contactCount == 0) return;
            float maximumClosingSpeed = 0f;
            for (int i = 0; i < collision.contactCount; ++i)
                maximumClosingSpeed = Mathf.Max(maximumClosingSpeed,
                    Mathf.Abs(Vector3.Dot(collision.relativeVelocity, collision.GetContact(i).normal)));
            float energyFromImpulse = .5f * collision.impulse.magnitude * maximumClosingSpeed;
            float availableEnergy = .5f * vehicle.Body.mass * maximumClosingSpeed * maximumClosingSpeed;
            float energy = Mathf.Min(energyFromImpulse, availableEnergy);
            if (energy < 150f || maximumClosingSpeed < .6f) return;
            vehicle.LastImpactSpeedMps = maximumClosingSpeed;
            vehicle.LastImpactEnergyJ = energy;
            float perContact = energy / collision.contactCount;
            for (int i = 0; i < collision.contactCount; ++i)
            {
                ContactPoint contact = collision.GetContact(i);
                Vector3 localPoint = transform.InverseTransformPoint(contact.point);
                Vector3 localDirection = transform.InverseTransformDirection(contact.normal).normalized;
                var result = structure.ApplyImpact(ToSim(localPoint), ToSim(localDirection), perContact, .95f);
                // Mechanical consequences depend on impact location and actual absorbed
                // structural work. Thresholds are provisional original-vehicle design data.
                float absorbed = result.AbsorbedEnergyJ;
                if (localPoint.z > .55f && localPoint.y < .9f)
                {
                    // Front-rail intrusion can rupture the cooling/powertrain assembly.
                    // Use actual permanent displacement so a saturated crumple zone does
                    // not leave an otherwise destroyed engine delivering full torque.
                    float frontalIntrusion = 0f;
                    foreach (StructuralNode node in structure.Nodes)
                        if (node.OriginalPosition.Z > 1.4f)
                            frontalIntrusion = Mathf.Max(frontalIntrusion, node.OriginalPosition.Z - node.Position.Z);
                    float crushHealth = 1f - Mathf.InverseLerp(.08f, .32f, frontalIntrusion);
                    vehicle.EngineHealth = Mathf.Min(crushHealth, Mathf.Clamp01(vehicle.EngineHealth - absorbed / 90000f));
                }
                for (int wheel = 0; wheel < 4; ++wheel)
                {
                    Vector3 hub = vehicle.WheelLocalPosition(wheel);
                    float proximity = Mathf.Clamp01(1f - Vector3.Distance(localPoint, hub) / .9f);
                    if (proximity <= 0f) continue;
                    float side = wheel % 2 == 0 ? -1f : 1f;
                    float bendDirection = Mathf.Abs(localDirection.x) > .15f ? Mathf.Sign(localDirection.x) : -side;
                    vehicle.ApplyWheelDamage(wheel, bendDirection * proximity * absorbed / 100000f);
                }
            }
            DeformMeshes();
            Impact?.Invoke(maximumClosingSpeed, energy);
        }

        private void DeformMeshes()
        {
            if (structure == null) return;
            foreach (DeformableMesh deformable in meshes)
            {
                if (deformable.Filter == null || deformable.Instance == null) continue;
                for (int vertex = 0; vertex < deformable.Output.Length; ++vertex)
                {
                    Vector3 point = deformable.RootPositions[vertex];
                    Vector3 delta = Vector3.zero;
                    float sum = 0f;
                    foreach (StructuralNode node in structure.Nodes)
                    {
                        Vector3 original = ToUnity(node.OriginalPosition);
                        float squaredDistance = (point - original).sqrMagnitude;
                        float weight = 1f / (.04f + squaredDistance * squaredDistance);
                        delta += (ToUnity(node.Position) - original) * weight;
                        sum += weight;
                    }
                    if (sum > 0f) delta /= sum;
                    Vector3 world = transform.TransformPoint(point + delta);
                    deformable.Output[vertex] = deformable.Filter.transform.InverseTransformPoint(world);
                }
                deformable.Instance.vertices = deformable.Output;
                deformable.Instance.RecalculateNormals();
                deformable.Instance.RecalculateBounds();
            }
        }

        public void Repair()
        {
            if (structure != null) structure.Reset();
            foreach (DeformableMesh deformable in meshes)
            {
                if (deformable.Instance == null) continue;
                deformable.Instance.vertices = deformable.OriginalLocal;
                deformable.Instance.RecalculateNormals();
                deformable.Instance.RecalculateBounds();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!DrawStructure || structure == null) return;
            foreach (StructuralBeam beam in structure.Beams)
            {
                Gizmos.color = beam.Broken ? Color.red : Color.Lerp(Color.green, Color.yellow, Mathf.Abs(beam.Strain) * 8f);
                Gizmos.DrawLine(transform.TransformPoint(ToUnity(structure.Nodes[beam.A].Position)),
                    transform.TransformPoint(ToUnity(structure.Nodes[beam.B].Position)));
            }
            foreach (StructuralNode node in structure.Nodes)
            {
                Gizmos.color = node.Broken ? Color.red : Color.cyan;
                Gizmos.DrawSphere(transform.TransformPoint(ToUnity(node.Position)), .035f);
            }
        }

        private void ReleaseMeshes()
        {
            foreach (DeformableMesh mesh in meshes)
            {
                if (mesh.Filter != null) mesh.Filter.sharedMesh = mesh.Source;
                if (mesh.Instance != null) Destroy(mesh.Instance);
            }
            meshes.Clear();
        }

        private void OnDestroy() => ReleaseMeshes();
        private static SimVector ToSim(Vector3 value) => new SimVector(value.x, value.y, value.z);
        private static Vector3 ToUnity(SimVector value) => new Vector3(value.X, value.Y, value.Z);
    }
}
