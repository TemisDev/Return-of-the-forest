using System;
using Bosque.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bosque.Session
{
    /// <summary>
    /// Atajo para pruebas manuales: <c>-zona claro</c> abre la zona 2 y <c>-zona refugio</c> la zona 3, con el
    /// progreso previo dado por hecho (crecimiento desbloqueado). Sin el argumento no hace nada.
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
                case "valle":
                    GameProgress.CompleteZone(GameProgress.Arroyo);
                    GameProgress.CompleteZone(GameProgress.Claro);
                    GameProgress.CompleteZone(GameProgress.Refugio);
                    if (SceneManager.GetActiveScene().name != "ValleOlvidado") SceneManager.LoadScene("ValleOlvidado");
                    break;
                case "refugio":
                    GameProgress.CompleteZone(GameProgress.Arroyo);
                    GameProgress.CompleteZone(GameProgress.Claro);
                    if (SceneManager.GetActiveScene().name != "RefugioPerdido") SceneManager.LoadScene("RefugioPerdido");
                    break;
            }
        }
    }
}
