using System;
using System.IO;
using Bosque.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Bosque.CoreSetup
{
    public static class ArroyoPreview
    {
        private static int frames;
        public static void CaptureBatch()
        {
            frames = 0;
            ArroyoPlayableSetup.CreateOrOpen();
            EditorApplication.update += Capture;
        }
        private static void Capture()
        {
            if (++frames < 10) return;
            EditorApplication.update -= Capture;
            try
            {
                var camera = GameObject.Find("GameplayCamera").GetComponent<Camera>();
                var player = GameObject.Find("Player").GetComponent<ThirdPersonMotor>();
                var rig = camera.GetComponent<ThirdPersonCamera>();
                string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.dev-logs"));
                Directory.CreateDirectory(folder);
                CaptureImage(camera, Path.Combine(folder, "arroyo-start.png"));
                for (int i = 0; i < 160; i++)
                {
                    player.Simulate(Vector2.up, false, 0, .02f);
                    Physics.SyncTransforms();
                    rig.Step(Vector2.zero, .02f);
                }
                CaptureImage(camera, Path.Combine(folder, "arroyo-walk.png"));
                Debug.Log("[T06] Capturas de escena y avance por simulacion: " + player.transform.position);
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        private static void CaptureImage(Camera camera, string path)
        {
            var texture = new RenderTexture(1280, 720, 24);
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = texture };
            var previous = RenderTexture.active;
            Texture2D image = null;
            try
            {
                texture.Create();
                if (!RenderPipeline.SupportsRenderRequest(camera, request))
                    throw new InvalidOperationException("URP no admite captura en este contexto.");
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = texture;
                image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                texture.Release(); UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
