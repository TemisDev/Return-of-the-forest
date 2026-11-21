using System;
using System.Linq;
using UnityEngine;

namespace Bosque.World
{
    /// <summary>Uno de los cuatro sectores del valle: limpio cuando no queda ningún residuo ni tronco caído.</summary>
    public sealed class ValleSector : MonoBehaviour
    {
        [SerializeField] private int index;
        [SerializeField] private string sectorName = "Sector";
        [SerializeField] private LitterItem[] litter = new LitterItem[0];
        [SerializeField] private FallenWood[] wood = new FallenWood[0];
        [SerializeField] private GameObject restoredDecor;

        public int Index => index;
        public string SectorName => sectorName;
        public LitterItem[] Litter => litter;
        public FallenWood[] Wood => wood;
        public int Total => litter.Length + wood.Length;
        public int Cleaned => litter.Count(l => l.Collected) + wood.Count(w => w.Cleared);
        public bool IsClean => Cleaned == Total;
        public GameObject RestoredDecor => restoredDecor;

        public void Configure(int i, string name, LitterItem[] items, FallenWood[] logs, GameObject decor)
        {
            index = i; sectorName = name; litter = items; wood = logs; restoredDecor = decor;
            if (restoredDecor != null) restoredDecor.SetActive(false);
        }
    }
}
