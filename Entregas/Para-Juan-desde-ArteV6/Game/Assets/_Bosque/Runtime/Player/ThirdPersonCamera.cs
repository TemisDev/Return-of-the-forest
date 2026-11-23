using UnityEngine;
using UnityEngine.InputSystem;

namespace Bosque.Player
{
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 4.4f;
        [SerializeField] private float focusHeight = 1.35f;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private float yaw;
        private float pitch = 20f;
        private Vector3 velocity;
        private Vector3 previousFocus;
        private bool initialized;
        public float Yaw => yaw;
        public void Configure(Transform follow)
        {
            target = follow;
            yaw = follow.eulerAngles.y;
            initialized = false;
            Step(Vector2.zero, .016f);
        }
        private void Start() { if (target != null) Configure(target); }
        private void LateUpdate()
        {
            var look = Cursor.lockState == CursorLockMode.Locked && Mouse.current != null
                ? Mouse.current.delta.ReadValue() : Vector2.zero;
            Step(look, Time.deltaTime);
        }
        public void Step(Vector2 lookDelta, float dt)
        {
            if (target == null || dt <= 0f) return;
            lookDelta = Vector2.ClampMagnitude(lookDelta, 256f);
            float k = .12f * CameraSettings.Sensitivity;
            yaw = Mathf.Repeat(yaw + lookDelta.x * k, 360f);
            pitch = Mathf.Clamp(pitch - lookDelta.y * k * (CameraSettings.InvertY ? -1f : 1f), -20f, 65f);
            var focus = target.position + Vector3.up * focusHeight;
            var desired = focus + Quaternion.Euler(pitch, yaw, 0) * Vector3.back * distance;
            desired = ResolveObstruction(focus, desired);
            if (!initialized || Vector3.Distance(previousFocus, focus) > 8f)
            {
                transform.position = desired;
                velocity = Vector3.zero;
                initialized = true;
            }
            else
            {
                var smoothed = Vector3.SmoothDamp(transform.position, desired, ref velocity,
                    .07f, Mathf.Infinity, dt);
                transform.position = ResolveObstruction(focus, smoothed);
            }
            transform.LookAt(focus);
            previousFocus = focus;
        }
        private Vector3 ResolveObstruction(Vector3 focus, Vector3 desired)
        {
            var offset = desired - focus;
            float length = offset.magnitude;
            if (length < .001f) return desired;
            var direction = offset / length;
            int count = Physics.SphereCastNonAlloc(focus, .18f, direction, hits, length, ~0,
                QueryTriggerInteraction.Ignore);
            float allowed = length;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.transform == target || hit.transform.IsChildOf(target)) continue;
                allowed = Mathf.Min(allowed, Mathf.Max(.2f, hit.distance - .08f));
            }
            return focus + direction * allowed;
        }
    }
}
