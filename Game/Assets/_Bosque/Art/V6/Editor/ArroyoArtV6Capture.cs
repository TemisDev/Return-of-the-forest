using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Bosque.ArtV6.Editor
{
    /// <summary>
    /// Capturas renderizadas reales (URP) de la escena de muestra V6 en ambos estados, desde
    /// encuadres fijos. No guarda la escena. Salida: Docs/Art/Entregable06/CAPTURAS.
    /// </summary>
    public static class ArroyoArtV6Capture
    {
        private static readonly (string name, Vector3 pos, Vector3 look)[] Views =
        {
            ("01_juego", new Vector3(3f, 2.71f, -3.74f), new Vector3(3f, 1.25f, 0.27f)),
            ("02_tronco", new Vector3(1.2f, 1.5f, 6.2f), new Vector3(-2.5f, -0.3f, 10f)),
            ("03_rocas", new Vector3(1.0f, 1.6f, 14.2f), new Vector3(-2.5f, -0.2f, 18.5f)),
            ("04_ramas", new Vector3(1.0f, 1.5f, 22.5f), new Vector3(-2.5f, -0.2f, 26f)),
            ("05_cauce", new Vector3(0.3f, 1.1f, 8.5f), new Vector3(-2.8f, -0.5f, 22f)),
            ("06_salida", new Vector3(3f, 1.8f, 28.5f), new Vector3(3f, 1.2f, 38.5f)),
            ("07_vista_general", new Vector3(13f, 9f, -9f), new Vector3(0f, 0f, 16f)),
        };

        public static void BuildAndCaptureBatch()
        {
            int code = 0;
            try
            {
                ArroyoArtV6Builder.BuildAll();
                if (!ArroyoArtV6Builder.ReportShaders()) code = 3;
                ArroyoArtV6Dresser.BuildShowcase();
                Capture();
            }
            catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        public static void CaptureBatch()
        {
            int code = 0;
            try { Capture(); }
            catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        /// <summary>Build Windows solo de la escena de muestra, para medir rendimiento (Builds/ArtV6Preview, fuera de Git).</summary>
        public static void BuildPreviewBatch()
        {
            int code = 0;
            try
            {
                string exe = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/ArtV6Preview/BosqueV6Preview.exe"));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ArroyoArtV6Dresser.ShowcaseScene },
                    locationPathName = exe,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None,
                });
                Debug.Log($"[V6][Build] {report.summary.result} errores={report.summary.totalErrors} avisos={report.summary.totalWarnings} tamaño={report.summary.totalSize} -> {exe}");
                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) code = 2;
            }
            catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        [MenuItem("Tools/Bosque/Arte/V6/4 - Capturar escena de muestra (ambos estados)")]
        public static void Capture()
        {
            ShaderUtil.allowAsyncCompilation = false;
            var scene = EditorSceneManager.OpenScene(ArroyoArtV6Dresser.ShowcaseScene, OpenSceneMode.Single);
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/Art/Entregable06/CAPTURAS"));
            Directory.CreateDirectory(folder);
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToList();
            Transform F(string n) => all.First(t => t.name == n);
            var degraded = F("DegradedVisuals").gameObject;
            var restored = F("RestoredVisuals").gameObject;
            var obstacles = new[] { "Obstacle_01", "Obstacle_02", "Obstacle_03" }.Select(n => F(n).gameObject).ToArray();
            var blocker = F("ExitBlocker").gameObject;
            var ambience = UnityEngine.Object.FindFirstObjectByType<BosqueV6Ambience>();
            var cam = F("GameplayCamera").GetComponent<Camera>();

            foreach (bool isRestored in new[] { false, true })
            {
                degraded.SetActive(!isRestored);
                restored.SetActive(isRestored);
                foreach (var o in obstacles) o.SetActive(!isRestored);
                blocker.SetActive(!isRestored);
                ambience.ApplyImmediate(isRestored);
                BakeEnvironment();
                ambience.ApplyImmediate(isRestored);
                foreach (var ps in UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
                    if (ps.gameObject.activeInHierarchy && ps.transform.parent?.GetComponent<ParticleSystem>() == null)
                        ps.Simulate(8f, true, true, true);
                VolumeManager.instance.Update(cam.transform, ~0);
                var ca = VolumeManager.instance.stack.GetComponent<ColorAdjustments>();
                var vg = VolumeManager.instance.stack.GetComponent<Vignette>();
                Debug.Log($"[V6][PostFX] restored={isRestored} saturation={ca.saturation.value} vignette={vg.intensity.value} " +
                          $"pp={cam.GetUniversalAdditionalCameraData().renderPostProcessing} volumes={UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Length}");
                foreach (var (name, pos, look) in Views)
                {
                    cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos));
                    string file = Path.Combine(folder, $"v6_{(isRestored ? "restaurado" : "degradado")}_{name}.png");
                    Render(cam, file, 1920, 1080);
                    Debug.Log("[V6][Capture] " + file);
                }
            }
            // No se guarda la escena: las activaciones fueron solo para fotografiar.
        }

        private static void BakeEnvironment()
        {
            var settings = new LightingSettings { bakedGI = false, realtimeGI = false };
            Lightmapping.lightingSettings = settings;
            if (!Lightmapping.Bake()) Debug.LogWarning("[V6] Lightmapping.Bake devolvió false");
        }

        private static void Render(Camera camera, string path, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            var prev = RenderTexture.active;
            var prevTarget = camera.targetTexture;
            try
            {
                // Camera.Render recorre el pipeline completo de URP, incluido el post-proceso del stack de volúmenes.
                camera.targetTexture = rt;
                camera.Render();
                camera.Render();
                RenderTexture.active = rt;
                var img = new Texture2D(w, h, TextureFormat.RGB24, false);
                img.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                img.Apply();
                File.WriteAllBytes(path, img.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(img);
            }
            finally
            {
                camera.targetTexture = prevTarget;
                RenderTexture.active = prev;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }
    }
}
