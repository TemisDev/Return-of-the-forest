using System;
using Bosque.Player;
using Bosque.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bosque.Interaction
{
    /// <summary>
    /// Permite a la Guardiana llevar un elemento de refugio (uno a la vez). El elemento flota junto a su hombro
    /// mientras lo guía. E sin objetivo lo deja en el suelo; caer al vacío lo devuelve a su sitio original.
    /// Recoger y colocar pasan por PlayerInteractor (RefugeItem y RefugeSpot son objetivos normales).
    /// </summary>
    [RequireComponent(typeof(PlayerInteractor))]
    public sealed class ItemCarrier : MonoBehaviour
    {
        [SerializeField] private Vector3 holdOffset = new Vector3(.66f, 1.0f, .35f);   // costado derecho, a la altura de la cadera (V16)
        [SerializeField, Range(.3f, 1f)] private float carrySpeedFactor = .8f;
        [SerializeField] private float dropDistance = 1f;

        private Transform anchor;
        private PlayerInteractor interactor;
        private ThirdPersonMotor motor;
        private int pickFrame = -1;
        private readonly RaycastHit[] hits = new RaycastHit[16];

        public RefugeItem Holding { get; private set; }
        public Transform Anchor => anchor;
        public event Action<RefugeItem> PickedUp;
        public event Action<RefugeItem> Dropped;

        private void Awake() => Initialize();

        private void Initialize()
        {
            if (anchor != null) return;
            interactor = GetComponent<PlayerInteractor>();
            motor = GetComponent<ThirdPersonMotor>();
            anchor = new GameObject("CargaRefugio").transform;
            anchor.SetParent(transform, false);
            anchor.localPosition = holdOffset;
            if (motor != null) motor.Respawned += OnRespawned;
        }

        private void OnDestroy() { if (motor != null) motor.Respawned -= OnRespawned; }

        private void Update()
        {
            if (Holding != null)
                anchor.localPosition = holdOffset + Vector3.up * (Mathf.Sin(Time.time * 2.2f) * .04f);
            if (Holding == null || Time.frameCount == pickFrame || !Application.isFocused ||
                Cursor.lockState != CursorLockMode.Locked || Keyboard.current == null ||
                !Keyboard.current.eKey.wasPressedThisFrame) return;
            interactor.Scan();
            if (interactor.Target == null) Drop();
        }

        /// <summary>Lo llama RefugeItem al aceptar la interacción.</summary>
        public bool Pick(RefugeItem item)
        {
            Initialize();
            if (item == null || Holding != null || !isActiveAndEnabled) return false;
            Holding = item; pickFrame = Time.frameCount;
            item.AttachTo(this, anchor);
            if (motor != null) motor.SpeedFactor = carrySpeedFactor;
            PickedUp?.Invoke(item);
            return true;
        }

        /// <summary>Suelta el elemento delante de la Guardiana, apoyado en el suelo.</summary>
        public bool Drop()
        {
            if (Holding == null) return false;
            var item = Holding;
            var forward = transform.forward; forward.y = 0; forward = forward.sqrMagnitude > .001f ? forward.normalized : Vector3.forward;
            var start = transform.position + forward * dropDistance + Vector3.up * 1.5f;
            float ground = transform.position.y;
            int n = Physics.RaycastNonAlloc(start, Vector3.down, hits, 4f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                if (hits[i].transform.IsChildOf(transform) || hits[i].transform.IsChildOf(item.transform)) continue;
                if (hits[i].distance < best) { best = hits[i].distance; ground = hits[i].point.y; }
            }
            Release(item);
            item.DropAt(new Vector3(start.x, ground, start.z), Quaternion.LookRotation(forward));
            Dropped?.Invoke(item);
            return true;
        }

        /// <summary>Deja de llevar el elemento sin moverlo (al colocarlo o reiniciar).</summary>
        public void Release(RefugeItem item)
        {
            if (item == null || Holding != item) return;
            Holding = null;
            if (motor != null) motor.SpeedFactor = 1f;
        }

        private void OnRespawned()
        {
            if (Holding != null) Holding.ResetItem();
        }
    }
}
