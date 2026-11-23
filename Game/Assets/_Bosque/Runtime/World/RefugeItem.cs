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
            // V16: el eje largo apunta hacia delante (como se lleva un tronco al costado) y el centro visible queda en el
            // ancla, junto a la cadera: nada atraviesa la cabeza ni el cuerpo.
            transform.SetParent(anchor, false);
            transform.localRotation = Quaternion.identity; transform.localPosition = Vector3.zero;
            var b = VisualBounds();
            // Eje más largo → hacia delante (Z del ancla): X se gira en yaw, Y se tumba (pitch).
            if (b.size.y >= b.size.x && b.size.y > b.size.z) { transform.localRotation = Quaternion.Euler(90f, 0, 0); b = VisualBounds(); }
            else if (b.size.x > b.size.z) { transform.localRotation = Quaternion.Euler(0, 90f, 0); b = VisualBounds(); }
            transform.position -= b.center - anchor.position;
        }

        private Bounds VisualBounds()
        {
            var rs = GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(transform.position, Vector3.zero);
            var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
            // Ejes del ancla (el portador puede estar girado): se mide en su espacio local.
            var a = transform.parent;
            if (a == null) return bb;
            var c = a.InverseTransformPoint(bb.center);
            var lb = new Bounds(c, Vector3.zero);
            foreach (var r in rs)
            {
                var e = r.bounds.extents; var ctr = r.bounds.center;
                for (int i = 0; i < 8; i++)
                    lb.Encapsulate(a.InverseTransformPoint(ctr + Vector3.Scale(e, new Vector3((i & 1) * 2 - 1, (i >> 1 & 1) * 2 - 1, (i >> 2 & 1) * 2 - 1))));
            }
            return new Bounds(a.TransformPoint(lb.center), lb.size);
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
