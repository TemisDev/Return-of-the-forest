using System;
using Bosque.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bosque.Session
{
    /// <summary>
    /// Atajo para pruebas manuales: <c>BosqueDeRegreso.exe -zona claro</c> abre directamente la zona 2 con el
    /// crecimiento desbloqueado (como si el arroyo ya estuviera completado). Sin el argumento no hace nada.
    /// </summary>
    public static class ZoneLauncher
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-zona");
            if (i < 0 || i + 1 >= args.Length) return;
            switch (args[i + 1].ToLowerInvariant())
            {
                case "claro":
                    GameProgress.CompleteZone(GameProgress.Arroyo);
                    if (SceneManager.GetActiveScene().name != "ClaroVacio") SceneManager.LoadScene("ClaroVacio");
                    break;
            }
        }
    }
}
