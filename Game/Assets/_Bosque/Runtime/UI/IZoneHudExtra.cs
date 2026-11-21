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
    public enum MarkerKind { Point, Station, Litter, Wood, Label, Bridge, Ford, Region }

    public struct MapMarker
    {
        public Vector3 position; public string label; public Color color; public bool big;
        public MarkerKind kind;
        /// <summary>Giro en grados sobre el mapa (antihorario desde el este): nombres de ríos, puentes y vados.</summary>
        public float angle;
    }

    /// <summary>Avance de un sector para el panel del mapa.</summary>
    public struct MapSector
    {
        public string name; public int cleaned, total, hidden; public Color color;
    }

    /// <summary>Datos opcionales del panel lateral del mapa (título, subtítulo y sectores).</summary>
    public interface IZoneMapInfo
    {
        string MapTitle { get; }
        string MapSubtitle { get; }
        IEnumerable<MapSector> MapSectors { get; }
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
