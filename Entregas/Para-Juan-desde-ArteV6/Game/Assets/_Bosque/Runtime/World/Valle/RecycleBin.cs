using System;
using System.Collections.Generic;
using Bosque.Interaction;
using UnityEngine;

namespace Bosque.World
{
    /// <summary>Contenedor de un punto de reciclaje. E deposita los residuos de su tipo que lleve la bolsa.</summary>
    public sealed class RecycleBin : InteractionTarget
    {
        [SerializeField] private WasteKind kind;
        [SerializeField] private Collider shape;
        [SerializeField] private string station = "Campamento";
        private string prompt = "";

        public WasteKind Kind => kind;
        public string Station => station;
        public override string Prompt => prompt;
        public event Action<RecycleBin, List<LitterItem>> Deposited;
        public event Action<RecycleBin, string> Rejected;

        public void Configure(WasteKind wasteKind, Collider col, string stationName) { kind = wasteKind; shape = col; station = stationName; }

        public override bool CanInteract(PlayerInteractor actor)
        {
            if (actor == null || !isActiveAndEnabled || shape == null || !shape.enabled) return false;
            var bag = actor.GetComponent<LitterBag>();
            if (bag == null || bag.IsEmpty) return false;
            int n = bag.CountOf(kind);
            prompt = n > 0 ? $"Depositar {WasteKinds.Name(kind).ToLowerInvariant()} ({n})" : "Contenedor de " + WasteKinds.Name(kind).ToLowerInvariant();
            return true;
        }

        public override bool TryInteract(PlayerInteractor actor)
        {
            if (!CanInteract(actor) || !actor.CanReach(this, shape)) return false;
            var bag = actor.GetComponent<LitterBag>();
            var taken = bag.TakeAll(kind);
            if (taken.Count == 0)
            {
                Rejected?.Invoke(this, $"No llevas residuos de {WasteKinds.Name(kind).ToLowerInvariant()}. En la bolsa: {bag.Summary()}. Mira el color y el rótulo de cada contenedor.");
                return false;
            }
            Deposited?.Invoke(this, taken);
            return true;
        }
    }
}
