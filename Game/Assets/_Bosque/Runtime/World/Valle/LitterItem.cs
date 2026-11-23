using System;
using Bosque.Interaction;
using UnityEngine;

namespace Bosque.World
{
    /// <summary>Tipos de residuo según el contenedor donde se deposita (colores de la norma peruana NTP 900.058).</summary>
    public enum WasteKind { Plastico = 0, VidrioMetal = 1, PapelCarton = 2, NoAprovechable = 3 }

    public static class WasteKinds
    {
        public static readonly WasteKind[] All = { WasteKind.Plastico, WasteKind.VidrioMetal, WasteKind.PapelCarton, WasteKind.NoAprovechable };
        public static string Name(WasteKind k) => k switch
        {
            WasteKind.Plastico => "Plástico",
            WasteKind.VidrioMetal => "Vidrio y metal",
            WasteKind.PapelCarton => "Papel y cartón",
            _ => "No aprovechable",
        };
        public static Color BinColor(WasteKind k) => k switch
        {
            WasteKind.Plastico => new Color(.92f, .92f, .9f),
            WasteKind.VidrioMetal => new Color(.95f, .76f, .15f),
            WasteKind.PapelCarton => new Color(.16f, .38f, .78f),
            _ => new Color(.12f, .12f, .12f),
        };
    }

    /// <summary>
    /// Residuo del valle. Se recoge con E si cabe en la bolsa (las llantas ocupan 3 espacios). Recogido desaparece del
    /// suelo; cuenta como limpio para su sector y debe depositarse después en el contenedor correcto.
    /// </summary>
    public sealed class LitterItem : InteractionTarget
    {
        [SerializeField] private string itemId;
        [SerializeField] private string label = "residuo";
        [SerializeField] private WasteKind kind;
        [SerializeField, Min(1)] private int slots = 1;
        [SerializeField] private int sector;
        [SerializeField] private Collider shape;

        public string ItemId => itemId;
        public string Label => label;
        public WasteKind Kind => kind;
        public int Slots => slots;
        public int Sector => sector;
        public bool Collected { get; private set; }
        public override string Prompt => "Recoger " + label;
        /// <summary>Intento con la bolsa llena (texto de la pista).</summary>
        public event Action<LitterItem, string> Rejected;

        public void Configure(string id, string displayName, WasteKind wasteKind, int size, int sectorIndex, Collider col)
        {
            itemId = id; label = displayName; kind = wasteKind; slots = size; sector = sectorIndex; shape = col;
        }

        public override bool CanInteract(PlayerInteractor actor) =>
            actor != null && isActiveAndEnabled && !Collected && shape != null && shape.enabled && actor.GetComponent<LitterBag>() != null;

        public override bool TryInteract(PlayerInteractor actor)
        {
            if (!CanInteract(actor) || !actor.CanReach(this, shape)) return false;
            var bag = actor.GetComponent<LitterBag>();
            if (!bag.CanFit(this))
            {
                Rejected?.Invoke(this, slots > 1 && bag.Free > 0
                    ? $"La {label} ocupa {slots} espacios y solo quedan {bag.Free}. Vacía la bolsa en un punto de reciclaje."
                    : "La bolsa está llena. Lleva los residuos a un punto de reciclaje y sepáralos por tipo.");
                return false;
            }
            bag.Add(this);
            SetCollected(true);
            return true;
        }

        public void SetCollected(bool value)
        {
            Collected = value;
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = !value;
            if (shape != null) shape.enabled = !value;
        }
    }
}
