using Bosque.Interaction;
using UnityEngine;

namespace Bosque.World
{
    /// <summary>
    /// Elemento de refugio de la zona 3 (tronco hueco, piedras...). Objeto de misión local con estados simples:
    /// en su sitio, llevado por la Guardiana, en el suelo o colocado en un lugar de refugio.
    /// </summary>
    public sealed class RefugeItem : InteractionTarget
    {
        [SerializeField] private string itemId;
        [SerializeField] private string label = "elemento";
        [SerializeField] private string sfxCue = "remove.log";
        [SerializeField] private Collider shape;
        [SerializeField] private Vector3 homePosition;
        [SerializeField] private Quaternion homeRotation = Quaternion.identity;
        [SerializeField] private Transform homeParent;

        private ItemCarrier carrier;
        public string ItemId => itemId;
        public string Label => label;
        public string SfxCue => sfxCue;
        public RefugeSpot PlacedAt { get; private set; }
        public bool Carried => carrier != null;
        public bool AtHome => carrier == null && PlacedAt == null &&
            (transform.position - homePosition).sqrMagnitude < .0001f;
        public override string Prompt => "Recoger " + label;

        public void Configure(string id, string displayName, string cue, Collider interactionShape)
        {
            itemId = id; label = displayName; sfxCue = cue; shape = interactionShape;
            homeParent = transform.parent; homePosition = transform.position; homeRotation = transform.rotation;
        }

        public override bool CanInteract(PlayerInteractor actor)
        {
            if (actor == null || !isActiveAndEnabled || shape == null || !shape.enabled || PlacedAt != null || carrier != null) return false;
            var c = actor.GetComponent<ItemCarrier>();
            return c != null && c.isActiveAndEnabled && c.Holding == null;
        }

        public override bool TryInteract(PlayerInteractor actor)
        {
            if (!CanInteract(actor) || !actor.CanReach(this, shape)) return false;
            return actor.GetComponent<ItemCarrier>().Pick(this);
        }

        internal void AttachTo(ItemCarrier owner, Transform anchor)
        {
            carrier = owner;
            if (shape != null) shape.enabled = false;
            transform.SetParent(anchor, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(0, 90f, 0);
        }

        internal void DropAt(Vector3 position, Quaternion rotation)
        {
            carrier = null;
            transform.SetParent(homeParent, true);
            transform.SetPositionAndRotation(position, rotation);
            if (shape != null) shape.enabled = true;
        }

        internal void PlaceAt(RefugeSpot spot, Transform pose)
        {
            carrier = null; PlacedAt = spot;
            transform.SetParent(spot.transform, true);
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            if (shape != null) shape.enabled = true;
        }

        /// <summary>Vuelve a su sitio original (reinicio o caída al vacío mientras se lleva).</summary>
        public void ResetItem()
        {
            if (carrier != null) carrier.Release(this);
            carrier = null; PlacedAt = null;
            transform.SetParent(homeParent, false);
            transform.SetPositionAndRotation(homePosition, homeRotation);
            if (shape != null) shape.enabled = true;
        }
    }
}
