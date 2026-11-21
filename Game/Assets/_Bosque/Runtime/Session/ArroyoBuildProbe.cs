#if DEVELOPMENT_BUILD || UNITY_EDITOR || BOSQUE_RC1_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Bosque.Interaction;
using Bosque.Player;
using Unity.Profiling;
using UnityEngine;

namespace Bosque.Session
{
    // Opt-in verification in diagnostic builds and the review candidate. Inactive in normal play.
    public sealed class ArroyoBuildProbe : MonoBehaviour
    {
        [Serializable] public sealed class Sample
        {
            public int frames;
            public float seconds, meanFps, worstFrameMs, meanDrawCalls, minFps, maxFps, routeMetres;
            public float peakWorkingSetMb, peakUnityAllocatedMb;
            public bool drawCallsAvailable;
            public bool workingSetAvailable, unityMemoryAvailable;
            public float p50FrameMs, p95FrameMs, p99FrameMs;
            public int framesOver60Budget;
            [NonSerialized] private readonly List<float> frameTimes = new List<float>(32768);
            public void RecordFrame(float dt)
            {
                if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
                frames++; float ms = dt * 1000; frameTimes.Add(ms);
                worstFrameMs = Mathf.Max(worstFrameMs, ms);
                maxFps = Mathf.Max(maxFps, 1 / dt);
                if (ms > 1000f / 60) framesOver60Budget++;
            }
            public void Finish(float elapsed)
            {
                if (elapsed <= 0 || float.IsNaN(elapsed) || float.IsInfinity(elapsed))
                    throw new ArgumentOutOfRangeException(nameof(elapsed));
                seconds = elapsed; meanFps = frames / seconds;
                minFps = worstFrameMs > 0 ? 1000 / worstFrameMs : 0;
                frameTimes.Sort();
                p50FrameMs = Percentile(.5f); p95FrameMs = Percentile(.95f); p99FrameMs = Percentile(.99f);
            }
            private float Percentile(float p) => frameTimes.Count == 0 ? 0 :
                frameTimes[Mathf.Clamp(Mathf.CeilToInt(frameTimes.Count * p) - 1, 0, frameTimes.Count - 1)];
        }
        [Serializable] public sealed class VisualStats
        {
            public int renderers, triangles, distinctMaterials;
        }
        [Serializable] public sealed class Report
        {
            public bool passed;
            public string error, unity, gpu, cpu, operatingSystem;
            public int ramMb, width, height, resets;
            public bool moved, jumped, rejectedEarlyExit, restored, completed, feedbackOnce;
            public string graphicsApi, sampleMode;
            public int requestedSampleSeconds;
            public bool development, focusedOnStart, cursorLockedOnStart, fullscreen;
            public int qualityLevel, vsync, targetFrameRate;
            public string qualityName, fullscreenMode;
            public float firstRenderedFrameSeconds, restoreCallCpuMs;
            public int[] progressAfterRemoval = new int[3];
            public Sample degraded, recovered, restorationTransition;
            public VisualStats degradedStats, recoveredStats;
        }
        private string output;
        private readonly Report report = new Report();
        private int sampleSeconds = 3;
        private bool movingSample;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Activate()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-arroyo-verify") < 0) return;
            Application.runInBackground = true;
            // La build arranca en el menú principal: el verificador mide el arroyo, así que lo carga primero.
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "ArroyoPlayable")
            {
                UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene, UnityEngine.SceneManagement.LoadSceneMode> onLoaded = null;
                onLoaded = (scene, mode) =>
                {
                    if (scene.name != "ArroyoPlayable") return;
                    UnityEngine.SceneManagement.SceneManager.sceneLoaded -= onLoaded;
                    new GameObject("OptionalBuildVerification").AddComponent<ArroyoBuildProbe>();
                };
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += onLoaded;
                UnityEngine.SceneManagement.SceneManager.LoadScene("ArroyoPlayable");
                return;
            }
            new GameObject("OptionalBuildVerification").AddComponent<ArroyoBuildProbe>();
        }
        private IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-arroyo-verify-output");
            output = index >= 0 && index + 1 < args.Length ? args[index + 1] : Path.Combine(Application.persistentDataPath, "ArroyoVerification");
            Directory.CreateDirectory(output);
            int durationIndex = Array.IndexOf(args, "-arroyo-verify-seconds");
            if (durationIndex >= 0 && durationIndex + 1 < args.Length && int.TryParse(args[durationIndex + 1], out int duration))
                sampleSeconds = Mathf.Clamp(duration, 3, 120);
            movingSample = Array.IndexOf(args, "-arroyo-benchmark-route") >= 0;
            Application.logMessageReceived += OnLog;
            yield return null;
            var session = FindFirstObjectByType<ArroyoSession>();
            if (!Check(session != null && session.Progress != null, "No se inicializó la sesión.")) yield break;
            report.unity = Application.unityVersion; report.gpu = SystemInfo.graphicsDeviceName;
            report.cpu = SystemInfo.processorType; report.ramMb = SystemInfo.systemMemorySize;
            report.operatingSystem = SystemInfo.operatingSystem; report.width = Screen.width; report.height = Screen.height;
            report.graphicsApi = SystemInfo.graphicsDeviceType.ToString();
            report.sampleMode = movingSample ? "automatic-bank-patrol-z3-z32" : "stationary";
            report.requestedSampleSeconds = sampleSeconds;
            report.development = Debug.isDebugBuild; report.focusedOnStart = Application.isFocused;
            report.cursorLockedOnStart = Cursor.lockState == CursorLockMode.Locked;
            report.fullscreen = Screen.fullScreen; report.fullscreenMode = Screen.fullScreenMode.ToString();
            report.qualityLevel = QualitySettings.GetQualityLevel(); report.qualityName = QualitySettings.names[report.qualityLevel];
            report.vsync = QualitySettings.vSyncCount; report.targetFrameRate = Application.targetFrameRate;
            var player = session.Player;
            var input = player.GetComponent<ThirdPersonInput>(); input.enabled = false;
            var actor = player.GetComponent<PlayerInteractor>();
            yield return new WaitForEndOfFrame(); report.firstRenderedFrameSeconds = Time.realtimeSinceStartup;
            yield return Capture("00-spawn.png");
            var start = player.transform.position;
            for (int i = 0; i < 10; i++) { player.Simulate(Vector2.up, false, 0, .02f); yield return null; }
            report.moved = Vector3.Distance(start, player.transform.position) > .3f;
            for (int i = 0; i < 30; i++)
            { player.Simulate(Vector2.zero, i == 0, 0, .02f); report.jumped |= player.transform.position.y > .3f; yield return null; }
            report.rejectedEarlyExit = !session.TryExit(player);
            if (!Check(report.moved && report.jumped && report.rejectedEarlyExit, "Regresión de movimiento/salto/salida inicial.")) yield break;
            yield return BankPose(player, "state-degraded.png");
            player.Teleport(new Vector3(-.6f, 0, 18), Quaternion.identity); Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.4f); yield return Capture("06-camera-bank.png");
            player.Teleport(new Vector3(-.6f, 0, 10), Quaternion.identity); Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.4f); actor.Scan();
            yield return Capture("01-degraded-interaction.png");
            report.degradedStats = CollectVisualStats();
            report.degraded = new Sample(); yield return SampleFrames(report.degraded, player);
            int removal = 0;
            foreach (int z in new[] { 10, 18, 26 })
            {
                player.Teleport(new Vector3(-.6f, 0, z), Quaternion.identity); Physics.SyncTransforms();
                if (!Check(actor.TryInteract(), "No se retiró el obstáculo Z=" + z)) yield break;
                report.progressAfterRemoval[removal++] = session.Progress.RemovedCount;
                input.enabled = false; yield return null;
            }
            report.restored = session.Progress.IsRestored && session.Progress.RemovedCount == 3;
            var feedback = FindFirstObjectByType<RestorationFeedback>();
            report.feedbackOnce = feedback != null && feedback.PresentationCount == 1;
            if (!Check(report.restored && report.feedbackOnce, "Restauración o efecto duplicado/ausente.")) yield break;
            yield return Capture("02-restoration-effect.png");
            yield return BankPose(player, "03-restored.png");
            report.recoveredStats = CollectVisualStats();
            report.recovered = new Sample(); yield return SampleFrames(report.recovered, player);
            player.Teleport(new Vector3(3, 0, 33), Quaternion.identity); Physics.SyncTransforms();
            for (int i = 0; i < 60 && !session.Progress.IsCompleted; i++)
            { player.Simulate(Vector2.up, false, 0, .02f); yield return null; }
            report.completed = session.Progress.IsCompleted;
            if (!Check(report.completed, "No se completó el volumen de salida.")) yield break;
            yield return Capture("04-completed.png");
            for (int cycle = 0; cycle < 3; cycle++)
            {
                if (cycle > 0)
                {
                    foreach (int z in new[] { 10, 18, 26 })
                    {
                        player.Teleport(new Vector3(-.6f, 0, z), Quaternion.identity); Physics.SyncTransforms();
                        double callStarted = Time.realtimeSinceStartupAsDouble;
                        if (!Check(actor.TryInteract(), "Retirada fallida después de reiniciar.")) yield break;
                        input.enabled = false;
                        if (cycle == 2 && z == 26)
                        {
                            report.restoreCallCpuMs = (float)((Time.realtimeSinceStartupAsDouble - callStarted) * 1000);
                            report.restorationTransition = new Sample();
                            // Separate repeat: no screenshot encoding during this three-second window.
                            yield return SampleFrames(report.restorationTransition, player, 3, false);
                        }
                        input.enabled = false; yield return null;
                    }
                    player.Teleport(new Vector3(3, 0, 33), Quaternion.identity); Physics.SyncTransforms();
                    for (int step = 0; step < 60 && !session.Progress.IsCompleted; step++)
                    { player.Simulate(Vector2.up, false, 0, .02f); yield return null; }
                    if (!Check(session.Progress.IsCompleted && feedback.PresentationCount == cycle + 1,
                        "Finalización o efecto incorrecto en un ciclo posterior.")) yield break;
                }
                session.Restart(); input.enabled = false;
                if (!Check(session.Progress.RemovedCount == 0 && !session.Progress.IsCompleted && actor.enabled,
                    "Reinicio inconsistente.")) yield break;
                report.resets++; yield return null;
            }
            yield return Capture("05-restarted.png");
            if (!Check(!Debug.isDebugBuild || (report.degraded.drawCallsAvailable && report.recovered.drawCallsAvailable),
                "No hay render verificable. Ejecuta la prueba con una ventana visible.")) yield break;
            report.passed = true; Save();
            Debug.Log("[BuildProbe] Verificación completada: " + output);
            Application.Quit(0);
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, name));
            yield return new WaitForSecondsRealtime(.15f);
        }
        private IEnumerator BankPose(ThirdPersonMotor player, string name)
        {
            player.Teleport(new Vector3(3, 0, 8), Quaternion.identity); Physics.SyncTransforms();
            FindFirstObjectByType<ThirdPersonCamera>().Configure(player.transform);
            yield return new WaitForSecondsRealtime(.5f);
            yield return Capture(name);
        }
        private static VisualStats CollectVisualStats()
        {
            var stats = new VisualStats(); var materials = new HashSet<Material>();
            foreach (var renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!renderer.enabled) continue;
                stats.renderers++;
                foreach (var material in renderer.sharedMaterials) if (material != null) materials.Add(material);
                var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh != null) for (int i = 0; i < mesh.subMeshCount; i++) stats.triangles += (int)mesh.GetIndexCount(i) / 3;
            }
            stats.distinctMaterials = materials.Count; return stats;
        }
        private IEnumerator SampleFrames(Sample sample, ThirdPersonMotor player, int duration = 0, bool allowRoute = true)
        {
            // Opt-in automatic route: does not claim keyboard input or a human QA playthrough.
            bool patrol = movingSample && allowRoute;
            if (patrol)
            {
                player.Teleport(new Vector3(3, 0, 3), Quaternion.identity); Physics.SyncTransforms();
                yield return new WaitForSecondsRealtime(.5f);
            }
            using (var process = System.Diagnostics.Process.GetCurrentProcess())
            using (var calls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count"))
            {
                double finish = Time.realtimeSinceStartupAsDouble + (duration > 0 ? duration : sampleSeconds);
                double start = Time.realtimeSinceStartupAsDouble;
                double nextMemorySample = start;
                long sum = 0; int counted = 0;
                bool forward = true;
                while (Time.realtimeSinceStartupAsDouble < finish)
                {
                    yield return null; sample.RecordFrame(Time.unscaledDeltaTime);
                    if (patrol)
                    {
                        var before = player.transform.position;
                        var target = new Vector3(3, before.y, forward ? 32 : 3);
                        if (Vector3.Distance(before, target) < .3f) forward = !forward;
                        var direction = target - before; direction.y = 0;
                        direction = direction.normalized;
                        player.Simulate(new Vector2(direction.x, direction.z), false, 0, Time.deltaTime);
                        sample.routeMetres += Vector3.Distance(before, player.transform.position);
                    }
                    if (Time.realtimeSinceStartupAsDouble >= nextMemorySample)
                    {
                        process.Refresh();
                        sample.peakWorkingSetMb = Mathf.Max(sample.peakWorkingSetMb, process.WorkingSet64 / 1048576f);
                        sample.peakUnityAllocatedMb = Mathf.Max(sample.peakUnityAllocatedMb,
                            UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576f);
                        nextMemorySample = Time.realtimeSinceStartupAsDouble + 1;
                    }
                    if (calls.Valid && calls.LastValue > 0) { sum += calls.LastValue; counted++; }
                }
                sample.Finish((float)(Time.realtimeSinceStartupAsDouble - start));
                sample.workingSetAvailable = sample.peakWorkingSetMb > 0;
                sample.unityMemoryAvailable = sample.peakUnityAllocatedMb > 0;
                sample.drawCallsAvailable = counted > 0;
                sample.meanDrawCalls = counted > 0 ? (float)sum / counted : 0;
            }
        }
        private bool Check(bool success, string error)
        {
            if (success) return true;
            report.error = error; Save(); Application.Quit(2); return false;
        }
        private void Save() => File.WriteAllText(Path.Combine(output, "results.json"), JsonUtility.ToJson(report, true));
        private void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            report.error = message; report.passed = false; Save(); Application.Quit(2);
        }
        private void OnDestroy() => Application.logMessageReceived -= OnLog;
    }
}
#endif
