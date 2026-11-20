using System;
using Bosque.Interaction;
using Bosque.Player;
using UnityEngine;

namespace Bosque.Character
{
    /// <summary>
    /// Traduce el estado real del motor (velocidad, suelo, salto) a parámetros del Animator.
    /// Solo lee: no mueve al personaje ni cambia el progreso. El movimiento sigue siendo del CharacterController
    /// (sin root motion). También detecta los apoyos de los pies para los sonidos de pasos.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class CharacterAnimationDriver : MonoBehaviour
    {
        public static readonly int SpeedId = Animator.StringToHash("Speed");
        public static readonly int GroundedId = Animator.StringToHash("Grounded");
        public static readonly int FallingId = Animator.StringToHash("Falling");
        public static readonly int VerticalSpeedId = Animator.StringToHash("VerticalSpeed");
        public static readonly int JumpId = Animator.StringToHash("Jump");
        public static readonly int InteractId = Animator.StringToHash("Interact");
        public static readonly int RestoreId = Animator.StringToHash("Restore");

        [SerializeField] private ThirdPersonMotor motor;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField, Tooltip("Tiempo en el aire antes de pasar a la pose de caída (evita parpadeos en bordillos).")]
        private float fallDelay = .18f;
        [SerializeField] private float speedDamp = .08f;
        [SerializeField, Tooltip("Altura máxima del pie (m) sobre el suelo para considerar un apoyo.")]
        private float footContactHeight = .14f;

        private Animator animator;
        private float airTime;
        private Transform leftFoot, rightFoot;
        private float leftPrevY, rightPrevY, leftLast, rightLast;
        private bool leftDown, rightDown;

        /// <summary>Apoyo de un pie: (esIzquierdo, intensidad 0..1 según la velocidad).</summary>
        public event Action<bool, float> Footstep;
        public Animator Animator => animator;
        public ThirdPersonMotor Motor => motor;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            animator.applyRootMotion = false;
            if (motor == null) motor = GetComponentInParent<ThirdPersonMotor>();
            if (interactor == null) interactor = GetComponentInParent<PlayerInteractor>();
            if (animator.isHuman)
            {
                leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            }
        }

        private void OnEnable()
        {
            if (motor != null) { motor.Jumped += OnJumped; motor.Respawned += OnRespawned; }
            if (interactor != null) interactor.Interacted += OnInteracted;
        }

        private void OnDisable()
        {
            if (motor != null) { motor.Jumped -= OnJumped; motor.Respawned -= OnRespawned; }
            if (interactor != null) interactor.Interacted -= OnInteracted;
        }

        private void OnJumped() => animator.SetTrigger(JumpId);
        private void OnInteracted(InteractionTarget target) => animator.SetTrigger(InteractId);
        private void OnRespawned() { airTime = 0; animator.ResetTrigger(JumpId); }

        /// <summary>Gesto de restauración (lo llama el adaptador de la zona al restaurarse el entorno).</summary>
        public void PlayRestore() => animator.SetTrigger(RestoreId);

        /// <summary>Vuelve a reposo sin transiciones (reinicio de la zona).</summary>
        public void ResetPose()
        {
            animator.ResetTrigger(JumpId); animator.ResetTrigger(InteractId); animator.ResetTrigger(RestoreId);
            animator.SetFloat(SpeedId, 0); animator.SetBool(GroundedId, true); animator.SetBool(FallingId, false);
            airTime = 0;
        }

        private void Update()
        {
            if (motor == null) return;
            bool simulated = Time.time - motor.LastStepTime < .15f;
            float speed = simulated ? motor.HorizontalSpeed : 0f;
            bool grounded = !simulated || motor.IsGrounded;
            airTime = grounded ? 0f : airTime + Time.deltaTime;
            animator.SetFloat(SpeedId, speed, speedDamp, Time.deltaTime);
            animator.SetBool(GroundedId, grounded);
            animator.SetBool(FallingId, airTime > fallDelay);
            animator.SetFloat(VerticalSpeedId, simulated ? motor.VerticalSpeed : 0f);
        }

        private void LateUpdate()
        {
            if (leftFoot == null || rightFoot == null || motor == null) return;
            float speed = motor.HorizontalSpeed;
            bool moving = motor.IsGrounded && speed > .4f && Time.time - motor.LastStepTime < .15f;
            float baseY = transform.position.y;
            Contact(leftFoot, ref leftPrevY, ref leftDown, ref leftLast, true, baseY, moving, speed);
            Contact(rightFoot, ref rightPrevY, ref rightDown, ref rightLast, false, baseY, moving, speed);
        }

        private void Contact(Transform foot, ref float prevY, ref bool down, ref float last, bool left, float baseY, bool moving, float speed)
        {
            float y = foot.position.y - baseY;
            bool falling = y < prevY - .0005f;
            if (!down && moving && y < footContactHeight && !falling && prevY > y - .002f && Time.time - last > .2f)
            {
                down = true; last = Time.time;
                Footstep?.Invoke(left, Mathf.Clamp01(speed / Mathf.Max(.1f, motor.RunSpeed)));
            }
            if (y > footContactHeight + .04f) down = false;
            prevY = y;
        }
    }
}
