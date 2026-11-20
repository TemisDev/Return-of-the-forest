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
            StartCoroutine(args.Contains("-v6-perf-breakdown") ? Breakdown(args) : Run(args));
        }

        private static GameObject FindAny(string name) =>
            Resources.FindObjectsOfTypeAll<Transform>().Where(t => t.name == name && t.gameObject.scene.IsValid()).Select(t => t.gameObject).FirstOrDefault();

        /// <summary>
        /// Reparto del coste en estado restaurado: cada fase quita una sola cosa (en memoria, nada se guarda).
        /// Además registra los primeros frames tras cargar para localizar el congelamiento inicial.
        /// </summary>
        private IEnumerator Breakdown(string[] args)
        {
            float awakeAt = Time.realtimeSinceStartup;
            var startup = new List<float>(); var stamps = new List<string>();
            for (int i = 0; i < 240; i++)
            {
                yield return null; startup.Add(Time.unscaledDeltaTime * 1000f);
                if (i < 12) stamps.Add($"{Time.frameCount}:{Time.realtimeSinceStartup:F3}");
            }
            int oi = Array.IndexOf(args, "-v6-perf-out");
            string output = oi >= 0 && oi + 1 < args.Length ? args[oi + 1] : Path.Combine(Application.persistentDataPath, "v6-perf-breakdown.json");
            var cam = FindAny("GameplayCamera").GetComponent<Camera>();
            foreach (var mb in cam.GetComponents<MonoBehaviour>()) if (mb.GetType().Name == "ThirdPersonCamera") mb.enabled = false;
            var camData = cam.GetUniversalAdditionalCameraData();
            var degraded = FindAny("DegradedVisuals"); var restored = FindAny("RestoredVisuals");
            foreach (var o in new[] { "Obstacle_01", "Obstacle_02", "Obstacle_03" }.Select(FindAny)) o.SetActive(false);
            degraded.SetActive(false); restored.SetActive(true);
            var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            float baseShadow = urp.shadowDistance; float baseScale = urp.renderScale;
            var veg = restored.GetComponentsInChildren<Transform>(true).Where(t => t.name == "V6_Vegetation").Select(t => t.gameObject).ToArray();
            var fx = restored.GetComponentsInChildren<Transform>(true).Where(t => t.name == "V6_FX").Select(t => t.gameObject).ToArray();
            var trees = restored.GetComponentsInChildren<Transform>(true).Where(t => t.name == "V6_Trees").Select(t => t.gameObject).ToArray();
            var backdrop = restored.GetComponentsInChildren<Transform>(true).Where(t => t.name == "V6_Backdrop").Select(t => t.gameObject).ToArray();
            var smallVeg = veg.SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).Where(r => r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off).ToArray();
            var sun = FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
            var sunData = sun != null ? sun.GetComponent<UniversalAdditionalLightData>() : null;
            var baseSoft = sunData != null ? sunData.softShadowQuality : default;
            var bdTrees = backdrop.SelectMany(g => g.GetComponentsInChildren<LODGroup>(true)).ToArray();
            var bdBlocks = backdrop.SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)).Where(r => r.name.StartsWith("V6_")).ToArray();
            var bdRocks = backdrop.SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)).Where(r => r.GetComponentInParent<LODGroup>() == null && !r.name.StartsWith("V6_")).ToArray();
            var phases = new (string name, Action on, Action off)[]
            {
                ("completo", () => { }, () => { }),
                ("sin_sombras", () => urp.shadowDistance = 0f, () => urp.shadowDistance = baseShadow),
                ("sombra_suave_baja", () => { if (sunData) sunData.softShadowQuality = SoftShadowQuality.Low; }, () => { if (sunData) sunData.softShadowQuality = baseSoft; }),
                ("sombra_dura", () => sun.shadows = LightShadows.Hard, () => sun.shadows = LightShadows.Soft),
                ("fondo_sin_arboles", () => Array.ForEach(bdTrees, g => g.gameObject.SetActive(false)), () => Array.ForEach(bdTrees, g => g.gameObject.SetActive(true))),
                ("fondo_arboles_lod1", () => Array.ForEach(bdTrees, g => g.ForceLOD(1)), () => Array.ForEach(bdTrees, g => g.ForceLOD(-1))),
                ("fondo_sin_bloques", () => Array.ForEach(bdBlocks, r => r.enabled = false), () => Array.ForEach(bdBlocks, r => r.enabled = true)),
                ("fondo_sin_rocas", () => Array.ForEach(bdRocks, r => r.enabled = false), () => Array.ForEach(bdRocks, r => r.enabled = true)),
                ("sin_vegetacion", () => Array.ForEach(veg, g => g.SetActive(false)), () => Array.ForEach(veg, g => g.SetActive(true))),
            };
            // Ciclos cortos alternados: el calentamiento del equipo afecta por igual a todas las fases.
            var perPhase = phases.ToDictionary(ph => ph.name, ph => new List<float>());
            for (int pass = 0; pass < 5; pass++)
                foreach (var (name, on, off) in phases)
                {
                    on();
                    var frames = perPhase[name];
                    float t0 = Time.realtimeSinceStartup, dur = 4f;
                    while (Time.realtimeSinceStartup - t0 < dur + 1f)
                    {
                        float t = (Time.realtimeSinceStartup - t0) / (dur + 1f);
                        cam.transform.position = new Vector3(3f, 2.7f, Mathf.Lerp(-2f, 34f, Mathf.PingPong(t * 2f, 1f)));
                        cam.transform.rotation = Quaternion.Euler(18f, -12f + Mathf.Sin(t * 12f) * 25f, 0f);
                        yield return null;
                        if (Time.realtimeSinceStartup - t0 > 1f) frames.Add(Time.unscaledDeltaTime * 1000f);
                    }
                    off();
                }
            var results = new List<string>();
            foreach (var kv in perPhase)
            {
                var frames = kv.Value; frames.Sort(); float avg = frames.Average();
                float p95 = frames[Mathf.Clamp(Mathf.CeilToInt(.95f * frames.Count) - 1, 0, frames.Count - 1)];
                results.Add("{\"fase\":\"" + kv.Key + "\",\"frames\":" + frames.Count + ",\"avgFps\":" + (1000f / avg).ToString("F1") + ",\"p95Ms\":" + p95.ToString("F2") + "}");
            }
            startup.Sort();
            string json = $"{{\"gpu\":\"{SystemInfo.graphicsDeviceName}\",\"api\":\"{SystemInfo.graphicsDeviceType}\",\"resolution\":\"{Screen.width}x{Screen.height}\"," +
                          $"\"segundos_hasta_awake\":{awakeAt:F2},\"primeros_frames\":\"{string.Join(" ", stamps)}\",\"arranque_peor_ms\":{startup[startup.Count - 1]:F1},\"arranque_frames_mas_100ms\":{startup.Count(f => f > 100f)}," +
                          $"\"fases\":[{string.Join(",", results)}]}}";
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            File.WriteAllText(output, json);
            Debug.Log("[V6][Perf] " + json);
            Application.Quit(0);
        }

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
