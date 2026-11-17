using Bosque.Interaction;
using Bosque.Session;
using UnityEngine;

namespace Bosque.World
{
    public sealed class RemovableObstacle : InteractionTarget
    {
        [SerializeField] private string obstacleId;
        [SerializeField] private Collider interactionCollider;
        [SerializeField] private string prompt = "Retirar obstáculo";
        private ArroyoSession session;
        public string ObstacleId => obstacleId;
        public override string Prompt => prompt;
        public void Configure(string id, Collider shape) { obstacleId = id; interactionCollider = shape; }
        public void Bind(ArroyoSession owner) => session = owner;
        public override bool CanInteract(PlayerInteractor actor)
        {
            if (actor == null || !isActiveAndEnabled || interactionCollider == null ||
                !interactionCollider.enabled || session == null || session.Progress == null ||
                session.Progress.IsCompleted || session.Progress.IsRemoved(obstacleId)) return false;
            // I-01: never remove the surface supporting the player (including a jump above it).
            var bounds = interactionCollider.bounds;
            var character = actor.GetComponent<CharacterController>();
            float radius = character != null ? character.radius : .35f;
            float feet = character != null ? character.bounds.min.y : actor.transform.position.y;
            var position = actor.transform.position;
            bool above = position.x >= bounds.min.x - radius && position.x <= bounds.max.x + radius &&
                position.z >= bounds.min.z - radius && position.z <= bounds.max.z + radius &&
                feet >= bounds.max.y - .15f;
            return !above;
        }
        public override bool TryInteract(PlayerInteractor actor)
        {
            if (!CanInteract(actor) || !actor.CanReach(this, interactionCollider)) return false;
            return session.Progress.TryRemove(obstacleId);
        }
    }
}
