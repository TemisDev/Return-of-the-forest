using System;
using System.IO;
using Bosque.Interaction;
using Bosque.Player;
using Bosque.Session;
using UnityEditor;
using UnityEngine;

namespace Bosque.CoreSetup
{
    public static class ArroyoV2Preview
    {
        private static int frames;
        public static void CaptureBatch()
        {
            frames = 0; ArroyoPlayableSetup.CreateOrOpen();
            EditorApplication.update += Capture;
        }
        private static void Capture()
        {
            if (++frames < 12) return;
            EditorApplication.update -= Capture;
            try
            {
                var player = GameObject.Find("Player").GetComponent<ThirdPersonMotor>();
                var session = UnityEngine.Object.FindFirstObjectByType<ArroyoSession>();
                session.InitializeFromScene();
                var actor = player.GetComponent<PlayerInteractor>();
                var camera = GameObject.Find("GameplayCamera").GetComponent<Camera>();
                string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.dev-logs"));
                Directory.CreateDirectory(folder);
                Pose(player, camera); ArroyoPreview.CaptureImage(camera, Path.Combine(folder, "arroyo-v2-before.png"));
                foreach (int z in new[] { 10, 18, 26 })
                {
                    player.Teleport(new Vector3(-.6f, 0, z), Quaternion.identity); Physics.SyncTransforms();
                    if (!actor.TryInteract()) throw new InvalidOperationException("No se pudo retirar obstáculo en Z " + z);
                }
                Pose(player, camera); ArroyoPreview.CaptureImage(camera, Path.Combine(folder, "arroyo-v2-restored.png"));
                Debug.Log("[ArroyoV2] Capturas reales antes/después, progreso " + session.Progress.RemovedCount);
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        private static void Pose(ThirdPersonMotor player, Camera camera)
        {
            player.Teleport(new Vector3(3, 0, 8), Quaternion.identity); Physics.SyncTransforms();
            var rig = camera.GetComponent<ThirdPersonCamera>(); rig.Configure(player.transform);
            for (int i = 0; i < 60; i++) rig.Step(Vector2.zero, .02f);
        }
    }
}
