using System;
using Bosque.Interaction;
using UnityEngine;

namespace Bosque.World
{
    /// <summary>
    /// Lugar indicado (anillo de piedras) donde puede ir un elemento de refugio. Solo sirve si está en el paso del
    /// animal, con sombra y cobertura; si no, devuelve una pista y la Guardiana conserva el elemento.
    /// </summary>
    public sealed class RefugeSpot : InteractionTarget
    {
        [SerializeField] private string spotId;
        [SerializeField] private bool suitable = true;
        [SerializeField, TextArea] private string hint = "Demasiado expuesto: aquí no hay sombra ni huellas.";
        [SerializeField] private Collider shape;
        [SerializeField] private Transform placement;

        private string heldLabel = "elemento";
        public string SpotId => spotId;
        public bool Suitable => suitable;
        public string Hint => hint;
        public RefugeItem Occupant { get; private set; }
        public Transform Placement => placement != null ? placement : transform;
        public override string Prompt => "Colocar " + heldLabel + " aquí";
        public event Action<RefugeSpot, RefugeItem> Placed;
        public event Action<RefugeSpot, string> Rejected;

        public void Configure(string id, bool isSuitable, string failureHint, Collider interactionShape, Transform pose)
        {
            spotId = id; suitable = isSuitable; hint = failureHint; shape = interactionShape; placement = pose;
        }

        public override bool CanInteract(PlayerInteractor actor)
        {
            if (actor == null || !isActiveAndEnabled || Occupant != null || shape == null || !shape.enabled) return false;
            var c = actor.GetComponent<ItemCarrier>();
            if (c == null || c.Holding == null) return false;
            heldLabel = c.Holding.Label;
            return true;
        }

        public override bool TryInteract(PlayerInteractor actor)
        {
            if (!CanInteract(actor) || !actor.CanReach(this, shape)) return false;
            var carrier = actor.GetComponent<ItemCarrier>();
            if (!suitable)
            {
                Rejected?.Invoke(this, hint);
                return false;
            }
            var item = carrier.Holding;
            carrier.Release(item);
            item.PlaceAt(this, Placement);
            Occupant = item;
            Placed?.Invoke(this, item);
            return true;
        }

        public void ResetSpot() => Occupant = null;
    }
}
