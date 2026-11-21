using System;
using UnityEngine;

namespace Bosque.World
{
    /// <summary>Tramo de río: polilínea cuya Y es la superficie del agua, con profundidad en el centro por punto.</summary>
    [Serializable]
    public sealed class WaterStream
    {
        public string name;
        public Vector3[] points;
        public float[] depth;
        /// <summary>Medio ancho del lecho plano y distancia a la que la orilla alcanza su altura final.</summary>
        public float flat = 2f, bank = 5f;
        public Vector3 labelPosition;
    }

    [Serializable]
    public sealed class WaterLake
    {
        public string name;
        public Vector3 center;
        public float radius, depth;
    }

    [Serializable]
    public sealed class WaterCrossing
    {
        public string name;
        public Vector3 position;
        public bool bridge;
    }

    /// <summary>
    /// Ríos y lagunas del valle. Una sola descripción sirve al constructor (talla el terreno y genera la malla de agua)
    /// y al juego (profundidad bajo la Guardiana, sonido del río, mapa).
    /// </summary>
    public sealed class ValleWater : MonoBehaviour
    {
        /// <summary>Altura de la orilla sobre la superficie del agua.</summary>
        public const float BankRise = .4f;

        [SerializeField] private WaterStream[] streams = new WaterStream[0];
        [SerializeField] private WaterLake[] lakes = new WaterLake[0];
        [SerializeField] private WaterCrossing[] crossings = new WaterCrossing[0];

        public WaterStream[] Streams => streams;
        public WaterLake[] Lakes => lakes;
        public WaterCrossing[] Crossings => crossings;

        public void Configure(WaterStream[] s, WaterLake[] l, WaterCrossing[] c) { streams = s; lakes = l; crossings = c; }

        /// <summary>Desplazamiento del lecho respecto a la superficie a una distancia lateral del centro del río.</summary>
        public static float StreamBed(float d, float depth, float flat, float bank) =>
            d <= flat ? -depth : d >= bank ? BankRise : Mathf.Lerp(-depth, BankRise, Mathf.SmoothStep(0, 1, (d - flat) / (bank - flat)));

        /// <summary>Desplazamiento del fondo de una laguna: hondo en el centro, playa poco profunda y orilla.</summary>
        public static float LakeBed(float r, float radius, float depth)
        {
            if (r >= radius + 3f) return BankRise;
            if (r >= radius) return Mathf.Lerp(0f, BankRise, (r - radius) / 3f);
            return -depth * (1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.45f, 1f, r / radius)));
        }

        /// <summary>Punto más cercano del eje de un río: distancia lateral, superficie, profundidad y dirección de la corriente.</summary>
        public static void Nearest(WaterStream s, Vector2 p, out float distance, out float surface, out float depth, out Vector2 flow, out int segment)
        {
            distance = float.MaxValue; surface = 0; depth = 0; flow = Vector2.up; segment = 0;
            for (int i = 1; i < s.points.Length; i++)
            {
                var a = new Vector2(s.points[i - 1].x, s.points[i - 1].z); var b = new Vector2(s.points[i].x, s.points[i].z);
                var ab = b - a; float len2 = ab.sqrMagnitude; if (len2 < 1e-6f) continue;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                float d = Vector2.Distance(p, a + ab * t);
                if (d >= distance) continue;
                distance = d; segment = i - 1; flow = ab.normalized;
                surface = Mathf.Lerp(s.points[i - 1].y, s.points[i].y, t);
                depth = Mathf.Lerp(s.depth[i - 1], s.depth[i], t);
            }
        }

        /// <summary>
        /// Superficie del agua en (x, z) si el punto está dentro de un cauce o laguna (hasta la orilla).
        /// La profundidad real es <c>surface - altura del suelo</c>.
        /// </summary>
        public bool SurfaceAt(Vector3 world, out float surface)
        {
            surface = float.MinValue; float best = float.MaxValue;
            var p = new Vector2(world.x, world.z);
            foreach (var l in lakes)
            {
                float r = Vector2.Distance(p, new Vector2(l.center.x, l.center.z)), reach = l.radius + 3f;
                if (r < reach && r / reach < best) { best = r / reach; surface = l.center.y; }
            }
            foreach (var s in streams)
            {
                Nearest(s, p, out float d, out float sy, out _, out _, out _);
                if (d < s.bank && d / s.bank < best) { best = d / s.bank; surface = sy; }
            }
            return best < float.MaxValue;
        }

        /// <summary>Profundidad del agua sobre una posición de pies (0 si está seco).</summary>
        public float DepthAt(Vector3 feet) => SurfaceAt(feet, out float s) ? Mathf.Max(0f, s - feet.y) : 0f;

        /// <summary>Punto más cercano sobre el eje de cualquier río o el centro de una laguna (para el sonido).</summary>
        public Vector3 NearestWaterPoint(Vector3 world)
        {
            var p = new Vector2(world.x, world.z); float best = float.MaxValue; var result = world;
            foreach (var s in streams)
                for (int i = 1; i < s.points.Length; i++)
                {
                    var a = s.points[i - 1]; var b = s.points[i];
                    var a2 = new Vector2(a.x, a.z); var ab = new Vector2(b.x, b.z) - a2;
                    float t = Mathf.Clamp01(Vector2.Dot(p - a2, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                    var q = Vector3.Lerp(a, b, t); float d = (new Vector2(q.x, q.z) - p).sqrMagnitude;
                    if (d < best) { best = d; result = q; }
                }
            foreach (var l in lakes)
            {
                var c = new Vector2(l.center.x, l.center.z); var dir = p - c;
                var q = dir.magnitude > l.radius ? c + dir.normalized * l.radius : p;
                float d = (q - p).sqrMagnitude;
                if (d < best) { best = d; result = new Vector3(q.x, l.center.y, q.y); }
            }
            return result;
        }
    }
}
