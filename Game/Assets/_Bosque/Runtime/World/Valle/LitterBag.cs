using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bosque.World
{
    /// <summary>
    /// Bolsa de la Guardiana: capacidad limitada (espacios). Obliga a planificar viajes a los puntos de reciclaje.
    /// Cada residuo se deposita solo en el contenedor de su tipo.
    /// </summary>
    public sealed class LitterBag : MonoBehaviour
    {
        [SerializeField, Min(1)] private int capacity = 8;
        private readonly List<LitterItem> items = new List<LitterItem>();

        public int Capacity => capacity;
        public int Used => items.Sum(i => i.Slots);
        public int Free => capacity - Used;
        public int Count => items.Count;
        public bool IsEmpty => items.Count == 0;
        public IReadOnlyList<LitterItem> Items => items;
        public event Action Changed;

        public void Configure(int slots) => capacity = Mathf.Max(1, slots);
        public bool CanFit(LitterItem item) => item != null && !items.Contains(item) && item.Slots <= Free;
        public int CountOf(WasteKind kind) => items.Count(i => i.Kind == kind);

        public void Add(LitterItem item)
        {
            if (!CanFit(item)) return;
            items.Add(item); Changed?.Invoke();
        }

        /// <summary>Saca de la bolsa todos los residuos de ese tipo y los devuelve.</summary>
        public List<LitterItem> TakeAll(WasteKind kind)
        {
            var taken = items.Where(i => i.Kind == kind).ToList();
            if (taken.Count == 0) return taken;
            items.RemoveAll(i => i.Kind == kind);
            Changed?.Invoke();
            return taken;
        }

        public void Clear() { if (items.Count == 0) return; items.Clear(); Changed?.Invoke(); }

        public string Summary()
        {
            if (items.Count == 0) return "vacía";
            return string.Join(" · ", WasteKinds.All.Where(k => CountOf(k) > 0).Select(k => WasteKinds.Name(k) + " " + CountOf(k)));
        }
    }
}
