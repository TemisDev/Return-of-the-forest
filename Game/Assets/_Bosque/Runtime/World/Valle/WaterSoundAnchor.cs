using UnityEngine;

namespace Bosque.World
{
    /// <summary>Lleva el emisor del sonido del río al punto de agua más cercano a la cámara.</summary>
    public sealed class WaterSoundAnchor : MonoBehaviour
    {
        [SerializeField] private ValleWater water;
        private float next;

        public void Configure(ValleWater w) => water = w;

        private void Update()
        {
            if (water == null || Time.unscaledTime < next) return;
            next = Time.unscaledTime + .2f;
            var cam = Camera.main;
            if (cam != null) transform.position = water.NearestWaterPoint(cam.transform.position);
        }
    }
}
