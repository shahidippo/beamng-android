using System;
using System.Collections.Generic;
using ImpactDrive.Physics;
using UnityEngine;
using UnityEngine.Rendering;

namespace ImpactDrive.Runtime
{
    /// <summary>
    /// Builds the bounded engineering proving ground and one original test vehicle.
    /// This is a validation environment, not any of the eight proposed launch maps.
    /// All dimensions are metres; roads are physical surfaces with continuous access.
    /// </summary>
    public static class ProvingGroundBuilder
    {
        public static readonly Vector3 SpawnPosition = new Vector3(0f, 0.65f, -230f);
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        public static VehicleController Build()
        {
            Materials.Clear();
            var world = new GameObject("Aster Engineering Proving Ground");
            BuildLighting(world.transform);
            BuildGround(world.transform);
            BuildGarage(world.transform);
            BuildTestFixtures(world.transform);
            VehicleController vehicle = BuildVehicle();
            vehicle.SetSpawnPose(SpawnPosition, Quaternion.identity);
            return vehicle;
        }

        private static void BuildLighting(Transform parent)
        {
            var sun = new GameObject("Late morning sun");
            sun.transform.SetParent(parent);
            sun.transform.rotation = Quaternion.Euler(48f, -38f, 0f);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, .93f, .83f);
            light.intensity = 1.45f;
            light.shadows = LightShadows.Soft;
            light.shadowBias = .035f;
            RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.48f, .59f, .7f);
            RenderSettings.ambientEquatorColor = new Color(.38f, .43f, .46f);
            RenderSettings.ambientGroundColor = new Color(.19f, .20f, .16f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.65f, .74f, .8f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 320f;
            RenderSettings.fogEndDistance = 850f;
            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader) { name = "Proving ground sky" };
                sky.SetFloat("_SunSize", .025f);
                sky.SetFloat("_AtmosphereThickness", .8f);
                sky.SetColor("_SkyTint", new Color(.48f, .58f, .68f));
                sky.SetColor("_GroundColor", new Color(.3f, .33f, .28f));
                RenderSettings.skybox = sky;
            }
        }

        private static void BuildGround(Transform parent)
        {
            Material grass = Mat("Dry site grass", new Color(.30f, .37f, .25f), 0f, .18f);
            Material asphalt = Mat("Dry asphalt", new Color(.135f, .15f, .16f), 0f, .30f);
            Material concrete = Mat("Test concrete", new Color(.48f, .50f, .49f), 0f, .35f);
            Material paint = Mat("Road marking", new Color(.89f, .88f, .75f), 0f, .2f);
            Box("Site terrain: 640 x 800 m", new Vector3(0f, -.25f, 80f), new Vector3(640f, .44f, 800f), grass, parent, true)
                .AddComponent<SurfaceProfile>().Surface = DriveSurface.Grass;

            // A 600 m run-up and braking lane; every branch joins the main lane.
            Road("Main acceleration lane", new Vector3(0f, 0f, 40f), new Vector3(24f, .04f, 600f), asphalt, parent);
            Road("West access", new Vector3(-55f, 0f, -190f), new Vector3(134f, .04f, 16f), asphalt, parent);
            Road("East south access", new Vector3(72f, 0f, -180f), new Vector3(164f, .04f, 16f), asphalt, parent);
            Road("East north access", new Vector3(72f, 0f, 0f), new Vector3(164f, .04f, 16f), asphalt, parent);
            Road("West return", new Vector3(-62f, 0f, -30f), new Vector3(124f, .04f, 16f), asphalt, parent);
            Road("Skidpad access", new Vector3(-55f, 0f, 60f), new Vector3(110f, .04f, 18f), asphalt, parent);
            Road("Gradient access", new Vector3(-48f, 0f, 150f), new Vector3(120f, .04f, 16f), asphalt, parent);
            Road("Gradient exit", new Vector3(-48f, 0f, 250f), new Vector3(120f, .04f, 16f), asphalt, parent);
            Road("Suspension road", new Vector3(-65f, 0f, -110f), new Vector3(16f, .04f, 170f), concrete, parent, DriveSurface.Concrete);
            Road("Gradient road", new Vector3(-90f, 0f, 200f), new Vector3(18f, .04f, 120f), concrete, parent, DriveSurface.Concrete);

            Road("Wet braking strip", new Vector3(65f, .003f, -90f), new Vector3(20f, .04f, 170f),
                Mat("Wet asphalt", new Color(.10f, .16f, .19f), .05f, .88f), parent, DriveSurface.WetAsphalt);
            Road("Gravel braking strip", new Vector3(96f, .003f, -90f), new Vector3(20f, .04f, 170f),
                Mat("Gravel", new Color(.48f, .42f, .31f), 0f, .05f), parent, DriveSurface.Gravel);
            Road("Ice braking strip", new Vector3(127f, .003f, -90f), new Vector3(20f, .04f, 170f),
                Mat("Ice", new Color(.56f, .73f, .78f), .08f, .91f), parent, DriveSurface.Ice);

            Disc("Skidpad", new Vector3(-110f, .022f, 60f), 52f, asphalt, parent, true);
            Ring("25 m radius", new Vector3(-110f, .025f, 60f), 25f, .16f, paint, parent);
            Ring("40 m radius", new Vector3(-110f, .025f, 60f), 40f, .16f, paint, parent);
            Disc("Skidpad centre", new Vector3(-110f, .026f, 60f), .4f, paint, parent, false);
            for (int z = -245; z <= 320; z += 12)
                Box("Centre dash", new Vector3(0f, .023f, z), new Vector3(.16f, .004f, 5f), paint, parent, false);
            for (int side = -1; side <= 1; side += 2)
                Box("Runway edge", new Vector3(side * 10.8f, .023f, 40f), new Vector3(.16f, .004f, 598f), paint, parent, false);
            for (int z = -200; z <= 300; z += 50)
            {
                Box("Distance mark", new Vector3(8.5f, .025f, z), new Vector3(3f, .004f, .12f), paint, parent, false);
                Sign((z + 200) + " m", new Vector3(14f, 0f, z), parent, 0f, 1.1f);
            }

            // Fixed geometry landmarks help judge motion without hiding the road.
            Material berm = Mat("Earth berm", new Color(.37f, .35f, .25f), 0f, .08f);
            Ramp("West boundary berm", new Vector3(-285f, -.03f, 80f), 580f, 22f, 4f, berm, parent, 90f);
            Ramp("East boundary berm", new Vector3(285f, -.03f, 80f), 580f, 22f, 4f, berm, parent, -90f);
            for (int i = 0; i < 18; i++)
            {
                float z = -220f + i * 34f;
                Tree(new Vector3(185f + (i % 3) * 6f, -.03f, z), parent, 3.8f + (i % 4) * .45f);
                Tree(new Vector3(-215f - (i % 3) * 7f, -.03f, z + 12f), parent, 4.1f + (i % 3) * .5f);
            }
        }

        private static void BuildTestFixtures(Transform parent)
        {
            Material concrete = Mat("Test concrete", new Color(.48f, .50f, .49f), 0f, .35f);
            Material yellow = Mat("Safety yellow", new Color(.91f, .63f, .08f), 0f, .25f);
            Material black = Mat("Safety black", new Color(.06f, .07f, .075f), 0f, .2f);

            Bump("80 mm diagonal speed bump", new Vector3(-65f, .02f, -145f), 13f, 1.1f, .08f, yellow, parent, 18f);
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                Bump("Offset articulation " + i, new Vector3(-65f + side * .8f, .02f, -110f + i * 5f), 1.1f,
                    1.4f, .07f + i * .012f, concrete, parent, 0f);
            }
            Ramp("10 percent ascent", new Vector3(-90f, .02f, 175f), 12f, 30f, 3f, concrete, parent);
            Box("Gradient summit", new Vector3(-90f, 1.52f, 195f), new Vector3(12f, 3f, 10f), concrete, parent, true);
            Ramp("10 percent descent", new Vector3(-90f, .02f, 215f), 12f, 30f, 3f, concrete, parent, 180f);

            GameObject wall = Box("Instrumented impact wall", new Vector3(0f, 1.05f, 338f), new Vector3(23f, 2.1f, 1f), concrete, parent, true);
            for (int i = -5; i <= 5; i++)
            {
                Box("Impact wall warning", new Vector3(i * 2f, 1.2f, 337.48f), new Vector3(1f, 1.6f, .025f), yellow, parent, false);
            }
            Sign("IMPACT WALL\nEND OF STRAIGHT", new Vector3(0f, 2.3f, 339f), parent, 0f, 1.5f);
            Sign("BRAKE / ACCELERATION", new Vector3(-20f, 0f, -205f), parent, 0f, 1.25f);
            Sign("WET", new Vector3(52f, 0f, -185f), parent, 0f, 1.05f);
            Sign("GRAVEL", new Vector3(83f, 0f, -185f), parent, 0f, 1.05f);
            Sign("ICE", new Vector3(114f, 0f, -185f), parent, 0f, 1.05f);
            Sign("SUSPENSION\n80 mm bump / offset humps", new Vector3(-76f, 0f, -164f), parent, 0f, 1.2f);
            Sign("SKIDPAD\n25 m / 40 m", new Vector3(-51f, 0f, 78f), parent, 90f, 1.25f);
            Sign("10% GRADIENT", new Vector3(-103f, 0f, 150f), parent, 0f, 1.1f);
            for (int i = 0; i < 8; i++) Cone(new Vector3(-5f + (i % 2) * 10f, .02f, -170f + i * 16f), parent);
            for (int i = 0; i < 14; i++) Cone(new Vector3(-120f + i * 8f, .02f, -195f), parent);
        }

        private static void BuildGarage(Transform parent)
        {
            Material concrete = Mat("Test concrete", new Color(.48f, .50f, .49f), 0f, .35f);
            Material steel = Mat("Garage steel", new Color(.13f, .19f, .22f), .5f, .5f);
            Road("Garage access", new Vector3(-22f, 0f, -230f), new Vector3(44f, .04f, 14f), concrete, parent);
            Road("Service apron", new Vector3(-43f, 0f, -230f), new Vector3(25f, .04f, 25f), concrete, parent);
            Box("Service shelter roof", new Vector3(-43f, 4.8f, -234f), new Vector3(18f, .22f, 12f), steel, parent, true);
            foreach (float x in new[] { -51f, -35f })
                foreach (float z in new[] { -239f, -229f })
                    Box("Shelter column", new Vector3(x, 2.4f, z), new Vector3(.24f, 4.8f, .24f), steel, parent, true);
            Sign("IMPACTDRIVE\nASTER ENGINEERING", new Vector3(-43f, 3.1f, -239.5f), parent, 180f, 1.6f);
            for (int i = 0; i < 3; i++)
                Box("Service cabinet", new Vector3(-50f + i * 2f, .85f, -238f), new Vector3(1.1f, 1.7f, .65f), steel, parent, true);
        }

        private static VehicleController BuildVehicle()
        {
            VehicleSpec spec = VehicleSpec.CreateAster();
            TextAsset definition = Resources.Load<TextAsset>("ImpactDrive/AsterSpec");
            if (definition != null)
            {
                // Populate explicit fields over complete defaults, preserving schema defaults.
                JsonUtility.FromJsonOverwrite(definition.text, spec);
            }
            spec.Validate();
            var root = new GameObject("Aster 1.6 — original engineering vehicle");
            root.transform.position = SpawnPosition;
            var rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var bodyCollider = root.AddComponent<BoxCollider>();
            bodyCollider.center = new Vector3(0f, .22f, 0f);
            bodyCollider.size = new Vector3(1.66f, .60f, 3.89f);
            var cabinCollider = root.AddComponent<BoxCollider>();
            cabinCollider.center = new Vector3(0f, .72f, -.3f);
            cabinCollider.size = new Vector3(1.39f, .60f, 1.46f);

            InstantiateModel("ImpactDrive/AsterBody", "Original body and interior", root.transform);
            Transform[] wheels = new Transform[4];
            Transform[] brakes = new Transform[4];
            for (int i = 0; i < wheels.Length; i++)
            {
                bool front = i < 2;
                bool left = i % 2 == 0;
                float track = front ? spec.FrontTrackM : spec.RearTrackM;
                var hub = new GameObject(new[] { "Front left hub", "Front right hub", "Rear left hub", "Rear right hub" }[i]);
                hub.transform.SetParent(root.transform, false);
                hub.transform.localPosition = new Vector3((left ? -.5f : .5f) * track, -.15f, (front ? .5f : -.5f) * spec.WheelbaseM);
                GameObject wheel = InstantiateModel("ImpactDrive/AsterWheel", "Original 205 tyre and alloy", hub.transform);
                wheel.transform.localScale = Vector3.one * (spec.WheelRadiusM / .31f);
                if (left) wheel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                wheels[i] = hub.transform;
                var brakePivot = new GameObject("Stationary brake carrier " + i);
                brakePivot.transform.SetParent(root.transform, false);
                brakePivot.transform.localPosition = hub.transform.localPosition;
                GameObject brake = InstantiateModel("ImpactDrive/AsterBrake", "Brake caliper", brakePivot.transform);
                brake.transform.localScale = Vector3.one * (spec.WheelRadiusM / .31f);
                if (left) brake.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                brakes[i] = brakePivot.transform;
            }
            VehicleController controller = root.AddComponent<VehicleController>();
            controller.Initialize(spec, wheels);
            root.AddComponent<MechanicalVisuals>().Initialize(controller, wheels, brakes);
            return controller;
        }

        private static GameObject InstantiateModel(string path, string name, Transform parent)
        {
            GameObject source = Resources.Load<GameObject>(path);
            if (source == null) throw new InvalidOperationException("Required original vehicle asset missing: Resources/" + path + ". Run Tools/Blender/create_aster.py and let Unity import the OBJ files.");
            GameObject model = UnityEngine.Object.Instantiate(source, parent, false);
            model.name = name;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                Material[] assigned = renderer.sharedMaterials;
                for (int i = 0; i < assigned.Length; i++)
                    assigned[i] = VehicleMaterial(assigned[i] != null ? assigned[i].name : "Aster_Paint");
                renderer.sharedMaterials = assigned;
            }
            return model;
        }

        private static Material VehicleMaterial(string name)
        {
            if (name.Contains("Glass"))
            {
                Material glass = Mat("Aster_Glass", new Color(.12f, .21f, .26f, .25f), .12f, .87f);
                glass.SetFloat("_Surface", 1f);
                glass.SetFloat("_Blend", 0f);
                glass.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                glass.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                glass.SetFloat("_ZWrite", 0f);
                glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                glass.SetOverrideTag("RenderType", "Transparent");
                glass.renderQueue = (int)RenderQueue.Transparent;
                return glass;
            }
            if (name.Contains("Rubber")) return Mat("Aster_Rubber", new Color(.028f, .028f, .028f), 0f, .16f);
            if (name.Contains("Alloy")) return Mat("Aster_Alloy", new Color(.51f, .57f, .59f), .86f, .78f);
            if (name.Contains("Chrome")) return Mat("Aster_Chrome", new Color(.8f, .84f, .86f), .95f, .85f);
            if (name.Contains("Headlamp")) return Mat("Aster_Headlamp", new Color(.80f, .92f, .97f), .45f, .85f);
            if (name.Contains("Taillamp")) return Mat("Aster_Taillamp", new Color(.72f, .025f, .015f), .25f, .8f);
            if (name.Contains("Indicator")) return Mat("Aster_Indicator", new Color(1f, .32f, .025f), .2f, .7f);
            if (name.Contains("Caliper")) return Mat("Aster_Caliper", new Color(.53f, .10f, .045f), .5f, .67f);
            if (name.Contains("Brake")) return Mat("Aster_Brake", new Color(.20f, .22f, .23f), .7f, .44f);
            if (name.Contains("Fabric")) return Mat("Aster_Fabric", new Color(.12f, .145f, .16f), 0f, .05f);
            if (name.Contains("Trim")) return Mat("Aster_Trim", new Color(.025f, .03f, .035f), .12f, .42f);
            return Mat("Aster_Paint", new Color(.035f, .23f, .34f), .65f, .75f);
        }

        private static Material Mat(string name, Color color, float metallic, float smoothness)
        {
            if (Materials.TryGetValue(name, out Material cached)) return cached;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("URP/Lit shader unavailable. Generate the Unity project render assets first.");
            var material = new Material(shader) { name = name, color = color };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            Materials.Add(name, material);
            return material;
        }

        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool collider)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider)
            {
                Collider collision = obj.GetComponent<Collider>();
                collision.enabled = false;
                UnityEngine.Object.Destroy(collision);
            }
            obj.isStatic = true;
            return obj;
        }

        private static void Road(string name, Vector3 position, Vector3 size, Material material, Transform parent, DriveSurface surface = DriveSurface.DryAsphalt)
        {
            GameObject road = Box(name, position, size, material, parent, true);
            road.AddComponent<SurfaceProfile>().Surface = surface;
        }

        private static GameObject MeshObject(string name, Vector3 position, Vector3[] vertices, int[] triangles, Material material, Transform parent, bool collide)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collide) obj.AddComponent<MeshCollider>().sharedMesh = mesh;
            obj.isStatic = true;
            return obj;
        }

        private static void Disc(string name, Vector3 position, float radius, Material mat, Transform parent, bool collider)
        {
            const int segments = 96;
            var vertices = new Vector3[segments + 1];
            var triangles = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                float a = i * 2f * Mathf.PI / segments;
                vertices[i + 1] = new Vector3(Mathf.Sin(a) * radius, 0f, Mathf.Cos(a) * radius);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % segments + 1;
            }
            MeshObject(name, position, vertices, triangles, mat, parent, collider);
        }

        private static void Ring(string name, Vector3 position, float radius, float width, Material mat, Transform parent)
        {
            const int segments = 128;
            var vertices = new Vector3[segments * 2];
            var triangles = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float a = i * 2f * Mathf.PI / segments;
                Vector3 radial = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                vertices[i * 2] = radial * radius;
                vertices[i * 2 + 1] = radial * (radius + width);
                int j = (i + 1) % segments;
                int k = i * 6;
                triangles[k] = i * 2; triangles[k + 1] = i * 2 + 1; triangles[k + 2] = j * 2;
                triangles[k + 3] = j * 2; triangles[k + 4] = i * 2 + 1; triangles[k + 5] = j * 2 + 1;
            }
            MeshObject(name, position, vertices, triangles, mat, parent, false);
        }

        private static void Ramp(string name, Vector3 position, float width, float length, float height, Material mat, Transform parent, float yaw = 0f)
        {
            float x = width * .5f, z = length * .5f;
            Vector3[] v = { new Vector3(-x, 0, -z), new Vector3(x, 0, -z), new Vector3(-x, height, z),
                new Vector3(x, height, z), new Vector3(-x, 0, z), new Vector3(x, 0, z) };
            int[] t = { 0, 2, 1, 1, 2, 3, 0, 4, 2, 1, 3, 5, 2, 4, 3, 3, 4, 5, 0, 1, 4, 1, 5, 4 };
            GameObject obj = MeshObject(name, position, v, t, mat, parent, true);
            obj.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private static void Bump(string name, Vector3 position, float width, float length, float height, Material mat, Transform parent, float yaw)
        {
            const int steps = 16;
            var v = new Vector3[(steps + 1) * 2];
            var t = new int[steps * 6];
            for (int i = 0; i <= steps; i++)
            {
                float u = i / (float)steps;
                float y = height * .5f * (1f - Mathf.Cos(u * 2f * Mathf.PI));
                v[i * 2] = new Vector3(-width * .5f, y, (u - .5f) * length);
                v[i * 2 + 1] = new Vector3(width * .5f, y, (u - .5f) * length);
                if (i == steps) continue;
                int k = i * 6;
                t[k] = i * 2; t[k + 1] = (i + 1) * 2; t[k + 2] = i * 2 + 1;
                t[k + 3] = i * 2 + 1; t[k + 4] = (i + 1) * 2; t[k + 5] = (i + 1) * 2 + 1;
            }
            GameObject bump = MeshObject(name, position, v, t, mat, parent, true);
            bump.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private static void Sign(string title, Vector3 position, Transform parent, float yaw, float size)
        {
            var sign = new GameObject(title + " sign");
            sign.transform.SetParent(parent, false);
            sign.transform.position = position;
            sign.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Material metal = Mat("Sign backing", new Color(.09f, .15f, .18f), .2f, .3f);
            GameObject pole = Box("Signpost", Vector3.zero, new Vector3(.07f, 1.6f * size, .07f), metal, sign.transform, true);
            pole.transform.localPosition = new Vector3(0, .8f * size, 0);
            pole.transform.localRotation = Quaternion.identity;
            GameObject board = Box("Sign board", Vector3.zero, new Vector3(3.1f * size, .8f * size, .06f), metal, sign.transform, true);
            board.transform.localPosition = new Vector3(0, 1.6f * size, 0);
            board.transform.localRotation = Quaternion.identity;
            var letters = new GameObject("Label");
            letters.transform.SetParent(sign.transform, false);
            letters.transform.localPosition = new Vector3(0f, 1.6f * size, -.04f);
            var label = letters.AddComponent<TextMesh>();
            label.text = title;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 48;
            label.characterSize = .075f * size;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(.84f, .92f, .94f);
            letters.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
        }

        private static void Cone(Vector3 position, Transform parent)
        {
            Material orange = Mat("Cone orange", new Color(1f, .25f, .035f), 0f, .35f);
            Material black = Mat("Cone rubber", new Color(.035f, .04f, .04f), 0f, .15f);
            Box("Cone base", position + Vector3.up * .025f, new Vector3(.38f, .05f, .38f), black, parent, false);
            const int count = 12;
            var v = new Vector3[count + 1];
            var t = new int[count * 3];
            v[0] = Vector3.up * .55f;
            for (int i = 0; i < count; i++)
            {
                float a = i * 2f * Mathf.PI / count;
                v[i + 1] = new Vector3(Mathf.Sin(a) * .14f, .05f, Mathf.Cos(a) * .14f);
                t[i * 3] = 0; t[i * 3 + 1] = i + 1; t[i * 3 + 2] = (i + 1) % count + 1;
            }
            MeshObject("Non-colliding lane cone", position, v, t, orange, parent, false);
        }

        private static void Tree(Vector3 position, Transform parent, float height)
        {
            Material trunkMat = Mat("Tree bark", new Color(.23f, .19f, .13f), 0f, .08f);
            Material leaves = Mat("Site trees", new Color(.18f, .28f, .18f), 0f, .12f);
            Box("Tree trunk", position + Vector3.up * height * .35f, new Vector3(.35f, height * .7f, .35f), trunkMat, parent, true);
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Tree crown";
            crown.transform.SetParent(parent, false);
            crown.transform.position = position + Vector3.up * height;
            crown.transform.localScale = new Vector3(height * .8f, height, height * .8f);
            crown.GetComponent<Renderer>().sharedMaterial = leaves;
            Collider collision = crown.GetComponent<Collider>();
            collision.enabled = false;
            UnityEngine.Object.Destroy(collision);
            crown.isStatic = true;
        }
    }
}
