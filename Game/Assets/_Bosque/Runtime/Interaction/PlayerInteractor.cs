using UnityEngine;
using UnityEngine.InputSystem;

namespace Bosque.Interaction
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float maxDistance = 2.5f;
        [SerializeField] private LayerMask targetLayers = ~0;
        private readonly Collider[] candidates = new Collider[128];
        private readonly RaycastHit[] sightHits = new RaycastHit[128];
        public InteractionTarget Target { get; private set; }
        public Vector3 Origin => transform.position + Vector3.up * .9f;
        public float MaxDistance => maxDistance;
        public bool CanReach(InteractionTarget target, Collider shape) =>
            isActiveAndEnabled && shape != null && shape.enabled &&
            (shape.ClosestPoint(Origin) - Origin).sqrMagnitude <= maxDistance * maxDistance &&
            HasLineOfSight(target, shape.ClosestPoint(Origin));
        public string Prompt => Target != null ? Target.Prompt : string.Empty;
        private void Update()
        {
            Scan();
            if (Application.isFocused && Cursor.lockState == CursorLockMode.Locked &&
                Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) TryInteract();
        }
        private void OnDisable() => ClearTarget();
        public void ClearTarget() => Target = null;
        public void Scan()
        {
            Target = null;
            float nearest = maxDistance * maxDistance;
            int count = Physics.OverlapSphereNonAlloc(Origin, maxDistance, candidates, targetLayers,
                QueryTriggerInteraction.Ignore);
            // Fail closed if a future dense scene exceeds the fixed query capacity.
            if (count == candidates.Length) return;
            for (int i = 0; i < count; i++)
            {
                var collider = candidates[i];
                var target = collider.GetComponentInParent<InteractionTarget>();
                if (target == null || !target.isActiveAndEnabled || !target.CanInteract(this)) continue;
                var point = collider.ClosestPoint(Origin);
                float distance = (point - Origin).sqrMagnitude;
                if (distance > nearest || !HasLineOfSight(target, point)) continue;
                nearest = distance; Target = target;
            }
        }
        private bool HasLineOfSight(InteractionTarget target, Vector3 point)
        {
            var offset = point - Origin;
            if (offset.magnitude < .001f) return true;
            int count = Physics.RaycastNonAlloc(Origin, offset.normalized, sightHits,
                offset.magnitude + .02f, ~0, QueryTriggerInteraction.Ignore);
            if (count == sightHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var hit = sightHits[i];
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                if (hit.collider.GetComponentInParent<InteractionTarget>() == target) continue;
                if (hit.distance < offset.magnitude - .01f) return false;
            }
            return true;
        }
        public bool TryInteract()
        {
            if (!isActiveAndEnabled) { ClearTarget(); return false; }
            Scan(); // Never execute a cached target after moving or changing availability.
            if (Target == null) return false;
            bool result = Target.TryInteract(this);
            Scan();
            return result;
        }
    }
}
