using UnityEngine;

namespace Bosque.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonMotor : MonoBehaviour
    {
        [SerializeField] private float speed = 4.5f;
        [SerializeField] private float jumpHeight = 1.1f;
        [SerializeField] private float gravity = -24f;
        [SerializeField] private Transform respawn;
        private CharacterController controller;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private float verticalSpeed;
        private void Awake() => Initialize();
        private void Initialize()
        {
            if (controller != null) return;
            controller = GetComponent<CharacterController>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }
        public void SetRespawn(Transform point) => respawn = point;
        public void Simulate(Vector2 input, bool jump, float yaw, float dt)
        {
            if (dt <= 0f) return;
            Initialize();
            if (transform.position.y < -5f) { Respawn(); return; }
            dt = Mathf.Min(dt, .05f);
            var direction = Quaternion.Euler(0, yaw, 0) * new Vector3(input.x, 0, input.y);
            direction = Vector3.ClampMagnitude(direction, 1f);
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2f;
            if (jump && controller.isGrounded)
                verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalSpeed += gravity * dt;
            controller.Move((direction * speed + Vector3.up * verticalSpeed) * dt);
            if (direction.sqrMagnitude > .001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(direction), 720f * dt);
            if (transform.position.y < -5f) Respawn();
        }
        private void Respawn()
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(respawn != null ? respawn.position : initialPosition,
                respawn != null ? respawn.rotation : initialRotation);
            verticalSpeed = 0;
            controller.enabled = true;
        }
    }
}
