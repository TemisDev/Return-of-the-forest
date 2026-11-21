using System;
using System.IO;
using System.Security.Cryptography;
using Bosque.Interaction;
using Bosque.Player;
using Bosque.Session;
using UnityEditor;
using UnityEngine;

namespace Bosque.CoreSetup
{
    public static class ArroyoVerification
    {
        public static void SetupBatch()
        {
            try
            {
                string source = Path.Combine(Application.dataPath, "_Bosque/Levels/Arroyo/ArroyoGreybox.unity");
                string before = Hash(source);
                ArroyoGameplaySetup.Integrate();
                int count = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
                ArroyoGameplaySetup.Integrate();
                int repeated = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
                if (count != repeated || before != Hash(source)) throw new InvalidOperationException("Integración no idempotente o cambió nivel base.");
                var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonMotor>();
                if (player.GetComponents<ThirdPersonMotor>().Length != 1 || player.GetComponents<PlayerInteractor>().Length != 1)
                    throw new InvalidOperationException("Controlador duplicado.");
                var session = UnityEngine.Object.FindFirstObjectByType<ArroyoSession>();
                if (session.Progress.RemovedCount != 0 || session.Progress.TotalCount != 3)
                    throw new InvalidOperationException("Progreso inicial inválido.");
                Debug.Log("[ArroyoV2] Integración dos veces: " + count + " -> " + repeated + " objetos. Nivel base intacto.");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        private static string Hash(string path)
        {
            using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
        }
    }
}
