#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using Bosque.Interaction;
using Bosque.Player;
using Unity.Profiling;
using UnityEngine;

namespace Bosque.Session
{
    // Opt-in development-build verification. Never creates another gameplay state.
    public sealed class ArroyoBuildProbe : MonoBehaviour
    {
        [Serializable] public sealed class Sample
        {
            public int frames;
            public float seconds, meanFps, worstFrameMs, meanDrawCalls;
            public bool drawCallsAvailable;
        }
        [Serializable] public sealed class Report
        {
            public bool passed;
            public string error, unity, gpu, cpu, operatingSystem;
            public int ramMb, width, height, resets;
            public bool moved, jumped, rejectedEarlyExit, restored, completed, feedbackOnce;
            public Sample degraded, recovered;
        }
        private string output;
        private readonly Report report = new Report();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Activate()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-arroyo-verify") < 0) return;
            Application.runInBackground = true;
            new GameObject("DevelopmentBuildVerification").AddComponent<ArroyoBuildProbe>();
        }
        private IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-arroyo-verify-output");
            output = index >= 0 && index + 1 < args.Length ? args[index + 1] : Path.Combine(Application.persistentDataPath, "ArroyoVerification");
            Directory.CreateDirectory(output);
            Application.logMessageReceived += OnLog;
            yield return null;
            var session = FindFirstObjectByType<ArroyoSession>();
            if (!Check(session != null && session.Progress != null, "No se inicializó la sesión.")) yield break;
            report.unity = Application.unityVersion; report.gpu = SystemInfo.graphicsDeviceName;
            report.cpu = SystemInfo.processorType; report.ramMb = SystemInfo.systemMemorySize;
            report.operatingSystem = SystemInfo.operatingSystem; report.width = Screen.width; report.height = Screen.height;
            var player = session.Player;
            var input = player.GetComponent<ThirdPersonInput>(); input.enabled = false;
            var actor = player.GetComponent<PlayerInteractor>();
            var start = player.transform.position;
            for (int i = 0; i < 10; i++) { player.Simulate(Vector2.up, false, 0, .02f); yield return null; }
            report.moved = Vector3.Distance(start, player.transform.position) > .3f;
            for (int i = 0; i < 30; i++)
            { player.Simulate(Vector2.zero, i == 0, 0, .02f); report.jumped |= player.transform.position.y > .3f; yield return null; }
            report.rejectedEarlyExit = !session.TryExit(player);
            if (!Check(report.moved && report.jumped && report.rejectedEarlyExit, "Regresión de movimiento/salto/salida inicial.")) yield break;
            player.Teleport(new Vector3(-.6f, 0, 10), Quaternion.identity); Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.4f); actor.Scan();
            yield return Capture("01-degraded-interaction.png");
            report.degraded = new Sample(); yield return SampleFrames(report.degraded);
            foreach (int z in new[] { 10, 18, 26 })
            {
                player.Teleport(new Vector3(-.6f, 0, z), Quaternion.identity); Physics.SyncTransforms();
                if (!Check(actor.TryInteract(), "No se retiró el obstáculo Z=" + z)) yield break;
                input.enabled = false; yield return null;
            }
            report.restored = session.Progress.IsRestored && session.Progress.RemovedCount == 3;
            var feedback = FindFirstObjectByType<RestorationFeedback>();
            report.feedbackOnce = feedback != null && feedback.PresentationCount == 1;
            if (!Check(report.restored && report.feedbackOnce, "Restauración o efecto duplicado/ausente.")) yield break;
            yield return Capture("02-restoration-effect.png");
            player.Teleport(new Vector3(3, 0, 8), Quaternion.identity); Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.4f);
            yield return Capture("03-restored.png");
            report.recovered = new Sample(); yield return SampleFrames(report.recovered);
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
                        if (!Check(actor.TryInteract(), "Retirada fallida después de reiniciar.")) yield break;
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
        private static IEnumerator SampleFrames(Sample sample)
        {
            // Short stationary development diagnostic, not the 60 s manual QA benchmark.
            using (var calls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count"))
            {
                double finish = Time.realtimeSinceStartupAsDouble + 3;
                double start = Time.realtimeSinceStartupAsDouble;
                long sum = 0; int counted = 0;
                while (Time.realtimeSinceStartupAsDouble < finish)
                {
                    yield return null; sample.frames++;
                    sample.worstFrameMs = Mathf.Max(sample.worstFrameMs, Time.unscaledDeltaTime * 1000);
                    if (calls.Valid && calls.LastValue > 0) { sum += calls.LastValue; counted++; }
                }
                sample.seconds = (float)(Time.realtimeSinceStartupAsDouble - start);
                sample.meanFps = sample.frames / sample.seconds;
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
