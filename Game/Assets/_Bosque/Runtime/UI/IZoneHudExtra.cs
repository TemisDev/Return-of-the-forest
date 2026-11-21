using System.Collections.Generic;
using UnityEngine;

namespace Bosque.UI
{
    /// <summary>Datos adicionales del HUD para zonas grandes: tarjeta lateral, barra de trabajo y línea del final.</summary>
    public interface IZoneHudExtra
    {
        string SideTitle { get; }
        /// <summary>Texto con formato enriquecido de TextMeshPro (bolsa, sectores, cronómetro…).</summary>
        string SideText { get; }
        /// <summary>Progreso 0..1 de una acción mantenida (apartar un tronco); negativo si no hay ninguna.</summary>
        float WorkProgress { get; }
        string EndingExtra { get; }
    }

    /// <summary>Qué representa un marcador: decide cómo y dónde se dibuja (mapa, minimapa, brújula).</summary>
    public enum MarkerKind { Point, Station, Litter, Wood, Label, Bridge, Ford }

    public struct MapMarker
    {
        public Vector3 position; public string label; public Color color; public bool big;
        public MarkerKind kind;
    }

    /// <summary>Mapa de la zona (tecla M): imagen cenital, área del mundo que cubre y marcadores.</summary>
    public interface IZoneMap
    {
        Texture MapTexture { get; }
        /// <summary>Rectángulo del mundo (x, z, ancho, alto) que cubre la imagen.</summary>
        Rect MapWorldRect { get; }
        Transform MapPlayer { get; }
        IEnumerable<MapMarker> MapMarkers { get; }
    }
}
