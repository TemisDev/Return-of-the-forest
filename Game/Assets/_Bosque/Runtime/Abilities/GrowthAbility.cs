using System;
using Bosque.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bosque.Abilities
{
    /// <summary>
    /// Habilidad de crecimiento (tecla Q). Se desbloquea al completar el arroyo (GameProgress) y actúa solo sobre
    /// el GrowthPoint más cercano dentro del alcance. No crea plataformas libres: la geometría la define la zona.
    /// </summary>
    public sealed class GrowthAbility : MonoBehaviour
    {
        [SerializeField, Min(.5f)] private float range = 2.6f;
        [SerializeField] private float cooldown = .6f;
        [SerializeField, Tooltip("Solo para escenas de prueba: permite usarla sin haber completado el arroyo.")]
        private bool unlockedForTesting;

        private readonly Collider[] hits = new Collider[64];
        private float lastUse = -10f;

        public GrowthPoint Target { get; private set; }
        public bool Unlocked => unlockedForTesting || GameProgress.GrowthUnlocked;
        public float Range => range;
        /// <summary>Crecimiento aceptado.</summary>
        public event Action<GrowthPoint> Grew;
        /// <summary>Intento en suelo no apto: el texto es la pista para el jugador.</summary>
        public event Action<GrowthPoint, string> Rejected;

        public void UnlockForTesting(bool value) => unlockedForTesting = value;
        public Vector3 Origin => transform.position + Vector3.up * .5f;

        private void Update()
        {
            Scan();
            var kb = Keyboard.current;
            if (kb != null && kb.qKey.wasPressedThisFrame && Application.isFocused && Cursor.lockState == CursorLockMode.Locked)
                TryGrow();
        }

        /// <summary>Busca el punto de crecimiento pendiente más cercano dentro del alcance.</summary>
        public void Scan()
        {
            Target = null;
            if (!Unlocked) return;
            int n = Physics.OverlapSphereNonAlloc(Origin, range, hits, ~0, QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var p = hits[i].GetComponentInParent<GrowthPoint>();
                if (p == null || p.Grown || !p.isActiveAndEnabled) continue;
                float d = (p.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; Target = p; }
            }
        }

        /// <summary>Usa la habilidad sobre el objetivo actual. Pulsaciones repetidas no duplican el crecimiento.</summary>
        public bool TryGrow()
        {
            if (!isActiveAndEnabled || !Unlocked || Time.time - lastUse < cooldown) return false;
            Scan();
            if (Target == null) return false;
            lastUse = Time.time;
            var point = Target;
            if (!point.Suitable) { Rejected?.Invoke(point, point.Hint); return false; }
            if (!point.TryGrow()) return false;
            Grew?.Invoke(point);
            Scan();
            return true;
        }
    }
}
