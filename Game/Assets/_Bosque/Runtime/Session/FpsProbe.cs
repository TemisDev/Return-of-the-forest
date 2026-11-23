using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bosque.Session
{
    /// <summary>
    /// Medición opcional en builds: <c>-medir-fps 30</c> espera 6 s, mide los FPS durante N segundos en la escena
    /// activa, con la vista inicial, y escribe "[FPS] ..." en el log; con <c>-salir</c> cierra el juego.
    /// </summary>
    public sealed class FpsProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Activate()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-medir-fps");
            if (i < 0 || i + 1 >= args.Length || !float.TryParse(args[i + 1], out float seconds)) return;
            var go = new GameObject("MedicionFps"); DontDestroyOnLoad(go);
            var p = go.AddComponent<FpsProbe>(); p.seconds = Mathf.Clamp(seconds, 5f, 300f); p.quit = Array.IndexOf(args, "-salir") >= 0;
        }

        private float seconds = 30f;
        private bool quit;

        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(6f);
            var frames = new List<float>();
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < seconds)
            {
                frames.Add(Time.unscaledDeltaTime);
                yield return null;
            }
            var sorted = frames.OrderBy(f => f).ToList();
            float mean = frames.Count / frames.Sum();
            float p95 = sorted[Mathf.Clamp((int)(sorted.Count * .95f), 0, sorted.Count - 1)] * 1000f;
            Debug.Log($"[FPS] escena {SceneManager.GetActiveScene().name}: media {mean:F1} FPS, p95 {p95:F1} ms, peor {sorted[sorted.Count - 1] * 1000f:F1} ms, " +
                      $"{frames.Count} fotogramas, {Screen.width}x{Screen.height}, {SystemInfo.graphicsDeviceName}");
            if (quit) Application.Quit();
        }
    }
}
