using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Bosque.ArtV6
{
    /// <summary>
    /// Medición de rendimiento del arte V6. Inactiva salvo que el ejecutable reciba "-v6-perf".
    /// Recorre la orilla con la cámara en ambos estados visuales y escribe tiempos por frame a JSON.
    /// Solo para la escena de muestra; no forma parte del juego normal.
    /// </summary>
    public sealed class BosqueV6PerfProbe : MonoBehaviour
    {
        public float secondsPerState = 20f;
        public float warmupSeconds = 2f;

        private void Awake()
        {
            var args = Environment.GetCommandLineArgs();
            if (!args.Contains("-v6-perf")) { Destroy(this); return; }
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            StartCoroutine(Run(args));
        }

        private static GameObject FindAny(string name) =>
            Resources.FindObjectsOfTypeAll<Transform>().Where(t => t.name == name && t.gameObject.scene.IsValid()).Select(t => t.gameObject).FirstOrDefault();

        private IEnumerator Run(string[] args)
        {
            int oi = Array.IndexOf(args, "-v6-perf-out");
            string output = oi >= 0 && oi + 1 < args.Length ? args[oi + 1] : Path.Combine(Application.persistentDataPath, "v6-perf.json");
            var cam = FindAny("GameplayCamera").GetComponent<Camera>();
            foreach (var mb in cam.GetComponents<MonoBehaviour>()) if (mb.GetType().Name == "ThirdPersonCamera") mb.enabled = false;
            var degraded = FindAny("DegradedVisuals"); var restored = FindAny("RestoredVisuals");
            var obstacles = new[] { "Obstacle_01", "Obstacle_02", "Obstacle_03" }.Select(FindAny).ToArray();
            var results = new List<string>();
            var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            float baseDistance = urp != null ? urp.shadowDistance : 0f; int baseCascades = urp != null ? urp.shadowCascadeCount : 0;
            // Fases intercaladas para reducir el sesgo térmico; "lite" = 2 cascadas a 30 m (solo en memoria, no se guarda).
            foreach (var (isRestored, lite) in new[] { (false, false), (true, false), (true, true), (false, true), (true, false) })
            {
                if (urp != null) { urp.shadowDistance = lite ? 30f : baseDistance; urp.shadowCascadeCount = lite ? 2 : baseCascades; }
                degraded.SetActive(!isRestored); restored.SetActive(isRestored);
                foreach (var o in obstacles) o.SetActive(!isRestored);
                var frames = new List<float>();
                float t0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t0 < secondsPerState + warmupSeconds)
                {
                    float t = (Time.realtimeSinceStartup - t0) / (secondsPerState + warmupSeconds);
                    float z = Mathf.Lerp(-2f, 34f, Mathf.PingPong(t * 2f, 1f));
                    cam.transform.position = new Vector3(3f, 2.7f, z);
                    cam.transform.rotation = Quaternion.Euler(18f, -12f + Mathf.Sin(t * 12f) * 25f, 0f);
                    yield return null;
                    if (Time.realtimeSinceStartup - t0 > warmupSeconds) frames.Add(Time.unscaledDeltaTime * 1000f);
                }
                frames.Sort();
                float P(float q) => frames[Mathf.Clamp(Mathf.CeilToInt(q * frames.Count) - 1, 0, frames.Count - 1)];
                float avg = frames.Average();
                results.Add($"{{\"state\":\"{(isRestored ? "restored" : "degraded")}{(lite ? "_shadowLite" : "")}\",\"frames\":{frames.Count},\"avgFps\":{1000f / avg:F1}," +
                            $"\"avgMs\":{avg:F2},\"p50Ms\":{P(0.5f):F2},\"p95Ms\":{P(0.95f):F2},\"p99Ms\":{P(0.99f):F2},\"maxMs\":{frames[frames.Count - 1]:F2}," +
                            $"\"over16_67ms\":{frames.Count(f => f > 16.67f)}}}");
            }
            string json = $"{{\"gpu\":\"{SystemInfo.graphicsDeviceName}\",\"cpu\":\"{SystemInfo.processorType}\",\"resolution\":\"{Screen.width}x{Screen.height}\"," +
                          $"\"vsync\":{QualitySettings.vSyncCount},\"quality\":\"{QualitySettings.names[QualitySettings.GetQualityLevel()]}\",\"states\":[{string.Join(",", results)}]}}";
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            File.WriteAllText(output, json);
            Debug.Log("[V6][Perf] " + json);
            Application.Quit(0);
        }
    }
}
