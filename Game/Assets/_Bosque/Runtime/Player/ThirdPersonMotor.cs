using System;
using UnityEngine;

namespace Bosque.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonMotor : MonoBehaviour
    {
        [SerializeField] private float speed = 4.5f;
        [SerializeField, Range(.1f, 1f)] private float walkFactor = .3f;
        [SerializeField] private float jumpHeight = 1.1f;
        [SerializeField] private float gravity = -24f;
        [SerializeField] private Transform respawn;
        private CharacterController controller;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private float verticalSpeed;
        private bool wasGrounded = true;
        // Margen de apoyo: CharacterController.isGrounded parpadea cuando casi no hay movimiento vertical en un frame
        // (FPS altos); sin este margen una pulsación de salto podía perderse estando quieto.
        private const float GroundGrace = .1f;
        private float groundTimer = GroundGrace;

        /// <summary>Velocidad realmente aplicada en el último paso (m/s). Solo lectura para animación y audio.</summary>
        public Vector3 Velocity { get; private set; }
        public float HorizontalSpeed => new Vector2(Velocity.x, Velocity.z).magnitude;
        public float VerticalSpeed => verticalSpeed;
        public bool IsGrounded => controller != null && (controller.isGrounded || groundTimer > 0f);
        public float RunSpeed => speed;
        /// <summary>Multiplicador de velocidad horizontal (p. ej., al cargar un elemento de refugio).</summary>
        public float SpeedFactor { get; set; } = 1f;
        /// <summary>Momento (Time.time) del último Simulate; si el motor deja de simularse, la animación vuelve a reposo.</summary>
        public float LastStepTime { get; private set; } = -1f;
        public event Action Jumped;
        /// <summary>Aterrizaje tras estar en el aire; el valor es la velocidad vertical de impacto (positiva).</summary>
        public event Action<float> Landed;
        public event Action Respawned;

        private void Awake() => Initialize();
        private void Initialize()
        {
            if (controller != null) return;
            controller = GetComponent<CharacterController>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }
        public void SetRespawn(Transform point) => respawn = point;
        public void Simulate(Vector2 input, bool jump, float yaw, float dt) => Simulate(input, jump, yaw, dt, false);
        public void Simulate(Vector2 input, bool jump, float yaw, float dt, bool walk)
        {
            if (dt <= 0f) return;
            Initialize();
            if (transform.position.y < -5f) { Respawn(); return; }
            dt = Mathf.Min(dt, .05f);
            var direction = Quaternion.Euler(0, yaw, 0) * new Vector3(input.x, 0, input.y);
            direction = Vector3.ClampMagnitude(direction, 1f);
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2f;
            bool jumped = false;
            if (jump && (controller.isGrounded || groundTimer > 0f) && verticalSpeed <= 0f)
            {
                verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumped = true; groundTimer = 0f;
            }
            float impact = -verticalSpeed;
            verticalSpeed += gravity * dt;
            var planar = direction * speed * SpeedFactor * (walk ? walkFactor : 1f);
            var before = transform.position;
            controller.Move((planar + Vector3.up * verticalSpeed) * dt);
            Velocity = (transform.position - before) / dt;
            LastStepTime = Time.time;
            if (direction.sqrMagnitude > .001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(direction), 720f * dt);
            if (jumped) Jumped?.Invoke();
            if (controller.isGrounded) groundTimer = GroundGrace; else groundTimer -= dt;
            bool grounded = IsGrounded;
            if (grounded && !wasGrounded) Landed?.Invoke(Mathf.Max(0f, impact));
            wasGrounded = grounded;
            if (transform.position.y < -5f) Respawn();
        }
        private void Respawn()
        {
            Teleport(respawn != null ? respawn.position : initialPosition,
                respawn != null ? respawn.rotation : initialRotation);
            Respawned?.Invoke();
        }
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            Initialize();
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            verticalSpeed = 0;
            Velocity = Vector3.zero;
            wasGrounded = true; groundTimer = GroundGrace;
            controller.enabled = true;
        }
    }
}
