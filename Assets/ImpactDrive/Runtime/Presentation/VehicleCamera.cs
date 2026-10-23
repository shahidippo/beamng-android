using UnityEngine;

namespace ImpactDrive.Runtime
{
    public sealed class VehicleCamera : MonoBehaviour
    {
        public enum ViewMode { Chase, FarChase, Cockpit, Bonnet, Rear, Orbit }
        public VehicleController Vehicle;
        public VirtualControl LookControl;
        public bool InputEnabled = true;
        public ViewMode Mode { get; private set; }
        private Camera view;
        private readonly RaycastHit[] hits = new RaycastHit[16];
        private Vector3 velocity;
        private float yaw;
        private float elevation = 18f;
        private bool snap = true;

        public static VehicleCamera Create(VehicleController vehicle)
        {
            var cameraObject = new GameObject("Driving camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = .08f;
            camera.farClipPlane = 1600f;
            camera.fieldOfView = 64f;
            camera.allowHDR = true;
            cameraObject.AddComponent<AudioListener>();
            var driver = cameraObject.AddComponent<VehicleCamera>();
            driver.Vehicle = vehicle;
            driver.view = camera;
            return driver;
        }

        public void NextView()
        {
            Mode = (ViewMode)(((int)Mode + 1) % 6);
            yaw = 0f;
            velocity = Vector3.zero;
            snap = true;
        }

        public void SnapToVehicle() { snap = true; velocity = Vector3.zero; }

        private void LateUpdate()
        {
            if (!Vehicle || !Vehicle.Body) return;
            Transform body = Vehicle.transform;
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            if (InputEnabled)
            {
                Vector2 look = LookControl ? LookControl.Value : Vector2.zero;
                yaw += look.x * 85f * dt;
                elevation = Mathf.Clamp(elevation - look.y * 50f * dt, -5f, 65f);
                if (Input.GetMouseButton(1))
                {
                    yaw += Input.GetAxisRaw("Mouse X") * 2.2f;
                    elevation = Mathf.Clamp(elevation - Input.GetAxisRaw("Mouse Y") * 1.5f, -5f, 65f);
                }
            }
            if (Mode == ViewMode.Cockpit || Mode == ViewMode.Bonnet)
            {
                Vector3 local = Mode == ViewMode.Cockpit ? new Vector3(-.42f, .80f, .04f)
                    : new Vector3(0f, .56f, 1.35f);
                transform.SetPositionAndRotation(body.TransformPoint(local), body.rotation * Quaternion.Euler(0f, yaw, 0f));
                view.fieldOfView = Mode == ViewMode.Cockpit ? 78f : 68f;
                snap = true;
                return;
            }

            Vector3 target = Vehicle.Body.worldCenterOfMass + Vector3.up * .75f;
            float distance = Mode == ViewMode.FarChase ? 10f : Mode == ViewMode.Orbit ? 6.5f : 6f;
            float rear = Mode == ViewMode.Rear ? 180f : 0f;
            Quaternion rotation = Quaternion.Euler(elevation, body.eulerAngles.y + yaw + rear, 0f);
            Vector3 desiredOffset = rotation * Vector3.back * distance;
            Vector3 direction = desiredOffset.normalized;
            float availableDistance = distance;
            int count = UnityEngine.Physics.SphereCastNonAlloc(target, .2f, direction, hits,
                distance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider collider = hits[i].collider;
                if (!collider || collider.transform.IsChildOf(body)) continue;
                availableDistance = Mathf.Min(availableDistance, Mathf.Max(.35f, hits[i].distance - .1f));
            }
            Vector3 desired = target + direction * availableDistance;
            transform.position = snap ? desired : Vector3.SmoothDamp(transform.position, desired, ref velocity,
                .11f, float.PositiveInfinity, dt);
            // Correct immediately when an obstruction appears: smoothing must not put the camera through a wall.
            if (availableDistance < distance - .1f) transform.position = desired;
            transform.rotation = Quaternion.LookRotation(target - transform.position, Vector3.up);
            view.fieldOfView = Mathf.Lerp(64f, 73f, Mathf.Clamp01(Mathf.Abs(Vehicle.SpeedKph) / 160f));
            snap = false;
        }
    }
}
