using UnityEngine;

namespace Bosque.Interaction
{
    public abstract class InteractionTarget : MonoBehaviour
    {
        public abstract string Prompt { get; }
        public abstract bool CanInteract(PlayerInteractor actor);
        public abstract bool TryInteract(PlayerInteractor actor);
    }
}
