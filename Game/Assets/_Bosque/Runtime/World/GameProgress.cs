using System;
using System.Collections.Generic;

namespace Bosque.World
{
    /// <summary>
    /// Progreso de la partida entre zonas (solo en memoria: el GDD no contempla guardado persistente).
    /// Completar el arroyo concede la habilidad de crecimiento; el final exige las cuatro zonas.
    /// </summary>
    public static class GameProgress
    {
        public const string Arroyo = "arroyo", Claro = "claro", Refugio = "refugio", Valle = "valle";
        private static readonly HashSet<string> completed = new HashSet<string>(StringComparer.Ordinal);

        public static bool GrowthUnlocked { get; private set; }
        public static event Action Changed;
        public static int CompletedCount => completed.Count;
        public static bool IsZoneComplete(string zone) => zone != null && completed.Contains(zone);

        public static void CompleteZone(string zone)
        {
            if (string.IsNullOrEmpty(zone) || !completed.Add(zone)) return;
            if (zone == Arroyo) GrowthUnlocked = true;
            Changed?.Invoke();
        }

        public static void UnlockGrowth()
        {
            if (GrowthUnlocked) return;
            GrowthUnlocked = true;
            Changed?.Invoke();
        }

        /// <summary>Nueva partida (o pruebas).</summary>
        public static void ResetAll()
        {
            completed.Clear(); GrowthUnlocked = false;
            Changed?.Invoke();
        }
    }
}
