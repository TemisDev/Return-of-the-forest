using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bosque.CharacterEditor
{
    /// <summary>Capturas de la Guardiana en ArroyoPlayable con poses de sus animaciones (no guarda la escena).</summary>
    public static class GuardianaCapture
    {
        public static void CaptureBatch()
        {
            int code = 0;
            try { Capture(); } catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        [MenuItem("Tools/Bosque/Personaje/Capturar poses de la Guardiana")]
        public static void Capture()
        {
            ShaderUtil.allowAsyncCompilation = false;
            var scene = EditorSceneManager.OpenScene(GuardianaBuilder.ScenePath, OpenSceneMode.Single);
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToList();
            var g = all.First(t => t.name == "Guardiana").gameObject;
            var player = g.transform.parent;
            var camGo = all.First(t => t.name == "GameplayCamera");
            var cam = camGo.GetComponent<Camera>();
            var amb = UnityEngine.Object.FindFirstObjectByType<Bosque.ArtV6.BosqueV6Ambience>();
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/Character/CAPTURAS"));
            Directory.CreateDirectory(dir);
            var animator = g.GetComponent<Animator>();
            // Poses reales del Animator Controller (mismos parámetros que envía el juego), actualizado a mano en el editor.
            var poses = new (string file, Action<Animator> setup, float seconds, Vector3 pos, float yaw, bool restored, bool back)[]
            {
                ("01_reposo_frente", a => { }, 1.2f, new Vector3(3f, 0, 2f), 180f, false, false),
                ("02_reposo_tres_cuartos", a => { }, 2.0f, new Vector3(3f, 0, 2f), 140f, false, false),
                ("03_caminar", a => a.SetFloat("Speed", 1.35f), 1.15f, new Vector3(3f, 0, 2f), 120f, false, false),
                ("04_trotar", a => a.SetFloat("Speed", 4.5f), .95f, new Vector3(3f, 0, 2f), 110f, true, false),
                ("05_salto", a => { a.SetTrigger("Jump"); a.SetBool("Grounded", false); a.SetFloat("VerticalSpeed", 3f); }, .45f, new Vector3(3f, .5f, 2f), 140f, true, false),
                ("06_interactuar", a => a.SetTrigger("Interact"), .55f, new Vector3(-.6f, 0, 8.6f), 0f, false, false),
                ("07_restaurar", a => a.SetTrigger("Restore"), .5f, new Vector3(3f, 0, 2f), 160f, true, false),
                ("08_espalda_juego", a => a.SetFloat("Speed", 4.5f), .6f, new Vector3(3f, 0, 2f), 0f, true, true),
            };
            var restoredGo = all.First(t => t.name == "RestoredVisuals").gameObject;
            var degradedGo = all.First(t => t.name == "DegradedVisuals").gameObject;
            foreach (var p in poses)
            {
                restoredGo.SetActive(p.restored); degradedGo.SetActive(!p.restored);
                if (amb != null) { amb.ApplyImmediate(p.restored); DynamicGI.UpdateEnvironment(); }
                player.SetPositionAndRotation(p.pos, Quaternion.Euler(0, p.yaw, 0));
                animator.Rebind(); animator.Update(0f);
                p.setup(animator);
                for (float t = 0; t < p.seconds; t += 1f / 60f) animator.Update(1f / 60f);
                var state = animator.GetCurrentAnimatorStateInfo(0);
                var target = p.pos + Vector3.up * 1.05f;
                Vector3 camPos = p.back ? p.pos + Quaternion.Euler(0, p.yaw, 0) * new Vector3(0, 2.3f, -4.2f)
                                        : p.pos + new Vector3(-.3f, 1.25f, 2.6f);
                if (p.file.StartsWith("06")) camPos = p.pos + new Vector3(2.1f, 1.3f, -1.1f);
                cam.transform.SetPositionAndRotation(camPos, Quaternion.LookRotation((p.back ? p.pos + Vector3.up * 1.45f : target) - camPos));
                Render(cam, Path.Combine(dir, $"guardiana_{p.file}.png"));
                Debug.Log($"[Guardiana][Captura] {p.file} estado base={(state.IsName("Locomocion") ? "Locomocion" : state.IsName("SaltoInicio") ? "SaltoInicio" : state.IsName("Aire") ? "Aire" : state.fullPathHash.ToString())} " +
                          $"gestos={(animator.GetCurrentAnimatorStateInfo(1).IsName("Interactuar") ? "Interactuar" : animator.GetCurrentAnimatorStateInfo(1).IsName("Restaurar") ? "Restaurar" : "Vacio")}");
            }
            animator.Rebind();
        }

        private static void Render(Camera camera, string path)
        {
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active; var prevTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = rt; camera.Render(); camera.Render();
                RenderTexture.active = rt;
                var img = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                img.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); img.Apply();
                File.WriteAllBytes(path, img.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(img);
            }
            finally { camera.targetTexture = prevTarget; RenderTexture.active = prev; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
        }
    }
}
